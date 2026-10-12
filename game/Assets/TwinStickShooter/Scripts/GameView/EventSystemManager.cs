namespace TwinStickShooter
{
  using UnityEngine;
  using UnityEngine.EventSystems;

  public class EventSystemManager : MonoBehaviour
  {
    public EventSystem GameEventSystem;

    private void Awake()
    {
      EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();

      if (eventSystems.Length > 1)
      {
        foreach (var item in eventSystems)
        {
          if (item == GameEventSystem)
          {
            Destroy(item.gameObject);
          }
        }
      }
    }
  }
}