namespace TwinStickShooter
{
  using UnityEngine;

  public class LockRotationUI : MonoBehaviour
  {
    public bool LockX;
    public bool LockY;
    public bool LockZ;

    private RectTransform _rectTransform;

    private Vector3 _initialRotation;

    private void Start()
    {
      _rectTransform = GetComponent<RectTransform>();
      _initialRotation = _rectTransform.eulerAngles;
    }

    private void Update()
    {
      Vector3 newRotation = _rectTransform.eulerAngles;
      newRotation.x = LockX ? _initialRotation.x : _rectTransform.eulerAngles.x;
      newRotation.y = LockY ? _initialRotation.y : _rectTransform.eulerAngles.y;
      newRotation.z = LockZ ? _initialRotation.z : _rectTransform.eulerAngles.z;

      _rectTransform.eulerAngles = newRotation;
    }
  }
}