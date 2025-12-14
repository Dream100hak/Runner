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
    // Calculate and store initial offset between camera and player
    if (_playerTransform != null)
        {
  _offset = transform.position - _playerTransform.position;
        }
        
     _originalPosition = transform.position;
    }

    private void LateUpdate()
    {
        // Follow player with smooth interpolation
     if (_playerTransform != null)
        {
     FollowPlayer();
  }

      // Apply camera shake if active
   if (_shakeTimer > 0)
        {
        ApplyCameraShake();
      _shakeTimer -= Time.deltaTime;
        }
        else if (_shakeTimer <= 0 && _shakeTimer != -1)
  {
       // Reset to original position after shake
      transform.position = _originalPosition;
      _shakeTimer = -1;
        }
    }

    /// <summary>
    /// Smoothly follows the player while maintaining the offset.
    /// </summary>
    private void FollowPlayer()
    {
        Vector3 targetPosition = _playerTransform.position + _offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, _smoothSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Applies camera shake effect for impact feedback.
    /// </summary>
    private void ApplyCameraShake()
    {
        Vector3 shakeOffset = Random.insideUnitSphere * _shakeIntensity;
     transform.position = _originalPosition + shakeOffset;
    }

    /// <summary>
    /// Triggers a camera shake effect.
    /// </summary>
    /// <param name="intensity">Intensity of the shake effect</param>
    /// <param name="duration">Duration of the shake effect in seconds</param>
    public void Shake(float intensity = -1f, float duration = -1f)
    {
        _shakeIntensity = intensity > 0 ? intensity : _shakeIntensity;
        _shakeDuration = duration > 0 ? duration : _shakeDuration;
        _shakeTimer = _shakeDuration;
        _originalPosition = transform.position;
        
      Debug.Log($"CameraController: Camera shake triggered (intensity: {_shakeIntensity}, duration: {_shakeDuration})");
    }

    /// <summary>
    /// Gets the current offset between camera and player.
    /// </summary>
    public Vector3 GetOffset() => _offset;

    /// <summary>
    /// Sets a new offset between camera and player.
    /// </summary>
    public void SetOffset(Vector3 newOffset)
    {
        _offset = newOffset;
    }

  /// <summary>
    /// Sets the player transform to follow.
    /// </summary>
    public void SetPlayerTransform(Transform playerTransform)
    {
        _playerTransform = playerTransform;
    }
}
