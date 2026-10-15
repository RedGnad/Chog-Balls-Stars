using System.Collections.Generic;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// Where a basic attack ends: a few small pieces in the colour of its object fly out, spin and shrink
    /// in a third of a second (ChogAttackLook). They are drawn with the one material of the heroes, from a
    /// small mesh made once for each colour; a burst is a handful of objects that remove themselves.
    /// </summary>
    public sealed class ChogImpact : MonoBehaviour
    {
        private const int Pieces = 7;
        private const float Seconds = 0.35f;
        private const float Speed = 3.2f;
        private const float PieceSize = 0.16f;

        private static readonly Dictionary<Color32, Mesh> s_meshes = new Dictionary<Color32, Mesh>();

        private readonly Vector3[] _velocity = new Vector3[Pieces];
        private readonly Vector3[] _spin = new Vector3[Pieces];
        private Transform[] _pieces;
        private float _age;

        /// <summary>A burst of pieces of that colour at that place.</summary>
        public static void Burst(Vector3 at, Color colour)
        {
            if (!GenesisEffects.Enabled)
                return;
            var host = new GameObject("ChogImpact");
            host.transform.position = at;
            host.AddComponent<ChogImpact>().Build(colour);
        }

        private void Build(Color colour)
        {
            var mesh = PieceMesh(colour);
            _pieces = new Transform[Pieces];
            for (var i = 0; i < Pieces; i++)
            {
                var piece = new GameObject("Piece", typeof(MeshFilter), typeof(MeshRenderer));
                piece.transform.SetParent(transform, false);
                piece.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = piece.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = ChogHeroModel.Material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                piece.transform.localScale = Vector3.one * PieceSize;
                piece.transform.localRotation = Random.rotation;
                var angle = (i + Random.value * 0.6f) * Mathf.PI * 2f / Pieces;
                _velocity[i] = new Vector3(Mathf.Cos(angle), 0.9f + Random.value * 0.6f, Mathf.Sin(angle)) * Speed;
                _spin[i] = Random.insideUnitSphere * 720f;
                _pieces[i] = piece.transform;
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= Seconds)
            {
                Destroy(gameObject);
                return;
            }
            var left = 1f - _age / Seconds;
            for (var i = 0; i < Pieces; i++)
            {
                _velocity[i] += Vector3.down * 9f * Time.deltaTime;
                _pieces[i].localPosition += _velocity[i] * Time.deltaTime;
                _pieces[i].Rotate(_spin[i] * Time.deltaTime, Space.Self);
                _pieces[i].localScale = Vector3.one * PieceSize * left;
            }
        }

        // A small diamond (an octahedron) in one lit colour.
        private static Mesh PieceMesh(Color colour)
        {
            Color32 key = colour;
            if (s_meshes.TryGetValue(key, out var kept) && kept != null)
                return kept;
            var corners = new[] { Vector3.up, Vector3.right, Vector3.forward, Vector3.left, Vector3.back, Vector3.down };
            var faces = new[] { 0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 1, 5, 2, 1, 5, 3, 2, 5, 4, 3, 5, 1, 4 };
            var vertices = new Vector3[faces.Length];
            var normals = new Vector3[faces.Length];
            var colours = new Color[faces.Length];
            var triangles = new int[faces.Length];
            for (var f = 0; f < faces.Length; f += 3)
            {
                var a = corners[faces[f]];
                var b = corners[faces[f + 1]];
                var c = corners[faces[f + 2]];
                var normal = (a + b + c).normalized;
                for (var k = 0; k < 3; k++)
                {
                    vertices[f + k] = corners[faces[f + k]] * 0.5f;
                    normals[f + k] = normal;
                    colours[f + k] = new Color(colour.r, colour.g, colour.b, 1f);
                    triangles[f + k] = f + k;
                }
            }
            var mesh = new Mesh { name = "ChogImpact piece", vertices = vertices, normals = normals, colors = colours, triangles = triangles };
            mesh.RecalculateBounds();
            s_meshes[key] = mesh;
            return mesh;
        }
    }
}
