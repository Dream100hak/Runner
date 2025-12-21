using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Plays a quick full-screen fade transition when entering battle.
/// Created at runtime if not present. Uses DOTween for smooth fade.
/// </summary>
public class BattleTransition : MonoBehaviour
{
    public static BattleTransition Instance { get; private set; }

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _image;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static BattleTransition CreateIfMissing()
    {
        if (Instance != null) return Instance;

        var go = new GameObject("BattleTransition");
        var bt = go.AddComponent<BattleTransition>();

        // create canvas
        var canvasGO = new GameObject("BT_Canvas");
        canvasGO.transform.SetParent(go.transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var cg = canvasGO.AddComponent<CanvasGroup>();
        bt._canvasGroup = cg;

        // create full-screen image
        var imgGO = new GameObject("BT_Image");
        imgGO.transform.SetParent(canvasGO.transform, false);
        var img = imgGO.AddComponent<Image>();
        img.color = Color.black;
        bt._image = img;

        var rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        // initial invisible
        cg.alpha = 0f;

        return bt;
    }

    /// <summary>
    /// Play a quick fade-to-black then back transition. onMidpoint is invoked when fully faded.
    /// </summary>
    public void Play(float totalDuration = 0.22f, Action onMidpoint = null)
    {
        if (_canvasGroup == null) return;

        // kill existing tweens
        _canvasGroup.DOKill();
        float inDur = totalDuration * 0.4f;
        float hold = totalDuration * 0.2f;
        float outDur = totalDuration - inDur - hold;

        Sequence seq = DOTween.Sequence();
        seq.Append(_canvasGroup.DOFade(1f, inDur).SetEase(Ease.OutQuad));
        seq.AppendInterval(hold);
        seq.AppendCallback(() => onMidpoint?.Invoke());
        seq.Append(_canvasGroup.DOFade(0f, outDur).SetEase(Ease.InQuad));
        seq.Play();
    }
}
