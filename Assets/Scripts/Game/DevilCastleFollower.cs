using UnityEngine;

/// <summary>
/// Keeps the devil castle positioned relative to the active camera so it stays at a fixed distance from the view.
/// </summary>
public class DevilCastleFollower : MonoBehaviour
{
    [SerializeField] private Camera _followCamera;
    [SerializeField] private Vector3 _offset = new Vector3(0f, -1.5f, 20f);
    [SerializeField] private bool _matchRotation = true;

    private void Awake()
    {
        if (_followCamera == null)
        {
            _followCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (_followCamera == null) return;

        Vector3 targetPosition = _followCamera.transform.TransformPoint(_offset);
        transform.position = targetPosition;

        if (_matchRotation)
        {
            transform.rotation = _followCamera.transform.rotation;
        }
    }
}
