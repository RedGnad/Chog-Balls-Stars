namespace TwinStickShooter
{
  using UnityEngine;

  public enum EDisableBehaviour
  {
    Destroy,
    Disable
  }

  public class TTL : MonoBehaviour
  {
    [SerializeField] private EDisableBehaviour _disableBehaviour;
    [SerializeField] private float _lifetime;
    private float _timer;

    private void OnEnable()
    {
      _timer = _lifetime;
    }

    // Update is called once per frame
    void Update()
    {
      _timer -= Time.deltaTime;
      if (_timer <= 0)
      {
        switch (_disableBehaviour)
        {
          case EDisableBehaviour.Destroy:
            Destroy(this.gameObject);
            break;
          case EDisableBehaviour.Disable:
            this.enabled = false;
            break;
          default:
            break;
        }
      }
    }
  }
}