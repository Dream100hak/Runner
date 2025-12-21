using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Spawns floating damage numbers on the UI canvas. If no prefab is assigned,
/// a TextMeshProUGUI element is created at runtime.
/// </summary>
public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    private readonly System.Collections.Generic.List<GameObject> _activePopups = new System.Collections.Generic.List<GameObject>();
    private CoreGameManager _coreGameManager;

    [Tooltip("Optional prefab containing a TextMeshProUGUI component. If null, one will be created at runtime.")]
    [SerializeField] private TMP_Text popupPrefab;
    [Tooltip("Canvas where damage popups will be placed. Should be a Screen Space - Camera or Overlay canvas.")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private float floatDistance = 40f;
    [SerializeField] private float duration = 0.9f;
    // removed random jitter — use explicit owner directions instead
    [Header("World offset (when victimForward is provided)")]
    [Tooltip("Horizontal world-space offset multiplier applied opposite victim forward.")]
    [SerializeField] private float worldHorizontalOffset = 0.6f;
    [Tooltip("Vertical world-space offset added on top of the victim position.")]
    [SerializeField] private float worldVerticalOffset = 0.8f;
    [Tooltip("Multiplier applied to horizontal offset when critical is true.")]
    [SerializeField] private float criticalHorizontalMultiplier = 1.4f;
    [Tooltip("Additional vertical offset when critical is true.")]
    [SerializeField] private float criticalVerticalAdd = 0.4f;
    [Header("Travel multipliers")]
    [Tooltip("General multiplier to make the popup travel farther when victimForward is provided.")]
    [SerializeField] private float travelDistanceMultiplier = 1.5f;
    [Tooltip("Additional multiplier applied when critical is true.")]
    [SerializeField] private float travelCriticalMultiplier = 1.2f;

    [Header("Owner specific directions")]
    [Tooltip("Screen-space offset (canvas units) applied to player popups. X = left/right, Y = up/down.")]
    [SerializeField] private Vector2 playerPopupScreenOffset = new Vector2(-50f, 10f);
    [Tooltip("Screen-space offset (canvas units) applied to enemy popups. X = left/right, Y = up/down.")]
    [SerializeField] private Vector2 enemyPopupScreenOffset = new Vector2(50f, 10f);

    [Header("Limits")]
    [Tooltip("Maximum number of simultaneous damage popups shown on screen. When exceeded, the oldest popup will be dismissed upward.")]
    [SerializeField] private int maxPopups = 3;

    public enum PopupOwner { None, Player, Enemy }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas == null)
                Debug.LogWarning("DamagePopupManager: No Canvas found in scene. Popups may not display.");
        }

        // try to subscribe to core manager state changes
        if (CoreGameManager.Instance != null)
        {
            _coreGameManager = CoreGameManager.Instance;
            _coreGameManager.OnStateChanged += OnCoreStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_coreGameManager != null)
            _coreGameManager.OnStateChanged -= OnCoreStateChanged;
        Debug.Log("DamagePopupManager: OnDestroy - unsubscribed");
    }

    private void OnEnable()
    {
        if (_coreGameManager == null && CoreGameManager.Instance != null)
        {
            _coreGameManager = CoreGameManager.Instance;
            _coreGameManager.OnStateChanged += OnCoreStateChanged;
        }
    }

    private void OnDisable()
    {
        if (_coreGameManager != null)
        {
            _coreGameManager.OnStateChanged -= OnCoreStateChanged;
            _coreGameManager = null;
        }
    }

    private void Update()
    {
        // Ensure we subscribe to CoreGameManager if it becomes available later
        if (_coreGameManager == null && CoreGameManager.Instance != null)
        {
            SubscribeToCore();
        }
    }

    private void SubscribeToCore()
    {
        if (_coreGameManager != null) return;
        if (CoreGameManager.Instance == null) return;

        _coreGameManager = CoreGameManager.Instance;
        _coreGameManager.OnStateChanged += OnCoreStateChanged;
        Debug.Log("DamagePopupManager: Subscribed to CoreGameManager.OnStateChanged");
    }

    /// <summary>
    /// Show damage number at a world position.
    /// If owner is provided, the popup will arc using the owner-specific direction (configured in inspector).
    /// </summary>
    public void ShowDamage(Vector3 worldPosition, int damage, Color? color = null, bool critical = false, PopupOwner owner = PopupOwner.None)
    {
        if (targetCanvas == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPosition);
        if (screenPos.z < 0f)
            return; // behind camera

        // If we're already at the max visible popups, dismiss the oldest one upward
        if (_activePopups.Count >= maxPopups)
        {
            var oldest = _activePopups[0];
            if (oldest != null)
                DismissPopupEarly(oldest);
        }

        // create popup
        TMP_Text tmp;
        GameObject go;
        if (popupPrefab != null)
        {
            go = Instantiate(popupPrefab.gameObject, targetCanvas.transform);
            go.SetActive(true);
            tmp = go.GetComponent<TMP_Text>();
        }
        else
        {
            go = new GameObject("DamagePopup", typeof(RectTransform));
            go.transform.SetParent(targetCanvas.transform, false);
            tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            tmp.fontSize = 32;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            // try to use default TMP settings; if fonts are missing it will fallback
        }

        if (tmp == null) return;

        // track active popup so it can be cleared when returning to running
        _activePopups.Add(go);
        // add a marker so ClearAllPopups can find any popup instances even if tracking misses them
        go.AddComponent<PopupTag>();

        tmp.text = damage.ToString();
        tmp.color = color ?? (critical ? Color.yellow : Color.red);
        tmp.fontStyle = critical ? FontStyles.Bold : FontStyles.Normal;

        // convert screen to canvas local point for start
        RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, out Vector2 localStart);
        RectTransform rt = tmp.GetComponent<RectTransform>();

        Vector2 localEnd = localStart;

        // compute critical multiplier once
        float critMulLocal = critical ? travelCriticalMultiplier : 1f;

        // If an owner is specified, use the configured screen-space offset (canvas units) so designer can set X/Y directly.
        if (owner == PopupOwner.Player)
        {
            localEnd = localStart + playerPopupScreenOffset * critMulLocal * travelDistanceMultiplier;
        }
        else if (owner == PopupOwner.Enemy)
        {
            localEnd = localStart + enemyPopupScreenOffset * critMulLocal * travelDistanceMultiplier;
        }

        // ensure popup rendered on top
        go.transform.SetAsLastSibling();
        // ensure pivot is centered for predictable movement
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = localStart;

        // initial scale & alpha
        rt.localScale = Vector3.one * (critical ? 1.15f : 0.9f);
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        // animate: quick parabolic arc (using DOJumpAnchorPos) + pop scale and fade
        Vector2 startPos = rt.anchoredPosition;
        // compute deterministic end position using owner screen offsets (in canvas units)
        float critMul = critMulLocal;
        float xOffset = 0f;
        float yOffset = 0f;
        if (owner == PopupOwner.Player)
        {
            xOffset = playerPopupScreenOffset.x * travelDistanceMultiplier * critMul;
            yOffset = playerPopupScreenOffset.y * travelDistanceMultiplier * critMul;
        }
        else if (owner == PopupOwner.Enemy)
        {
            xOffset = enemyPopupScreenOffset.x * travelDistanceMultiplier * critMul;
            yOffset = enemyPopupScreenOffset.y * travelDistanceMultiplier * critMul;
        }

        Vector2 endPos = new Vector2(localStart.x + xOffset, localStart.y + yOffset + (floatDistance * travelDistanceMultiplier + (critical ? 18f * travelDistanceMultiplier : 0f)));
        float jumpPower = Mathf.Max(12f, floatDistance * 0.6f * travelDistanceMultiplier);
        int numJumps = 1;

        Sequence seq = DOTween.Sequence();
        // set the sequence target so we can kill it by GameObject when clearing
        seq.SetTarget(go);
        // small pop first
        seq.Append(rt.DOScale(Vector3.one * (critical ? 1.35f : 1.05f), duration * 0.15f).SetEase(Ease.OutBack));
        // perform the jump arc (no auto-fade/destroy) so the popup remains visible until explicitly cleared
        var jumpTween = rt.DOJumpAnchorPos(endPos, jumpPower, numJumps, duration).SetEase(Ease.OutCubic);
        seq.Join(jumpTween);
        // slight squash at end for polish
        seq.Append(rt.DOScale(Vector3.one * 0.95f, 0.08f).SetEase(Ease.OutCubic).SetDelay(0f));
    }

    private void OnCoreStateChanged(CoreGameManager.GameState previous, CoreGameManager.GameState next)
    {
        // when returning to running, clear any lingering damage popups
        if (next == CoreGameManager.GameState.Running)
        {
            ClearAllPopups();
        }
    }

    private void ClearAllPopups()
    {
        // First, aggressively remove any popup objects that were tagged
        if (targetCanvas != null)
        {
            var tags = targetCanvas.GetComponentsInChildren<PopupTag>(true);
            for (int t = tags.Length - 1; t >= 0; --t)
            {
                var tag = tags[t];
                if (tag == null) continue;
                var go = tag.gameObject;
                if (go == null) continue;
                // clear visible text immediately but keep object in hierarchy
                DG.Tweening.DOTween.Kill(go, complete: false);
                var cg = go.GetComponent<CanvasGroup>(); if (cg != null) cg.DOKill();
                var tmp = go.GetComponent<TMP_Text>();
                if (tmp != null) tmp.text = string.Empty;
                // make invisible
                if (cg != null) cg.alpha = 0f;
            }
        }
        // Also clear tracked popups
        for (int i = _activePopups.Count - 1; i >= 0; --i)
        {
            var go = _activePopups[i];
            if (go == null) continue;
            DG.Tweening.DOTween.Kill(go, complete: false);
            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null) cg.DOKill();
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = string.Empty;
            if (cg != null) cg.alpha = 0f;
        }
        _activePopups.Clear();
    }

    // marker component attached to instantiated popup GameObjects so they can be found and removed
    private class PopupTag : MonoBehaviour { }

    // Dismiss a popup early by moving it upward and fading it out, then destroying it.
    private void DismissPopupEarly(GameObject go)
    {
        if (go == null) return;

        // remove from tracked list if present
        if (_activePopups.Contains(go))
            _activePopups.Remove(go);

        // kill existing tweens targeting this GO
        DG.Tweening.DOTween.Kill(go);

        var rt = go.GetComponent<RectTransform>();
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        // target position: move up by a bit and slightly to preserve readable spacing
        Vector2 start = rt != null ? rt.anchoredPosition : Vector2.zero;
        Vector2 target = start + new Vector2(0f, floatDistance * 0.6f);

        float earlyDuration = Mathf.Clamp(duration * 0.5f, 0.25f, 0.6f);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(go);
        if (rt != null)
        {
            seq.Append(rt.DOAnchorPos(target, earlyDuration).SetEase(Ease.OutCubic));
            seq.Join(rt.DOScale(Vector3.one * 0.8f, earlyDuration).SetEase(Ease.OutCubic));
        }
        seq.Join(cg.DOFade(0f, earlyDuration).SetEase(Ease.Linear));
        seq.OnComplete(() => {
            // ensure any tweens targeting this GO are killed
            DG.Tweening.DOTween.Kill(go);
            if (go != null) Destroy(go);
        });
    }
}
