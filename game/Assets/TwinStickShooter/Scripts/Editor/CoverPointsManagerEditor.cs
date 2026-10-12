namespace TwinStickShooter
{
  using UnityEditor;
  using UnityEngine;

  [CustomEditor(typeof(CoverPointsManager))]
  public class CoverPointsManagerEditor : Editor
  {
    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();

      if (GUILayout.Button("Generate Cover Points") == true)
      {
        (target as CoverPointsManager).GenerateCoverPoints();
      }

      if (GUILayout.Button("Clear Cover Points") == true)
      {
        (target as CoverPointsManager).ClearCoverPoints();
      }
    }

    private void OnSceneGUI()
    {
      Handles.color = Color.red;

      var targetGameObject = target as MonoBehaviour;
      for (int i = 0; i < targetGameObject.transform.childCount; i++)
      {
        var child = targetGameObject.transform.GetChild(i);
        Handles.DrawSolidDisc(child.transform.position, Vector3.up, .25f);
      }
    }
  }
}