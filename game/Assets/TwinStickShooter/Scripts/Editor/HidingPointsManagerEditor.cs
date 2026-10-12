namespace TwinStickShooter
{
#if UNITY_EDITOR
  using Quantum;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEditor;
  using UnityEngine;

  [CustomEditor(typeof(HidingGroupsDrawer))]
  public class HidingPointsManagerEditor : Editor
  {
    private Dictionary<int, Vector3> _groupsAndPositions;

    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();

      EditorGUILayout.Space();

      EditorGUILayout.HelpBox("The Hiding Groups are baked during the Map's OnBake callback", MessageType.Info);

      EditorGUILayout.Space();

      if (GUILayout.Button("Clear Groups Ids") == true)
      {
        if (EditorUtility.DisplayDialog("Are you sure?",
              "This action will delete the hiding groups info from the baked assed", "Yes", "No") == true)
        {
          (target as HidingGroupsDrawer).ClearHidingGroups();
          _groupsAndPositions.Clear();
        }
      }
    }

    private void OnSceneGUI()
    {
      var hidingPointsManager = target as HidingGroupsDrawer;

      if (_groupsAndPositions == null)
      {
        FillGroupsAndPositions();
      }

      Handles.color = Color.black;

      GUIStyle style = new GUIStyle();
      style.fontSize = (int)(hidingPointsManager.FontSize / SceneView.lastActiveSceneView.size);
      style.normal.textColor = Color.yellow;
      foreach (var groupPos in _groupsAndPositions)
      {
        Handles.DrawSolidDisc(groupPos.Value, Vector3.up, .5f);
        Handles.Label(groupPos.Value - hidingPointsManager.Offset, groupPos.Key.ToString(), style);
      }
    }

    private void FillGroupsAndPositions()
    {
      _groupsAndPositions = new Dictionary<int, Vector3>();

      if (QuantumUnityDB.TryGetGlobalAsset<InvisibilityGroups>("Resources/DB/InvisibilityGroups", out var asset))
      {
        List<Group> groups = asset.Groups;

        QuantumMapData mapData = FindObjectOfType<QuantumMapData>();

        Dictionary<int, Vector3> positionsSums = new Dictionary<int, Vector3>();
        Dictionary<int, int> positionsCounts = new Dictionary<int, int>();

        for (int groupId = 0; groupId < groups.Count; groupId++)
        {
          Group group = groups[groupId];

          if (positionsSums.ContainsKey(groupId) == false)
          {
            positionsSums.Add(groupId, Vector3.zero);
            positionsCounts.Add(groupId, 0);
          }

          for (int j = 0; j < group.StaticCollidersIds.Count; j++)
          {
            int colliderId = group.StaticCollidersIds[j];
            MonoBehaviour prop = mapData.StaticCollider2DReferences[colliderId];

            positionsSums[groupId] += prop.transform.position;
            positionsCounts[groupId]++;
          }
        }

        foreach (var sum in positionsSums)
        {
          var average = sum.Value / positionsCounts[sum.Key];
          _groupsAndPositions.Add(sum.Key, average);
        }
      }
    }
  }
#endif
}