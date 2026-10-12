namespace Quantum
{
  using UnityEngine;
  [System.Serializable]
  public partial class CharacterInfo : AssetObject
  {
#if QUANTUM_UNITY
    public string Name;
    public Color NameColor;
#endif
  }
}