namespace TwinStickShooter
{
  using System.Collections.Generic;
  using UnityEngine;
  using Photon.Deterministic;
  using Quantum;

  public class AttackPreviewDynamicMesh : MonoBehaviour
  {
    public Quantum.LayerMask ObstacleMask;

    [SerializeField] private int _meshResolveIterations;

    [SerializeField] private float _edgeDistThreshold;

    [SerializeField] private float _meshResolution;

    [SerializeField] private float _angle;

    [SerializeField] private float _radius;

    [SerializeField] private MeshFilter _meshFilter;
    private Mesh _mesh;

    public struct ViewCastInfo
    {
      public bool Hit;
      public Vector3 Point;
      public float Dist;
      public float Angle;

      public ViewCastInfo(bool hit, Vector3 point, float dist, float angle)
      {
        Hit = hit;
        Point = point;
        Dist = dist;
        Angle = angle;
      }
    }

    public struct EdgeInfo
    {
      public Vector3 PointA;
      public Vector3 PointB;

      public EdgeInfo(Vector3 pointA, Vector3 pointB)
      {
        PointA = pointA;
        PointB = pointB;
      }
    }

    private void Start()
    {
      _mesh = new Mesh();
      _mesh.name = "View Mesh";
      _meshFilter.mesh = _mesh;
    }

    private void LateUpdate()
    {
      DrawFieldOfView();
    }

    private void DrawFieldOfView()
    {
      int stepCount = Mathf.RoundToInt(_angle * _meshResolution);
      float stepAngleSize = _angle / stepCount;
      List<Vector3> viewPoints = new List<Vector3>();
      ViewCastInfo oldViewCast = new ViewCastInfo();
      for (int i = 0; i <= stepCount; i++)
      {
        float angle = transform.parent.eulerAngles.y - _angle / 2 + stepAngleSize * i;
        ViewCastInfo newViewCast = ViewCast(angle);

        if (i > 0)
        {
          bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.Dist - newViewCast.Dist) > _edgeDistThreshold;
          if (oldViewCast.Hit != newViewCast.Hit || (oldViewCast.Hit && newViewCast.Hit && edgeDstThresholdExceeded))
          {
            EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
            if (edge.PointA != Vector3.zero)
            {
              viewPoints.Add(edge.PointA);
            }

            if (edge.PointB != Vector3.zero)
            {
              viewPoints.Add(edge.PointB);
            }
          }
        }

        viewPoints.Add(newViewCast.Point);
        oldViewCast = newViewCast;
      }

      int vertexCount = viewPoints.Count + 1;
      Vector3[] vertices = new Vector3[vertexCount];
      int[] triangles = new int[(vertexCount - 2) * 3];

      vertices[0] = Vector3.zero;
      for (int i = 0; i < vertexCount - 1; i++)
      {
        vertices[i + 1] = transform.parent.InverseTransformPoint(viewPoints[i]);

        if (i < vertexCount - 2)
        {
          triangles[i * 3] = 0;
          triangles[i * 3 + 1] = i + 1;
          triangles[i * 3 + 2] = i + 2;
        }
      }

      _mesh.Clear();
      _mesh.vertices = vertices;
      _mesh.triangles = triangles;
      _mesh.RecalculateNormals();
    }

    private EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
      float minAngle = minViewCast.Angle;
      float maxAngle = maxViewCast.Angle;
      Vector3 minPoint = Vector3.zero;
      Vector3 maxPoint = Vector3.zero;

      for (int i = 0; i < _meshResolveIterations; i++)
      {
        float angle = (minAngle + maxAngle) / 2;
        ViewCastInfo newViewCast = ViewCast(angle);

        bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.Dist - newViewCast.Dist) > _edgeDistThreshold;
        if (newViewCast.Hit == minViewCast.Hit && !edgeDstThresholdExceeded)
        {
          minAngle = angle;
          minPoint = newViewCast.Point;
        }
        else
        {
          maxAngle = angle;
          maxPoint = newViewCast.Point;
        }
      }

      return new EdgeInfo(minPoint, maxPoint);
    }

    private ViewCastInfo ViewCast(float globalAngle)
    {
      Frame f = QuantumRunner.Default.Game.Frames.Verified;
      var position = transform.position;
      
      Vector3 dir = DirFromAngle(globalAngle, true);
      FPVector2 fpDir = new FPVector2(FP.FromFloat_UNSAFE(dir.x), FP.FromFloat_UNSAFE(dir.z));
      FPVector2 origin = new FPVector2(FP.FromFloat_UNSAFE(position.x),
        FP.FromFloat_UNSAFE(position.z));

      var hit = f.Physics2D.Raycast(origin, fpDir, FP.FromFloat_UNSAFE(_radius), ObstacleMask);

      if (hit.HasValue)
      {
        Vector3 point = new Vector3((float)hit.Value.Point.X, transform.position.y, (float)hit.Value.Point.Y);
        float distance = (float)hit.Value.CastDistanceNormalized * _radius;
        return new ViewCastInfo(true, point, distance, globalAngle);
      }
      else
      {
        return new ViewCastInfo(false, transform.position + dir * _radius, _radius, globalAngle);
      }
    }

    private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
      if (!angleIsGlobal)
      {
        angleInDegrees += transform.eulerAngles.y;
      }

      return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
  }
}