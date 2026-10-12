using System;

namespace TwinStickShooter
{
  using UnityEngine;
  using Quantum;

  public class HidingGroupsDrawer : MonoBehaviour
  {
    public float FontSize = 10;
    public Vector3 Offset;
    
    public void ClearHidingGroups()
    {
      if (QuantumUnityDB.TryGetGlobalAsset("Resources/DB/InvisibilityGroups", out InvisibilityGroups asset))
      {
        asset.Groups.Clear();
      }
    }
  }
}