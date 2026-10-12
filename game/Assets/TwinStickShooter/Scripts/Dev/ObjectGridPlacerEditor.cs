namespace TwinStickShooter
{
#if UNITY_EDITOR
  using UnityEngine;
  using UnityEditor;

  [CustomEditor(typeof(ObjectGridPlacer))]
  public class ObjectGridPlacerEditor : Editor
  {
    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();
      ObjectGridPlacer placer = (ObjectGridPlacer)target;
      if (GUILayout.Button("Generate"))
      {
        placer.GenerateGrid();
      }

      if (GUILayout.Button("Clear"))
      {
        placer.Clean();
      }
    }
  }
#endif
}