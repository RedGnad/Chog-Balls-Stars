namespace TwinStickShooter
{
  using UnityEngine;
  using Quantum;

  public class HideHealthBarTimer : QuantumCallbacks
  {
    public float TimeToHide = 2;

    private float _timer;
    private QuantumEntityView _entityView;

    private void Start()
    {
      _timer = TimeToHide;
      _entityView = GetComponent<CharacterUIFollow>().EntityView;
      QuantumEvent.Subscribe<EventCharacterDamaged>(this, OnCharacterDamaged);
    }

    private void OnCharacterDamaged(EventCharacterDamaged e)
    {
      if (e.target == _entityView.EntityRef)
      {
        _timer = 0;
        transform.gameObject.SetActive(true);
      }
    }

    public void Update()
    {
      _timer += Time.deltaTime;
      if (_timer > TimeToHide)
      {
        transform.gameObject.SetActive(false);
      }
    }
  }
}