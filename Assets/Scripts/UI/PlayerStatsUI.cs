using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Updates the player stat UI elements when PlayerStats changes.
/// </summary>
public class PlayerStatsUI : MonoBehaviour
{
    [SerializeField] private PlayerStats _playerStats;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _expText;
    [SerializeField] private Image _hpGauge;
    [SerializeField] private Image _expGauge;

    [Header("Auto Find")]
    [SerializeField] private string hpGaugeName = "UI_IMG_HpGauge";
    [SerializeField] private string hpTextName = "UI_TMP_HP";
    [SerializeField] private string expGaugeName = "UI_IMG_ExpGauge";
    [SerializeField] private string expTextName = "UI_TMP_EXP";

    private void Awake()
    {
        if (_playerStats == null)
        {
            _playerStats = FindObjectOfType<PlayerStats>();
            if (_playerStats == null)
                Debug.LogError("PlayerStatsUI: PlayerStats component not found in scene");
        }

        if (_hpGauge == null && !string.IsNullOrEmpty(hpGaugeName))
        {
            _hpGauge = FindComponentInSceneByName<Image>(hpGaugeName);
        }
        if (_expGauge == null && !string.IsNullOrEmpty(expGaugeName))
        {
            _expGauge = FindComponentInSceneByName<Image>(expGaugeName);
        }
        if (_hpText == null && !string.IsNullOrEmpty(hpTextName))
        {
            _hpText = FindComponentInSceneByName<TMP_Text>(hpTextName);
        }
        if (_expText == null && !string.IsNullOrEmpty(expTextName))
        {
            _expText = FindComponentInSceneByName<TMP_Text>(expTextName);
        }
    }

    private void OnEnable()
    {
        if (_playerStats != null)
            _playerStats.OnStatsChanged += HandleStatsChanged;
    }

    private void OnDisable()
    {
        if (_playerStats != null)
            _playerStats.OnStatsChanged -= HandleStatsChanged;
    }

    private void Start()
    {
        if (_playerStats != null)
            UpdateUI(_playerStats);
    }

    private void HandleStatsChanged(PlayerStats stats)
    {
        UpdateUI(stats);
    }

    private void UpdateUI(PlayerStats stats)
    {
        if (stats == null) return;

        float hpFill = stats.MaxHP <= 0f ? 0f : Mathf.Clamp01(stats.CurrentHP / stats.MaxHP);
        if (_hpGauge != null)
            _hpGauge.fillAmount = hpFill;
        if (_hpText != null)
            _hpText.text = string.Format("{0:F0}/{1:F0}", stats.CurrentHP, stats.MaxHP);

        float expFill = stats.ExpToNextLevel <= 0f ? 0f : Mathf.Clamp01(stats.CurrentExp / stats.ExpToNextLevel);
        if (_expGauge != null)
            _expGauge.fillAmount = expFill;
        if (_expText != null)
            _expText.text = string.Format("{0:F0}/{1:F0}", stats.CurrentExp, stats.ExpToNextLevel);
    }

    private T FindComponentInSceneByName<T>(string objectName) where T : Component
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            var result = RecursiveFind<T>(roots[i].transform, objectName);
            if (result != null) return result;
        }
        return null;
    }

    private T RecursiveFind<T>(Transform parent, string objectName) where T : Component
    {
        if (string.Equals(parent.name, objectName, System.StringComparison.OrdinalIgnoreCase))
        {
            var comp = parent.GetComponent<T>();
            if (comp != null) return comp;
        }
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            var found = RecursiveFind<T>(child, objectName);
            if (found != null) return found;
        }
        return null;
    }
}
