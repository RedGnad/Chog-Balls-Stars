namespace TwinStickShooter
{
  using System;
  using UnityEngine;

  [Serializable]
  public class ObjectGridPlacer : MonoBehaviour
  {
    public int GridWidth = 60;
    public int GridHeight = 60;
    public GameObject Prefab;

    public Vector3 OffSet;
    public Quaternion Rotation;
    public bool Centralized = true;

    public void GenerateGrid()
    {
      for (int i = 0; i < GridWidth; i++)
      {
        for (int j = 0; j < GridHeight; j++)
        {
          Vector3 position = new Vector3(j * OffSet.x, OffSet.y, i * OffSet.z);
          if (Centralized)
          {
            position = position - new Vector3(OffSet.x * GridWidth / 2, 0, OffSet.z * GridHeight / 2);
          }

          Instantiate(Prefab, position, Rotation, transform);
        }
      }
    }

    public void Clean()
    {
      Transform[] objs = GetComponentsInChildren<Transform>();
      for (int i = 0; i < objs.Length; i++)
      {
        if (objs[i] != transform)
        {
          DestroyImmediate(objs[i].gameObject);
        }
      }
    }
  }
}