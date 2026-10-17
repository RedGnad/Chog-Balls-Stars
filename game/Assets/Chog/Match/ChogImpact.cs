using System.Collections.Generic;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// Where a basic attack ends: small pieces fly out, spin and shrink (ChogAttackLook, ChogFxKit). They are
    /// small diamonds in the colours of the object (the look of before), flat flakes that flutter down (the
    /// cloth of a cap, wool, a feather), or small copies of the object itself (coins, stars, leaves, snow).
    /// They are drawn with the one material of the heroes, from small meshes made once for each colour. A
    /// burst is kept once over and played again for the next hit (ChogEffectPool); "pieces" and "speed" make
    /// a larger burst from the same object (a hero that falls).
    /// </summary>
    public sealed class ChogImpact : MonoBehaviour
    {
        private const int MostPieces = 10;
        private const int Pieces = 7;
        private const float Seconds = 0.35f;
        private const float Speed = 3.2f;
        private const float PieceSize = 0.16f;
        // Beyond this many bursts in the air a new one is skipped: the fight is already unreadable.
        private const int MaxLive = 16;

        private static readonly Dictionary<Color32, Mesh> s_meshes = new Dictionary<Color32, Mesh>();
        private static readonly Dictionary<Color32, Mesh> s_flakes = new Dictionary<Color32, Mesh>();
        private static readonly Stack<ChogImpact> s_free = new Stack<ChogImpact>();
        private static int s_live;

        /// <summary>How a burst looks: its kind of pieces, how many, how large and fast, in which colours.</summary>
        public struct Look
        {
            public ImpactPieces Pieces;
            public int Count;
            /// <summary>World size of a piece.</summary>
            public float Size;
            /// <summary>How fast they fly out, over the burst of before.</summary>
            public float Speed;
            /// <summary>How hard they are thrown up, over the burst of before.</summary>
            public float Rise;
            public Color Colour;
            public Color Other;
            /// <summary>Colours taken in turn by the pieces, when given (a rainbow).</summary>
            public Color[] Palette;
            /// <summary>The object the copies are (as large as ChogHeroModel.AttackSize).</summary>
            public Mesh Copy;
        }

        private readonly Vector3[] _velocity = new Vector3[MostPieces];
        private readonly Vector3[] _spin = new Vector3[MostPieces];
        private readonly float[] _size = new float[MostPieces];
        private readonly float[] _phase = new float[MostPieces];
        private Transform[] _pieces;
        private MeshFilter[] _filters;
        private ImpactPieces _kind;
        private int _count;
        private float _age;
        private float _seconds;

        /// <summary>A burst of pieces of that colour at that place.</summary>
        public static void Burst(Vector3 at, Color colour) => Burst(at, colour, 1f, 1f);

        /// <summary>
        /// A burst whose pieces are "scale" times larger and fly "speed" times faster (a hero that falls
        /// bursts larger than a hit). The second colour, when given, paints every other piece.
        /// </summary>
        public static void Burst(Vector3 at, Color colour, float scale, float speed, Color? other = null)
        {
            Burst(at, new Look
            {
                Pieces = ImpactPieces.Diamonds, Count = Pieces, Size = PieceSize * scale, Speed = speed, Rise = 1f,
                Colour = colour, Other = other ?? colour
            });
        }

        /// <summary>A burst of that look at that place.</summary>
        public static void Burst(Vector3 at, in Look look)
        {
            if (!GenesisEffects.Enabled || s_live >= MaxLive || look.Count <= 0)
                return;
            if (look.Pieces == ImpactPieces.Copies && look.Copy == null)
                return;
            var impact = ChogEffectPool.Take(s_free);
            if (impact == null)
            {
                impact = ChogEffectPool.Make("ChogImpact").AddComponent<ChogImpact>();
                impact.Make();
            }
            impact.transform.position = at;
            impact.Play(look);
        }

        private void Make()
        {
            _pieces = new Transform[MostPieces];
            _filters = new MeshFilter[MostPieces];
            for (var i = 0; i < MostPieces; i++)
            {
                var piece = new GameObject("Piece", typeof(MeshFilter), typeof(MeshRenderer));
                piece.transform.SetParent(transform, false);
                var renderer = piece.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = ChogHeroModel.Material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                _filters[i] = piece.GetComponent<MeshFilter>();
                _pieces[i] = piece.transform;
            }
        }

        private void Play(in Look look)
        {
            s_live++;
            _age = 0f;
            _kind = look.Pieces;
            _count = Mathf.Clamp(look.Count, 1, MostPieces);
            var speed = Mathf.Max(0.1f, look.Speed);
            var scale = look.Size / PieceSize;
            switch (_kind)
            {
                case ImpactPieces.Flakes:
                    _seconds = 0.65f * Mathf.Lerp(1f, scale, 0.3f);
                    break;
                case ImpactPieces.Copies:
                    _seconds = 0.55f;
                    break;
                default:
                    _seconds = Seconds * Mathf.Lerp(1f, scale, 0.5f);
                    break;
            }

            Mesh first = null, second = null;
            if (_kind != ImpactPieces.Copies && (look.Palette == null || look.Palette.Length == 0))
            {
                first = _kind == ImpactPieces.Flakes ? FlakeMesh(look.Colour) : PieceMesh(look.Colour);
                second = _kind == ImpactPieces.Flakes ? FlakeMesh(look.Other) : PieceMesh(look.Other);
            }
            for (var i = 0; i < MostPieces; i++)
            {
                var shown = i < _count;
                _pieces[i].gameObject.SetActive(shown);
                if (!shown)
                    continue;
                Mesh mesh;
                if (_kind == ImpactPieces.Copies)
                    mesh = look.Copy;
                else if (look.Palette != null && look.Palette.Length > 0)
                {
                    var colour = look.Palette[i % look.Palette.Length];
                    mesh = _kind == ImpactPieces.Flakes ? FlakeMesh(colour) : PieceMesh(colour);
                }
                else
                    mesh = i % 2 == 0 ? first : second;
                _filters[i].sharedMesh = mesh;

                // A copy is the object at AttackSize: brought to the size of a piece.
                _size[i] = _kind == ImpactPieces.Copies
                    ? look.Size / ChogHeroModel.AttackSize * (0.8f + 0.4f * Random.value)
                    : look.Size * (0.8f + 0.4f * Random.value);
                _pieces[i].localPosition = Vector3.zero;
                _pieces[i].localScale = Vector3.one * _size[i];
                _pieces[i].localRotation = Random.rotation;
                _phase[i] = Random.value * Mathf.PI * 2f;
                var angle = (i + Random.value * 0.6f) * Mathf.PI * 2f / _count;
                var up = (0.9f + Random.value * 0.6f) * Mathf.Max(0.2f, look.Rise);
                _velocity[i] = new Vector3(Mathf.Cos(angle), up, Mathf.Sin(angle)) * Speed * speed;
                var turn = _kind == ImpactPieces.Flakes ? 360f : _kind == ImpactPieces.Copies ? 540f : 720f;
                _spin[i] = Random.insideUnitSphere * turn;
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _seconds)
            {
                s_live = Mathf.Max(0, s_live - 1);
                ChogEffectPool.Give(s_free, this);
                return;
            }
            var dt = Time.deltaTime;
            var t = _age / _seconds;
            for (var i = 0; i < _count; i++)
            {
                float shrink;
                switch (_kind)
                {
                    case ImpactPieces.Flakes:
                        // Cloth and feathers: slowed by the air, falling softly, swaying as they fall.
                        _velocity[i] *= Mathf.Exp(-3f * dt);
                        _velocity[i] += Vector3.down * 3.5f * dt;
                        _pieces[i].localPosition += (_velocity[i] + new Vector3(Mathf.Sin(_age * 9f + _phase[i]) * 0.8f, 0f, 0f)) * dt;
                        shrink = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                        break;
                    case ImpactPieces.Copies:
                        _velocity[i] += Vector3.down * 10f * dt;
                        _pieces[i].localPosition += _velocity[i] * dt;
                        shrink = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                        break;
                    default:
                        _velocity[i] += Vector3.down * 9f * dt;
                        _pieces[i].localPosition += _velocity[i] * dt;
                        shrink = 1f - t;
                        break;
                }
                _pieces[i].Rotate(_spin[i] * dt, Space.Self);
                _pieces[i].localScale = Vector3.one * _size[i] * shrink;
            }
        }

        // A burst destroyed with its scene while in the air no longer counts.
        private void OnDestroy()
        {
            if (gameObject.activeSelf && _age < _seconds && _seconds > 0f)
                s_live = Mathf.Max(0, s_live - 1);
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

        // A flat flake of cloth, a little bent, in one lit colour (the heroes' shader draws both its sides).
        private static Mesh FlakeMesh(Color colour)
        {
            Color32 key = colour;
            if (s_flakes.TryGetValue(key, out var kept) && kept != null)
                return kept;
            var outline = new[]
            {
                new Vector3(-0.5f, 0f, -0.32f), new Vector3(0.05f, 0.08f, -0.5f), new Vector3(0.5f, 0f, -0.1f),
                new Vector3(0.3f, 0.06f, 0.45f), new Vector3(-0.35f, 0f, 0.38f)
            };
            var vertices = new List<Vector3> { new Vector3(0f, 0.04f, 0f) };
            var normals = new List<Vector3> { Vector3.up };
            var triangles = new List<int>();
            foreach (var corner in outline)
            {
                vertices.Add(corner);
                normals.Add(Vector3.up);
            }
            for (var c = 0; c < outline.Length; c++)
            {
                triangles.Add(0);
                triangles.Add(1 + (c + 1) % outline.Length);
                triangles.Add(1 + c);
            }
            var colours = new Color[vertices.Count];
            for (var v = 0; v < colours.Length; v++)
                colours[v] = new Color(colour.r, colour.g, colour.b, 1f);
            var mesh = new Mesh { name = "ChogImpact flake" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.colors = colours;
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            s_flakes[key] = mesh;
            return mesh;
        }
    }
}
