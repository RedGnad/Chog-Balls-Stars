namespace TwinStickShooter
{
  using UnityEngine;

  public class QuantumStaticColliderId : MonoBehaviour
  {
    [SerializeField] private int _staticId;

    public void SetId(int id)
    {
      _staticId = id;
    }
  }
}