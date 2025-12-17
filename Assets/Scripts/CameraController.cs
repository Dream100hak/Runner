using UnityEngine;

/// <summary>
/// Manages camera tracking for diagonal quarter-view perspective.
/// Maintains offset from player and includes camera shake effects.
/// </summary>
public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private float _smoothSpeed = 5f;
    [SerializeField] private float _shakeIntensity = 0.1f;
    [SerializeField] private float _shakeDuration = 0.2f;

    private Vector3 _offset = Vector3.zero;
    private Camera _camera;
    private float _shakeTimer = 0f;
    private Vector3 _originalPosition;
    private Vector3 _savedCameraPosition;
    private Quaternion _savedCameraRotation;
    private bool _manualView;
    private Vector3 _manualPosition;
    private Quaternion _manualRotation;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
            Debug.LogError("CameraController: Camera component not found");
        if (_playerTransform == null)
            Debug.LogError("CameraController: Player transform not set");
    }

    private void Start()
    {
        if (_playerTransform != null)
        {
            _offset = transform.position - _playerTransform.position;
        }
        _originalPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (_manualView)
        {
            transform.position = _manualPosition;
            transform.rotation = _manualRotation;
        }
        else if (_playerTransform != null)
        {
            FollowPlayer();
        }

        _originalPosition = transform.position;

        if (_shakeTimer > 0)
        {
            ApplyCameraShake();
            _shakeTimer -= Time.deltaTime;
        }
        else if (_shakeTimer <= 0 && _shakeTimer != -1)
        {
            transform.position = _originalPosition;
            _shakeTimer = -1;
        }
    }

    private void FollowPlayer()
    {
        Vector3 targetPosition = _playerTransform.position + _offset;
        Vector3 currentPos = transform.position;
        currentPos.x = Mathf.Lerp(currentPos.x, targetPosition.x, _smoothSpeed * Time.deltaTime);
        currentPos.z = Mathf.Lerp(currentPos.z, targetPosition.z, _smoothSpeed * Time.deltaTime);
        transform.position = currentPos;
    }

    private void ApplyCameraShake()
    {
        Vector3 shakeOffset = new Vector3(
            0f,
            Random.Range(-_shakeIntensity, _shakeIntensity),
            Random.Range(-_shakeIntensity, _shakeIntensity)
        );

        transform.position = _originalPosition + shakeOffset;
    }

    public void Shake(float intensity = -1f, float duration = -1f)
    {
        if (intensity > 0f)
            _shakeIntensity = intensity;
        if (duration > 0f)
            _shakeDuration = duration;

        _shakeTimer = _shakeDuration;
        _originalPosition = transform.position;
        Debug.Log($"CameraController: Camera shake triggered (intensity: {_shakeIntensity}, duration: {_shakeDuration})");
    }

    public Vector3 GetOffset() => _offset;

    public void SetOffset(Vector3 newOffset)
    {
        _offset = newOffset;
    }

    public void SetPlayerTransform(Transform playerTransform)
    {
        _playerTransform = playerTransform;
        if (_playerTransform != null)
        {
            _offset = transform.position - _playerTransform.position;
        }
    }

    public void SetManualView(Vector3 position, Quaternion rotation)
    {
        if (!_manualView)
        {
            _savedCameraPosition = transform.position;
            _savedCameraRotation = transform.rotation;
        }

        _manualView = true;
        _manualPosition = position;
        _manualRotation = rotation;
        transform.position = position;
        transform.rotation = rotation;
        _originalPosition = transform.position;
    }

    public void ClearManualView()
    {
        if (!_manualView)
            return;

        _manualView = false;
        transform.position = _savedCameraPosition;
        transform.rotation = _savedCameraRotation;
        if (_playerTransform != null)
        {
            _offset = transform.position - _playerTransform.position;
        }
        _originalPosition = transform.position;
    }
}
