using System.Collections.Generic;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// A bolt of lightning between two points, added over the scene as light (ChogEffectMaterials.Light): a
    /// wide band of its colour with a thin white-hot core, jagged, drawn again with a new shape about twenty
    /// times a second so that it flickers, with a fork or two on a computer. Thrown by a Thunder Cap, around
    /// a target that a haste special hit, from the eyes of a haste hero that fires, from a falling core. Once
    /// over it is kept for the next (ChogEffectPool); beyond a cap a new one is skipped.
    /// </summary>
    public sealed class ChogLightning : MonoBehaviour
    {
        private const int MaxLive = 10;
        private const int MaxLiveLighter = 4;
        private const float FlickerSeconds = 0.05f;
        private const float UnitsPerSegment = 0.45f;

        private static readonly Stack<ChogLightning> s_free = new Stack<ChogLightning>();
        private static readonly System.Random s_random = new System.Random(1287);
        private static int s_live;

        private readonly EffectMeshDraft _draft = new EffectMeshDraft();
        private readonly List<Vector3> _path = new List<Vector3>();
        private readonly List<Vector3> _fork = new List<Vector3>();
        private EffectPiece _piece;
        private Mesh _mesh;
        private Vector3 _from;
        private Vector3 _to;
        private Color _colour;
        private float _width;
        private int _forks;
        private float _seconds;
        private float _age;
        private float _nextShape;

        /// <summary>
        /// A bolt from "from" to "to" in "colour", "width" wide at its core, for "seconds"; "forks" side branches
        /// (none on a phone).
        /// </summary>
        public static void Strike(Vector3 from, Vector3 to, Color colour, float seconds = 0.25f, float width = 0.12f, int forks = 1)
        {
            if (!GenesisEffects.Enabled || GenesisEffects.Level == GenesisEffects.FxLevel.Before || ChogEffectMaterials.Light == null)
                return;
            if (s_live >= (GenesisEffects.Lighter ? MaxLiveLighter : MaxLive))
                return;
            var bolt = ChogEffectPool.Take(s_free);
            if (bolt == null)
            {
                bolt = ChogEffectPool.Make("ChogLightning").AddComponent<ChogLightning>();
                bolt._piece = new EffectPiece(bolt.transform, "Bolt");
                bolt._mesh = new Mesh { name = "ChogLightning" };
                bolt._mesh.MarkDynamic();
            }
            s_live++;
            bolt._from = from;
            bolt._to = to;
            bolt._colour = colour;
            bolt._width = width;
            bolt._forks = GenesisEffects.Lighter ? 0 : forks;
            bolt._seconds = Mathf.Max(0.05f, seconds);
            bolt._age = 0f;
            bolt._nextShape = 0f;
            bolt._piece.Show(bolt._mesh, ChogEffectMaterials.Light);
            bolt._piece.Place(Vector3.zero, Quaternion.identity, Vector3.one);
            bolt.Shape();
        }

        private void LateUpdate()
        {
            _age += Time.deltaTime;
            if (_age >= _seconds)
            {
                s_live = Mathf.Max(0, s_live - 1);
                ChogEffectPool.Give(s_free, this);
                return;
            }
            if (_age >= _nextShape)
                Shape();
            // Bright at once, flickering, then gone.
            var left = 1f - _age / _seconds;
            var flicker = 0.65f + 0.35f * (float)s_random.NextDouble();
            _piece.Fade(Mathf.Clamp01(left * 1.6f) * flicker);
        }

        private void OnDestroy()
        {
            if (gameObject.activeSelf && _age < _seconds)
                s_live = Mathf.Max(0, s_live - 1);
            if (_mesh != null)
                Destroy(_mesh);
        }

        // A new jagged shape for the bolt and its forks, in world space (the piece stays at the origin).
        private void Shape()
        {
            _nextShape = _age + FlickerSeconds;
            var length = Vector3.Distance(_from, _to);
            var segments = Mathf.Clamp(Mathf.CeilToInt(length / UnitsPerSegment), 3, GenesisEffects.Lighter ? 8 : 14);
            var jitter = Mathf.Min(0.6f, 0.12f + length * 0.08f);
            ChogFxMeshes.BoltPath(_path, _from, _to, segments, jitter, s_random);
            var middle = (_from + _to) * 0.5f;
            var toViewer = GenesisSpecial3D.TowardViewer(middle);
            var hot = Color.Lerp(_colour, Color.white, 0.8f);
            _draft.Clear();
            ChogFxMeshes.Ribbon(_draft, _path, toViewer, _width * 4f, _colour, 0.55f);
            ChogFxMeshes.Ribbon(_draft, _path, toViewer, _width, hot, 1f);
            for (var f = 0; f < _forks && _path.Count > 3; f++)
            {
                // A fork leaves from a point of the bolt and goes off to one side, shorter.
                var root = _path[1 + s_random.Next(_path.Count - 2)];
                var away = (_to - _from).normalized + Random.insideUnitSphere * 0.9f;
                var end = root + away.normalized * (length * 0.3f);
                ChogFxMeshes.BoltPath(_fork, root, end, 4, jitter * 0.6f, s_random);
                ChogFxMeshes.Ribbon(_draft, _fork, toViewer, _width * 2.4f, _colour, 0.4f);
                ChogFxMeshes.Ribbon(_draft, _fork, toViewer, _width * 0.6f, hot, 0.85f);
            }
            _draft.Fill(_mesh);
        }
    }
}
