using System.Collections.Generic;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The lights of a fight, faked: a real light per blow would relight the whole arena and cost a phone a
    /// pass per object, and the heroes' matcap does not read lights anyway. Instead a flash adds its colour
    /// over what is behind it (ChogEffectMaterials.Light): a flare turned to the camera, white-hot in its
    /// middle, with rays, and the pool of light it casts on the floor; a ring of light that runs out along
    /// the floor (a shock wave); a column of light a hero comes back in. On a computer the glow of the match
    /// (ChogMatchPost) makes them bloom. Each one, once over, is kept for the next (ChogEffectPool); beyond
    /// a cap a new one is skipped.
    /// </summary>
    public sealed class ChogLight : MonoBehaviour
    {
        private enum Kind : byte { Flash, Ring, Column }

        private const int MaxLive = 40;
        private const int MaxLiveLighter = 16;
        // A flash reaches its size in this share of its life, then fades.
        private const float FlashRise = 0.12f;

        private static readonly Stack<ChogLight> s_free = new Stack<ChogLight>();
        private static readonly Dictionary<(Color32, int), Mesh> s_flares = new Dictionary<(Color32, int), Mesh>();
        private static readonly Dictionary<Color32, Mesh> s_pools = new Dictionary<Color32, Mesh>();
        private static readonly Dictionary<(Color32, int), Mesh> s_rings = new Dictionary<(Color32, int), Mesh>();
        private static readonly Dictionary<Color32, Mesh> s_columns = new Dictionary<Color32, Mesh>();
        private static int s_live;
        private static int s_shown;
        private static int s_skipped;

        private EffectPiece _main;
        private EffectPiece _floor;
        private Kind _kind;
        private Vector3 _at;
        private float _size;
        private float _height;
        private float _seconds;
        private float _age;
        private float _turn;

        /// <summary>One line for the performance overlay.</summary>
        public static string Status => "lights live " + s_live + "  shown " + s_shown + "  skipped " + s_skipped;

        /// <summary>
        /// A flash of light at "at" (in the air): a flare "size" wide with "rays" rays, and, with "floor", the
        /// pool of light it casts on the floor under it; it pops in and fades in "seconds".
        /// </summary>
        public static void Flash(Vector3 at, Color colour, float size, float seconds = 0.3f, int rays = 0, bool floor = true)
        {
            var light = Take();
            if (light == null)
                return;
            light._kind = Kind.Flash;
            light._at = at;
            light._size = size;
            light._seconds = Mathf.Max(0.05f, seconds);
            light._turn = Random.value * 360f;
            light._main.Show(Flare(colour, rays), ChogEffectMaterials.Light);
            if (floor)
                light._floor.Show(Pool(colour), ChogEffectMaterials.Light);
            else
                light._floor.Hide();
            light.Draw();
        }

        /// <summary>A ring of light that runs out along the floor from "at" to "radius" in "seconds" (a shock wave).</summary>
        public static void Ring(Vector3 at, Color colour, float radius, float seconds = 0.45f, float width = 0.22f)
        {
            var light = Take();
            if (light == null)
                return;
            light._kind = Kind.Ring;
            light._at = GenesisSpecial3D.OnFloor(at) + Vector3.up * 0.02f;
            light._size = radius;
            light._seconds = Mathf.Max(0.05f, seconds);
            light._main.Show(RingMesh(colour, width), ChogEffectMaterials.Light);
            light._floor.Hide();
            light.Draw();
        }

        /// <summary>A column of light "radius" wide and "height" high standing on the floor at "at", that rises and thins out.</summary>
        public static void Column(Vector3 at, Color colour, float radius, float height, float seconds = 0.6f)
        {
            var light = Take();
            if (light == null)
                return;
            light._kind = Kind.Column;
            light._at = GenesisSpecial3D.OnFloor(at);
            light._size = radius;
            light._height = height;
            light._seconds = Mathf.Max(0.05f, seconds);
            light._main.Show(ColumnMesh(colour), ChogEffectMaterials.Light);
            light._floor.Show(Pool(colour), ChogEffectMaterials.Light);
            light.Draw();
        }

        private static ChogLight Take()
        {
            if (!GenesisEffects.Enabled || GenesisEffects.Level == GenesisEffects.FxLevel.Before || ChogEffectMaterials.Light == null)
                return null;
            if (s_live >= (GenesisEffects.Lighter ? MaxLiveLighter : MaxLive))
            {
                s_skipped++;
                return null;
            }
            var light = ChogEffectPool.Take(s_free);
            if (light == null)
            {
                light = ChogEffectPool.Make("ChogLight").AddComponent<ChogLight>();
                light._main = new EffectPiece(light.transform, "Light");
                light._floor = new EffectPiece(light.transform, "Floor Light");
            }
            light._age = 0f;
            s_live++;
            s_shown++;
            return light;
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
            Draw();
        }

        // A light destroyed with its scene while shown no longer counts.
        private void OnDestroy()
        {
            if (gameObject.activeSelf && _age < _seconds)
                s_live = Mathf.Max(0, s_live - 1);
        }

        private void Draw()
        {
            var t = Mathf.Clamp01(_age / _seconds);
            switch (_kind)
            {
                case Kind.Flash:
                {
                    // In at once and a little past its size, then fading as it widens.
                    var rise = Mathf.Clamp01(t / FlashRise);
                    var fade = t < FlashRise ? 1f : 1f - (t - FlashRise) / (1f - FlashRise);
                    fade *= fade;
                    var size = _size * (0.55f + 0.45f * GenesisSpecial3D.Pop(rise)) * (1f + 0.25f * t);
                    var toward = GenesisSpecial3D.TowardViewer(_at);
                    var facing = Quaternion.LookRotation(-toward, GenesisSpecial3D.UpOnScreen()) * Quaternion.Euler(0f, 0f, _turn + 40f * t);
                    _main.Place(_at, facing, Vector3.one * size);
                    _main.Fade(fade);
                    var ground = GenesisSpecial3D.OnFloor(_at) + Vector3.up * 0.01f;
                    // The higher the flash, the softer and wider its light on the floor.
                    var lift = Mathf.Clamp01((_at.y - ground.y) / 3f);
                    _floor.Place(ground, Quaternion.identity, Vector3.one * (size * (1.3f + lift)));
                    _floor.Fade(fade * (1f - 0.4f * lift));
                    break;
                }
                case Kind.Ring:
                {
                    var spread = GenesisSpecial3D.EaseOut(t);
                    _main.Place(_at, Quaternion.identity, Vector3.one * Mathf.Max(0.01f, _size * (0.15f + 0.85f * spread)));
                    var left = 1f - t;
                    _main.Fade(left * left);
                    break;
                }
                default:
                {
                    // Up in a tenth of its life, then thinning out as it fades.
                    var up = Mathf.Clamp01(t / 0.15f);
                    var left = 1f - t;
                    var radius = _size * Mathf.Lerp(1f, 0.35f, t);
                    _main.Place(_at, Quaternion.identity, new Vector3(radius, _height * GenesisSpecial3D.EaseOut(up), radius));
                    _main.Fade(left);
                    _floor.Place(_at + Vector3.up * 0.01f, Quaternion.identity, Vector3.one * (_size * 2.2f));
                    _floor.Fade(left * left);
                    break;
                }
            }
        }

        private static Mesh Flare(Color colour, int rays)
        {
            var key = ((Color32)colour, rays);
            if (!s_flares.TryGetValue(key, out var mesh) || mesh == null)
                s_flares[key] = mesh = ChogFxMeshes.FlareDraft(colour, rays).ToMesh("ChogFlare");
            return mesh;
        }

        private static Mesh Pool(Color colour)
        {
            Color32 key = colour;
            if (!s_pools.TryGetValue(key, out var mesh) || mesh == null)
                s_pools[key] = mesh = ChogFxMeshes.PoolDraft(colour).ToMesh("ChogLightPool");
            return mesh;
        }

        private static Mesh RingMesh(Color colour, float width)
        {
            var key = ((Color32)colour, Mathf.RoundToInt(width * 100f));
            if (!s_rings.TryGetValue(key, out var mesh) || mesh == null)
                s_rings[key] = mesh = ChogFxMeshes.RingDraft(colour, width).ToMesh("ChogLightRing");
            return mesh;
        }

        private static Mesh ColumnMesh(Color colour)
        {
            Color32 key = colour;
            if (!s_columns.TryGetValue(key, out var mesh) || mesh == null)
                s_columns[key] = mesh = ChogFxMeshes.ColumnDraft(colour).ToMesh("ChogLightColumn");
            return mesh;
        }
    }
}
