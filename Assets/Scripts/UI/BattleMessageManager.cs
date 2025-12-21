using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;

/// <summary>
/// Central manager for short battle UI messages. Targets a `TMP_InputField` named `UI_IPF_Msg` by default
/// but can be assigned in inspector. Provides simple API for common battle messages.
/// </summary>
public class BattleMessageManager : MonoBehaviour
{
    public static BattleMessageManager Instance { get; private set; }

    [SerializeField] private TMP_InputField messageInputField;
    [SerializeField] private string messageFieldName = "UI_IPF_Msg";
    // displayDuration removed ? messages persist until explicitly removed or exceeded maxMessages
    [SerializeField] private int maxMessages = 6;
    [Header("Animated List")]
    [Tooltip("If set, messages will be created as individual TMP_Text items under this container and animated. If null, falls back to TMP_InputField text mode.")]
    [SerializeField] private RectTransform messageContainer;
    [Tooltip("Prefab containing a TMP_Text (and optional CanvasGroup) used for each message line")]
    [SerializeField] private TMP_Text messagePrefab;
    [SerializeField] private float lineHeight = 22f;
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private float fadeDuration = 0.18f;
    [SerializeField] private float slideInOffset = 10f;

    private class MessageEntry { public string text; public GameObject go; public RectTransform rt; public CanvasGroup cg; }
    private readonly System.Collections.Generic.List<MessageEntry> _messages = new System.Collections.Generic.List<MessageEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (messageInputField == null && !string.IsNullOrEmpty(messageFieldName))
        {
            messageInputField = FindComponentInSceneByName<TMP_InputField>(messageFieldName);
        }

        // If a message prefab is assigned, ensure lineHeight is reasonable relative to its font size
        if (messagePrefab != null)
        {
            // suggested line height = fontSize + lineSpacing (at least some padding)
            float suggested = messagePrefab.fontSize + Mathf.Max(4f, messagePrefab.lineSpacing);
            if (lineHeight < suggested)
                lineHeight = Mathf.CeilToInt(suggested);
        }

        if (messageInputField != null)
        {
            // Make it read-only for display
            messageInputField.readOnly = true;
            messageInputField.interactable = false;
            // enable multiline and bottom alignment so messages stack upward like a chat
            messageInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            if (messageInputField.textComponent != null)
            {
                messageInputField.textComponent.alignment = TMPro.TextAlignmentOptions.BottomLeft;
                messageInputField.textComponent.enableWordWrapping = true;
            }
            messageInputField.text = "";
        }
        else
        {
            Debug.LogWarning("BattleMessageManager: TMP_InputField not found. Create one named '" + messageFieldName + "' or assign in inspector.");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ShowEncounter()
    {
        ShowMessage("적이 나타났다!");
    }

    public void ShowPlayerDamage(string playerName, int damage)
    {
        if (string.IsNullOrEmpty(playerName)) playerName = "플레이어";
        ShowMessage(string.Format("{0}은(는) {1}의 데미지를 받았다!", playerName, damage));
    }

    public void ShowEnemyDamage(string enemyName, int damage)
    {
        if (string.IsNullOrEmpty(enemyName)) enemyName = "적";
        ShowMessage(string.Format("{0}은(는) {1}의 데미지를 받았다!", enemyName, damage));
    }

    public void ShowEnemyDefeated()
    {
        ShowMessage("적을 해치웠다!");
    }

    public void ShowMessage(string message)
    {
        // If animated container is set and prefab available, use animated list
        if (messageContainer != null && messagePrefab != null)
        {
            var go = GameObject.Instantiate(messagePrefab.gameObject, messageContainer);
            go.SetActive(true);
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = message;

            // ensure CanvasGroup exists for fades
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            var rt = go.GetComponent<RectTransform>();

            var entry = new MessageEntry { text = message, go = go, rt = rt, cg = cg };
            _messages.Add(entry);

            // enforce maximum messages: remove oldest immediately if over limit
            if (_messages.Count > maxMessages)
            {
                // remove oldest with fade and destroy
                RemoveOldestImmediate();
            }

            // set starting position slightly lower for slide-in
            int idx = _messages.IndexOf(entry);
            float targetY = -idx * lineHeight;
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, targetY - slideInOffset);
            }

            // animate reposition for all entries
            RepositionMessagesAnimated();

            // fade in new entry
            cg.DOFade(1f, fadeDuration);

            // messages persist until removed by limit or manually
            return;
        }

        // fallback to simple input field mode
        if (messageInputField == null)
        {
            Debug.Log(message);
            return;
        }

        // add message entry for fallback textual buffer (persistent)
        var simpleEntry = new MessageEntry { text = message, go = null, rt = null, cg = null };
        _messages.Add(simpleEntry);
        while (_messages.Count > maxMessages) _messages.RemoveAt(0);
        UpdateDisplay();
        // messages persist until removed by limit or manually
    }

    private void RepositionMessagesAnimated()
    {
        for (int i = 0; i < _messages.Count; i++)
        {
            var e = _messages[i];
            if (e.rt == null) continue;
            float targetY = -i * lineHeight;
            e.rt.DOAnchorPosY(targetY, slideDuration).SetEase(Ease.OutCubic);
        }
    }

    private void RemoveOldestImmediate()
    {
        if (_messages.Count == 0) return;
        var oldest = _messages[0];
        _messages.RemoveAt(0);
        if (oldest.cg != null)
        {
            // fade out then destroy
            oldest.cg.DOFade(0f, fadeDuration).OnComplete(() => Destroy(oldest.go));
        }
        else if (oldest.go != null)
        {
            Destroy(oldest.go);
        }
        // reposition remaining
        RepositionMessagesAnimated();
    }

    private void UpdateDisplay()
    {
        if (messageInputField == null) return;
        // join messages with newline; older messages are at the top
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < _messages.Count; i++)
        {
            sb.AppendLine(_messages[i].text);
        }
        messageInputField.text = sb.ToString();
    }

    // ExpiryLoop removed ? messages no longer auto-expire

    // Helper: find component by object name (recursive across scene)
    private T FindComponentInSceneByName<T>(string objectName) where T : Component
    {
        var scene = SceneManager.GetActiveScene();
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
