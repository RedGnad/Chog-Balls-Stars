[assembly: Quantum.QuantumMapBakeAssemblyAttribute]

namespace TwinStickShooter
{
  using Quantum;
  using UnityEngine;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEditor;

  public class HidingPointsMapBaker : MapDataBakerCallback
  {
    public override void OnBake(QuantumMapData data)
    {
      
      var mapAsset = QuantumUnityDB.GetGlobalAsset<Map>(data.GetAsset(true));

      Dictionary<int, List<MonoBehaviour>> groupsDict = new Dictionary<int, List<MonoBehaviour>>();
      FillGroupsIds(data, mapAsset, groupsDict);

      QuantumUnityDB.TryGetGlobalAsset("QuantumUser/Resources/InvisibilityGroups", out InvisibilityGroups asset);

      var groupsList = new List<Group>();

      // Populate the asset groups info
      foreach (var group in groupsDict)
      {
        foreach (var colliderComponent in group.Value)
        {
          var index = data.StaticCollider2DReferences.IndexOf(colliderComponent);
          var staticColliderId = mapAsset.StaticColliders2D[index].StaticData.ColliderIndex;

          if (groupsList.Count <= group.Key)
          {
            var nestedList = new Group
            {
              StaticCollidersIds = new List<int>()
            };
            groupsList.Add(nestedList);
          }

          groupsList[group.Key].StaticCollidersIds.Add(staticColliderId);
        }
      }

      asset.Groups = groupsList;
    }

    public override void OnBeforeBake(QuantumMapData data)
    {
    }

    private void FillGroupsIds(QuantumMapData data, Map mapAsset, Dictionary<int, List<MonoBehaviour>> groupsDict)
    {
      List<GameObject> allProps = GameObject.FindGameObjectsWithTag("InvisibilityProp").ToList();

      List<BoxCollider> dummyColliders = new List<BoxCollider>();
      foreach (var prop in allProps)
      {
        AddDummyCollider(dummyColliders, prop);
      }

      byte groupId = 0;
      while (allProps.Count > 0)
      {
        var randomProp = allProps[0];

        FillNeighborsIds(data, mapAsset, allProps, groupsDict, randomProp, groupId);

        groupId++;
      }

      RemoveDummyColliders(dummyColliders);
    }

    private void FillNeighborsIds(QuantumMapData data, Map mapAsset, List<GameObject> allProps,
      Dictionary<int, List<MonoBehaviour>> groupsDict, GameObject prop, byte groupId)
    {
      allProps.Remove(prop);

      if (groupsDict.ContainsKey(groupId) == false)
      {
        groupsDict.Add(groupId, new List<MonoBehaviour>());
      }

      QuantumStaticBoxCollider2D colliderComponent = prop.GetComponent<QuantumStaticBoxCollider2D>();
      groupsDict[groupId].Add(colliderComponent);

      var prototype = prop.GetComponent<QPrototypeInvisibilitySpot>().Prototype;
      prototype.GroupId = groupId;
      prototype.StaticColliderId = GetStaticColliderId(data, mapAsset, colliderComponent);

      var position = prop.transform.position;
      prototype.NorthNeighborId =
        TryFillNeighbor(data, mapAsset, allProps, groupsDict, position, Vector3.forward, groupId);
      prototype.SouthNeighborId =
        TryFillNeighbor(data, mapAsset, allProps, groupsDict, position, Vector3.back, groupId);
      prototype.EastNeighborId =
        TryFillNeighbor(data, mapAsset, allProps, groupsDict, position, Vector3.right, groupId);
      prototype.WestNeighborId = TryFillNeighbor(data, mapAsset, allProps, groupsDict, position, Vector3.left, groupId);

#if UNITY_EDITOR
      EditorUtility.SetDirty(prop.GetComponent<QPrototypeInvisibilitySpot>());
#endif
    }

    private int TryFillNeighbor(QuantumMapData data, Map mapAsset, List<GameObject> allProps,
      Dictionary<int, List<MonoBehaviour>> groupsDict, Vector3 origin, Vector3 direction, byte groupId)
    {
      var neighbors =
        Physics.OverlapSphere(origin + direction, 0.25f, UnityEngine.LayerMask.GetMask("InvisibilityPoint"));
      bool foundNeighbor = neighbors.Length > 0;
      if (foundNeighbor)
      {
        var neighbor = neighbors[0].gameObject;
        // If the object is still on the props list, it means that it was not processed yet, so we do it
        if (allProps.Contains(neighbor))
        {
          FillNeighborsIds(data, mapAsset, allProps, groupsDict, neighbor, groupId);
        }

        QuantumStaticBoxCollider2D colliderComponent =
          neighbors[0].gameObject.GetComponent<QuantumStaticBoxCollider2D>();
        return GetStaticColliderId(data, mapAsset, colliderComponent);
      }

      return -1;
    }

    private static void AddDummyCollider(List<BoxCollider> dummyColliders, GameObject prop)
    {
      var quantumCollider = prop.GetComponentInChildren<QuantumStaticBoxCollider2D>();

      var dummyCollider = prop.AddComponent<BoxCollider>();
      var colliderSize = quantumCollider.Size.ToUnityVector2();
      dummyCollider.size = new Vector3(colliderSize.x, 1, colliderSize.y);
      dummyCollider.center = new Vector3(0, .5f, 0);

      dummyColliders.Add(dummyCollider);
    }

    private void RemoveDummyColliders(List<BoxCollider> dummyColliders)
    {
      foreach (var collider in dummyColliders)
      {
        MonoBehaviour.DestroyImmediate(collider);
      }
    }

    private int GetStaticColliderId(QuantumMapData data, Map mapAsset,
      QuantumStaticBoxCollider2D colliderComponent)
    {
      var index = data.StaticCollider2DReferences.IndexOf(colliderComponent);
      return mapAsset.StaticColliders2D[index].StaticData.ColliderIndex;
    }
  }
}