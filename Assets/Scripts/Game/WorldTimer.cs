using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Counts down from startTime to 0 and updates a TextMeshPro UI text named "UI_TimeText" (or assigned in inspector).
/// Also interpolates scene lighting and camera background color to create a day->night (world-ending) effect.
/// When timer reaches 0, triggers a short destruction sequence.
/// </summary>
public class WorldTimer : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private float startTime = 30f;
    [SerializeField] private bool startOnAwake = true;

    [Header("UI")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private string timeTextObjectName = "UI_TimeText";

    [Header("Lighting")]
    [Tooltip("Color at start (day)")]
    [SerializeField] private Color dayColor = new Color(1f, 0.95f, 0.8f);
    [Tooltip("Color at sunset")]
    [SerializeField] private Color sunsetColor = new Color(1f, 0.5f, 0.2f);
    [Tooltip("Color at end (night)")]
    [SerializeField] private Color nightColor = new Color(0.03f, 0.05f, 0.12f);
    [SerializeField] private float dayIntensity = 1f;
    [SerializeField] private float nightIntensity = 0.05f;

    [Header("Fog")]
    [SerializeField] private bool enableFog = true;
    [SerializeField] private Color fogDayColor = new Color(0.8f, 0.85f, 0.9f, 1f);
    [SerializeField] private Color fogNightColor = new Color(0.02f, 0.05f, 0.1f, 1f);
    [SerializeField] private float fogDensityDay = 0.003f;
    [SerializeField] private float fogDensityNight = 0.04f;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    private float _timeLeft;
    private bool _running;
    private Light _directionalLight;
    private Color _originalAmbient;
    private Color _originalCamBg;
    private Material _skyboxInstance;
    private Color _skyboxOriginalTint = Color.white;
    private Color _skyboxOriginalGround = Color.gray;
    private float _skyboxOriginalExposure = 1f;
    private float _skyboxOriginalAtmosphere = 1f;
    private CameraController _cameraController;

    private void Awake()
    {
        _timeLeft = Mathf.Max(0f, startTime);

        if (timeText == null && !string.IsNullOrEmpty(timeTextObjectName))
        {
            timeText = FindComponentInSceneByName<TMP_Text>(timeTextObjectName);
            if (timeText == null) Debug.LogWarning("WorldTimer: UI_TimeText not found by name: " + timeTextObjectName);
        }

        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null) _originalCamBg = targetCamera.backgroundColor;

        _cameraController = FindObjectOfType<CameraController>();

        var lights = FindObjectsOfType<Light>();
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                _directionalLight = l;
                break;
            }
        }

        _originalAmbient = RenderSettings.ambientSkyColor;

        if (RenderSettings.skybox != null)
        {
            _skyboxInstance = new Material(RenderSettings.skybox);
            RenderSettings.skybox = _skyboxInstance;

            if (_skyboxInstance.HasProperty("_SkyTint")) _skyboxOriginalTint = _skyboxInstance.GetColor("_SkyTint");
            if (_skyboxInstance.HasProperty("_GroundColor")) _skyboxOriginalGround = _skyboxInstance.GetColor("_GroundColor");
            if (_skyboxInstance.HasProperty("_Exposure")) _skyboxOriginalExposure = _skyboxInstance.GetFloat("_Exposure");
            if (_skyboxInstance.HasProperty("_AtmosphereThickness")) _skyboxOriginalAtmosphere = _skyboxInstance.GetFloat("_AtmosphereThickness");
        }

        if (enableFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
        }
    }

    private void Start()
    {
        UpdateUIImmediate();
        if (startOnAwake) StartTimer();
    }

    private void Update()
    {
        if (!_running) return;
        if (_timeLeft <= 0f) return;

        _timeLeft -= Time.deltaTime;
        if (_timeLeft < 0f) _timeLeft = 0f;
        UpdateUI(_timeLeft);
        UpdateLighting(_timeLeft);

        if (_timeLeft == 0f)
        {
            _running = false;
            StartCoroutine(WorldEndSequence());
        }
    }

    public void StartTimer()
    {
        _timeLeft = Mathf.Max(0f, startTime);
        _running = true;
    }

    public void StopTimer()
    {
        _running = false;
    }

    private void UpdateUIImmediate()
    {
        UpdateUI(_timeLeft);
        UpdateLighting(_timeLeft);
    }

    private void UpdateUI(float t)
    {
        if (timeText == null) return;
        timeText.text = t.ToString("F2");
    }

    private void UpdateLighting(float t)
    {
        if (startTime <= 0f) return;
        float progress = 1f - (t / startTime);

        if (_directionalLight != null)
        {
            if (progress < 0.6f)
            {
                float p = Mathf.InverseLerp(0f, 0.6f, progress);
                _directionalLight.color = Color.Lerp(dayColor, sunsetColor, p);
                _directionalLight.intensity = Mathf.Lerp(dayIntensity, dayIntensity * 0.6f, p);
            }
            else
            {
                float p = Mathf.InverseLerp(0.6f, 1f, progress);
                _directionalLight.color = Color.Lerp(sunsetColor, nightColor, p);
                _directionalLight.intensity = Mathf.Lerp(dayIntensity * 0.6f, nightIntensity, p);
            }
        }

        RenderSettings.ambientSkyColor = Color.Lerp(dayColor, nightColor, progress);

        if (targetCamera != null)
        {
            targetCamera.backgroundColor = Color.Lerp(_originalCamBg, nightColor * 0.35f, progress);
        }

        if (enableFog)
        {
            RenderSettings.fogColor = Color.Lerp(fogNightColor, fogDayColor, progress);
            RenderSettings.fogDensity = Mathf.Lerp(fogDensityNight, fogDensityDay, progress);
        }

        if (_skyboxInstance != null)
        {
            Color targetTint = (progress < 0.6f)
                ? Color.Lerp(_skyboxOriginalTint, sunsetColor, Mathf.InverseLerp(0f, 0.6f, progress))
                : Color.Lerp(sunsetColor, nightColor, Mathf.InverseLerp(0.6f, 1f, progress));

            if (_skyboxInstance.HasProperty("_SkyTint")) _skyboxInstance.SetColor("_SkyTint", targetTint);

            if (_skyboxInstance.HasProperty("_Exposure"))
            {
                float exposure = Mathf.Lerp(_skyboxOriginalExposure, 0.15f, progress);
                _skyboxInstance.SetFloat("_Exposure", exposure);
            }

            if (_skyboxInstance.HasProperty("_AtmosphereThickness"))
            {
                float atm = Mathf.Lerp(_skyboxOriginalAtmosphere, 0.4f, progress);
                _skyboxInstance.SetFloat("_AtmosphereThickness", atm);
            }

            if (_skyboxInstance.HasProperty("_GroundColor"))
            {
                Color ground = Color.Lerp(_skyboxOriginalGround, nightColor * 0.5f, progress);
                _skyboxInstance.SetColor("_GroundColor", ground);
            }
        }

        if (timeText != null)
        {
            float glow = Mathf.Clamp01(progress * 2f);
            Color baseCol = Color.Lerp(Color.white, new Color(1f, 0.6f, 0.2f), Mathf.Clamp01(progress));
            Color final = Color.Lerp(baseCol, Color.red, Mathf.SmoothStep(0.8f, 1f, progress));
            timeText.color = final;
            float scale = 1f + Mathf.Sin(Time.time * 8f) * 0.02f * (0.5f + glow);
            timeText.transform.localScale = Vector3.one * scale;
        }
    }

    private IEnumerator WorldEndSequence()
    {
        if (timeText != null)
        {
            timeText.text = "0.00";
        }

        if (_cameraController != null)
        {
            _cameraController.enabled = false;
        }

        if (timeText != null)
        {
            float dur = 1.2f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / dur;
                timeText.color = Color.Lerp(Color.red, Color.black, p);
                timeText.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.9f, p);
                yield return null;
            }
        }

        if (_directionalLight != null)
        {
            _directionalLight.color = Color.black;
            _directionalLight.intensity = 0f;
        }
        RenderSettings.ambientSkyColor = Color.black;
        if (targetCamera != null) targetCamera.backgroundColor = Color.black;

        yield return null;
    }

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
