namespace TwinStickShooter
{
  using UnityEngine;
  using Quantum;

  public class GameplaySFX : MonoBehaviour
  {
    void Start()
    {
      QuantumEvent.Subscribe<EventSkillAction>(this, OnSkillAction);
      QuantumEvent.Subscribe<EventOnCreateAttack>(this, OnCreateAttack);
    }

    private void OnSkillAction(EventSkillAction e)
    {
      SkillData asset = QuantumUnityDB.GetGlobalAsset<SkillData>(e.skillData);
      if (asset.SFX != null)
      {
        AudioManager.Instance.Play(asset.SFX);
      }
    }

    private void OnCreateAttack(EventOnCreateAttack e)
    {
      AttackData asset = QuantumUnityDB.GetGlobalAsset<AttackData>(e.skillData);
      if (asset.SFX != null)
      {
        AudioManager.Instance.Play(asset.SFX);
      }
    }
  }
}