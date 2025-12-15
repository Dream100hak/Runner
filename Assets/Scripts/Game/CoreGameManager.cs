using System;
using UnityEngine;

/// <summary>
/// Central game flow controller. Handles FSM transitions, timer countdown, and exposes events for other systems.
/// </summary>
public class CoreGameManager : MonoBehaviour
{
    public enum GameState { Intro, Running, Battle, BossCutscene, GameOver, Clear }

    public static CoreGameManager Instance { get; private set; }

    public event Action<GameState, GameState>? OnStateChanged;
    public event Action<float>? OnTimeUpdated;
    public event Action? OnTimeExpired;

    [SerializeField] private float _startTimeLimit = 30f;


    [SerializeField]
    private GameState _currentState = GameState.Intro;
    private float _timeLeft;
    private bool _timerActive;

    public GameState CurrentState => _currentState;
    public float TimeLeft => _timeLeft;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _timeLeft = _startTimeLimit;
    }

    private void Start()
    {
        SetState(GameState.Intro);
        StartGame();
    }

    private void Update()
    {
        if (!_timerActive || _timeLeft <= 0f)
            return;

        _timeLeft -= Time.deltaTime;
        if (_timeLeft < 0f)
            _timeLeft = 0f;

        OnTimeUpdated?.Invoke(_timeLeft);

        if (_timeLeft <= 0f)
        {
            _timerActive = false;
            OnTimeExpired?.Invoke();
            SetState(GameState.BossCutscene);
        }
    }

    public void StartGame()
    {
        _timeLeft = _startTimeLimit;
        SetState(GameState.Running);
    }

    public void EnterBattle()
    {
        SetState(GameState.Battle);
    }

    public void EnterBossCutscene()
    {
        SetState(GameState.BossCutscene);
    }

    public void EndGameClear()
    {
        SetState(GameState.Clear);
    }

    public void TriggerGameOver()
    {
        SetState(GameState.GameOver);
    }

    private void SetState(GameState nextState)
    {
        if (_currentState == nextState)
            return;

        GameState previous = _currentState;
        _currentState = nextState;

        _timerActive = _currentState == GameState.Running || _currentState == GameState.Battle;
        if (!_timerActive)
            OnTimeUpdated?.Invoke(_timeLeft);

        OnStateChanged?.Invoke(previous, _currentState);
    }

    public void ResetTimer()
    {
        _timeLeft = _startTimeLimit;
        OnTimeUpdated?.Invoke(_timeLeft);
      
        _timerActive = _currentState == GameState.Running || _currentState == GameState.Battle;
    }
}
