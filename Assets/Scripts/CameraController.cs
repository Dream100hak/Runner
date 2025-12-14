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
        
     // For quarter-view camera: follow X and Z, but keep Y fixed to maintain viewing angle
        // Only interpolate X and Z axes for smooth tracking
        Vector3 currentPos = transform.position;
   currentPos.x = Mathf.Lerp(currentPos.x, targetPosition.x, _smoothSpeed * Time.deltaTime);
      currentPos.z = Mathf.Lerp(currentPos.z, targetPosition.z, _smoothSpeed * Time.deltaTime);
 // Y position stays at offset (don't follow player's Y movement)
   
        transform.position = currentPos;
    }

    /// <summary>
    /// Applies camera shake effect for impact feedback.
    /// </summary>
    private void ApplyCameraShake()
    {
        // Shake only on Z axis (forward-back) and Y axis (up-down)
        // Do NOT shake on X axis (left-right) to maintain stable view
     Vector3 shakeOffset = new Vector3(
            0, // X axis: no shake (keep view centered)
          Random.Range(-_shakeIntensity, _shakeIntensity),  // Y axis: vertical shake
            Random.Range(-_shakeIntensity, _shakeIntensity)   // Z axis: forward-back shake
        );
        
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
