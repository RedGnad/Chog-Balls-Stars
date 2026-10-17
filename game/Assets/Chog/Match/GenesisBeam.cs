using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The laser of a Genesis with laser eyes, drawn the way the collection draws it: two straight
    /// beams, one per eye, each a dark outline, a flat colour and a lighter core. The beams grow
    /// from the hero to the head of the attack, then thin out and disappear. A beam that disappeared is
    /// kept and drawn again for the next one (ChogEffectPool).
    /// </summary>
    public sealed class GenesisBeam : MonoBehaviour
    {
        // Half the distance between the two eyes, and the height of the head, in world units.
        private const float EyeSpacing = 0.22f;
        private const float EyeHeight = 1.15f;

        private const float OutlineWidth = 0.34f;
        private const float ColourWidth = 0.24f;
        private const float CoreWidth = 0.1f;
        // Once the attack is over the beams stay whole for a moment, so the eye can read the line
        // that was hit, then thin out.
        private const float HoldSeconds = 0.3f;
        private const float FadeSeconds = 0.25f;

        private static readonly Stack<GenesisBeam> s_free = new Stack<GenesisBeam>();

        private readonly LineRenderer[] _lines = new LineRenderer[6];
        private readonly float[] _widths = new float[6];
        private Vector3 _origin;
        private float _fadeStarted = -1f;

        /// <summary>"thickness" multiplies the width of the beams: 1 for the laser as the eyes draw it.</summary>
        public static GenesisBeam Create(Material material, GenesisEyeLook look, Vector3 origin, float thickness = 1f)
        {
            var beam = ChogEffectPool.Take(s_free);
            if (beam == null)
                beam = ChogEffectPool.Make("GenesisBeam").AddComponent<GenesisBeam>();
            beam._origin = origin + Vector3.up * EyeHeight;
            beam._fadeStarted = -1f;

            // Drawn back to front for each eye: outline, colour, core.
            for (var eye = 0; eye < 2; eye++)
            {
                beam.AddLine(eye * 3, material, look.Outline, OutlineWidth * thickness, 0);
                beam.AddLine(eye * 3 + 1, material, look.Outer, ColourWidth * thickness, 1);
                beam.AddLine(eye * 3 + 2, material, look.Core, CoreWidth * thickness, 2);
            }

            beam.SetHead(origin);
            return beam;
        }

        /// <summary>Moves the far end of the beams to where the attack is now.</summary>
        public void SetHead(Vector3 head)
        {
            head += Vector3.up * EyeHeight;
            var along = head - _origin;
            along.y = 0f;
            var side = along.sqrMagnitude < 0.0001f ? Vector3.zero : Vector3.Cross(Vector3.up, along.normalized) * EyeSpacing;

            for (var i = 0; i < _lines.Length; i++)
            {
                var offset = i < 3 ? side : -side;
                _lines[i].SetPosition(0, _origin + offset);
                _lines[i].SetPosition(1, head + offset);
            }
        }

        /// <summary>The attack is over: the beams thin out, then this object removes itself.</summary>
        public void Release() => _fadeStarted = Time.time;

        private void Update()
        {
            if (_fadeStarted < 0f)
                return;

            var left = Mathf.Clamp01(1f - (Time.time - _fadeStarted - HoldSeconds) / FadeSeconds);
            if (left <= 0f)
            {
                ChogEffectPool.Give(s_free, this);
                return;
            }

            for (var i = 0; i < _lines.Length; i++)
                _lines[i].widthMultiplier = _widths[i] * left;
        }

        // The line at that place, made once for a beam and set again each time it is drawn.
        private void AddLine(int index, Material material, Color color, float width, int order)
        {
            var line = _lines[index];
            if (line == null)
            {
                var child = new GameObject("Line");
                child.transform.SetParent(transform, false);
                line = child.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.numCapVertices = 3;
                line.alignment = LineAlignment.View;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
            }
            line.sharedMaterial = material;
            line.startColor = color;
            line.endColor = color;
            line.widthMultiplier = width;
            line.sortingOrder = order;
            _lines[index] = line;
            _widths[index] = width;
        }
    }
}
