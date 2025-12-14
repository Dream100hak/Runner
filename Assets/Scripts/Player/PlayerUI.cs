using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Connects PlayerInventory health values to UGUI elements.
/// Tries to auto-find UI objects named "UI_HpBar" and "UI_HPBarText" if not assigned in inspector.
/// </summary>
public class PlayerUI : MonoBehaviour
{
    [SerializeField] private Slider hpBarSlider;
    [SerializeField] private TMP_Text hpBarText;

    [SerializeField] private string hpBarObjectName = "UI_HpBar";
    [SerializeField] private string hpTextObjectName = "UI_HPBarText";

    private PlayerInventory _playerInventory;
    private int _lastHp = -1;

    private void Awake()
    {
        _playerInventory = FindObjectOfType<PlayerInventory>();
        if (_playerInventory == null)
            Debug.LogError("PlayerUI: PlayerInventory not found in scene");

        // Try to find Slider and TMP_Text by name across the scene (works with inactive objects too)
        if (hpBarSlider == null && !string.IsNullOrEmpty(hpBarObjectName))
        {
            hpBarSlider = FindComponentInSceneByName<Slider>(hpBarObjectName);
            if (hpBarSlider == null) Debug.LogWarning("PlayerUI: HP bar Slider not assigned and not found by name: " + hpBarObjectName);
        }

        if (hpBarText == null && !string.IsNullOrEmpty(hpTextObjectName))
        {
            hpBarText = FindComponentInSceneByName<TMP_Text>(hpTextObjectName);
            if (hpBarText == null) Debug.LogWarning("PlayerUI: HP text (TMP) not assigned and not found by name: " + hpTextObjectName);
        }
    }

    // Recursively searches all root GameObjects in the active scene for a component attached to a GameObject with the given name.
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

    private void Start()
    {
        UpdateUIImmediate();
    }

    private void Update()
    {
        if (_playerInventory == null) return;
        int current = _playerInventory.CurrentHealth;
        if (current != _lastHp)
        {
            _lastHp = current;
            UpdateUI(current);
        }
    }

    private void UpdateUIImmediate()
    {
        if (_playerInventory == null) return;
        _lastHp = -1;
        UpdateUI(_playerInventory.CurrentHealth);
    }

    private void UpdateUI(int current)
    {
        int max = _playerInventory.MaxHealth;
        if (hpBarSlider != null && max > 0)
        {
            // Ensure slider range matches health
            if (hpBarSlider.minValue != 0) hpBarSlider.minValue = 0;
            if (hpBarSlider.maxValue != max) hpBarSlider.maxValue = max;
            hpBarSlider.value = Mathf.Clamp(current, 0, max);
        }
        if (hpBarText != null)
            hpBarText.text = string.Format("{0}/{1}", current, max);
    }
}
