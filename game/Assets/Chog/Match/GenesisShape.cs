using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// Flat cartoon shapes an attack is drawn with, built from the colours of the caster's eyes
    /// instead of a particle effect: the fan of light of a visor or a pair of glasses, the tear
    /// that is lobbed, the puddle of tears it leaves, the eyes themselves for a stare. A few
    /// triangles each, one unlit material. A released shape shrinks away, then is kept to draw the
    /// next shape of its kind (ChogEffectPool): its objects and its meshes are filled again instead
    /// of being made anew. The pictures that face the camera (eyes, signs) are kept per look and size.
    /// </summary>
    public sealed class GenesisShape : MonoBehaviour
    {
        private const int ArcSegments = 12;
        private const int CircleSegments = 24;
        private const float GroundHeight = 0.12f;
        private const float ShrinkSeconds = 0.2f;

        // A sign pops in from this share of its size, in this time.
        private const float PopFrom = 0.35f;
        private const float PopSeconds = 0.12f;

        // The pictures that face the camera are few in a match (six heroes, a dozen signs); beyond this many
        // the cache is not grown, and a picture is made for its one shape.
        private const int MaxCachedPictures = 256;

        // What a pooled shape is made of, which decides the pool it goes back to.
        private enum Kind
        {
            Flat,   // one mesh of its own and an outline: a fan or a disc on the ground
            Drop,   // three discs of its own that face the camera
            Facing  // one picture shared by every shape of the same look
        }

        private static readonly Stack<GenesisShape> s_flat = new Stack<GenesisShape>();
        private static readonly Stack<GenesisShape> s_drop = new Stack<GenesisShape>();
        private static readonly Stack<GenesisShape> s_facing = new Stack<GenesisShape>();
        private static readonly Dictionary<PictureKey, Mesh> s_pictures = new Dictionary<PictureKey, Mesh>();

        private Kind _kind;
        private float _shrinkStarted = -1f;
        private float _popStarted = -1f;
        private float _releaseAt = -1f;
        private Vector3 _fullScale;
        private Transform _camera;
        private bool _facesCamera;

        // The parts a pooled shape keeps between two uses.
        private MeshFilter _filter;
        private MeshRenderer _renderer;
        private LineRenderer _outline;
        private MeshFilter[] _discs;
        // A picture made for this one shape (the cache was full), destroyed when it is replaced.
        private Mesh _ownPicture;

        /// <summary>
        /// A fan on the ground from "apex" towards "direction": the main colour on one side, the
        /// other colour on the other side, the core colour down the middle.
        /// </summary>
        public static GenesisShape Fan(Material material, GenesisEyeLook look, Vector3 apex, Vector3 direction,
                                       float radius, float halfAngleDegrees)
        {
            var right = look.Other.a > 0f ? look.Other : look.Outer;
            var vertices = new Vector3[ArcSegments + 2];
            var colors = new Color[ArcSegments + 2];
            var triangles = new int[ArcSegments * 3];
            vertices[0] = Vector3.zero;
            colors[0] = WithAlpha(look.Core, 0.95f);
            for (var i = 0; i <= ArcSegments; i++)
            {
                var along = i / (float)ArcSegments;
                var angle = Mathf.Lerp(-halfAngleDegrees, halfAngleDegrees, along) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;

                // One side, the bright middle, the other side: the gradient of the visor.
                var color = along < 0.5f
                    ? Color.Lerp(look.Outer, look.Core, along * 2f)
                    : Color.Lerp(look.Core, right, along * 2f - 1f);
                colors[i + 1] = WithAlpha(color, 0.8f);
            }

            for (var i = 0; i < ArcSegments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            var shape = Flat("GenesisFan", material, vertices, colors, triangles);
            shape.transform.position = apex + Vector3.up * GroundHeight;
            direction.y = 0f;
            shape.transform.rotation = direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : Quaternion.identity;
            shape.SetOutline(material, look.Outline, vertices, 0.1f, true);
            return shape;
        }

        /// <summary>A disc on the ground: the puddle a zone leaves, lighter in its middle.</summary>
        public static GenesisShape GroundDisc(Material material, GenesisEyeLook look, Vector3 center, float radius)
        {
            var vertices = Circle(radius, true, out var triangles);
            var colors = new Color[vertices.Length];
            colors[0] = WithAlpha(look.Core, 0.6f);
            for (var i = 1; i < colors.Length; i++)
                colors[i] = WithAlpha(look.Outer, 0.45f);

            var shape = Flat("GenesisGroundDisc", material, vertices, colors, triangles);
            shape.transform.position = center + Vector3.up * GroundHeight;
            shape.transform.rotation = Quaternion.identity;
            shape.SetOutline(material, look.Outline, vertices, 0.14f, false);
            return shape;
        }

        /// <summary>A round drop that always faces the camera: outline, colour and a lighter heart.</summary>
        public static GenesisShape Drop(Material material, GenesisEyeLook look, Vector3 position, float radius)
        {
            var shape = ChogEffectPool.Take(s_drop);
            if (shape == null)
            {
                shape = ChogEffectPool.Make("GenesisDrop").AddComponent<GenesisShape>();
                shape._kind = Kind.Drop;
                shape._discs = new MeshFilter[3];
            }
            shape.Reset(true);
            shape.SetDisc(0, material, look.Outline, radius);
            shape.SetDisc(1, material, look.Outer, radius * 0.8f);
            shape.SetDisc(2, material, look.Core, radius * 0.4f);
            shape.transform.position = position;
            return shape;
        }

        /// <summary>The hero's own pair of eyes on a strip of its skin, always facing the camera.</summary>
        public static GenesisShape EyePair(Material material, GenesisEyeLook look, Color skin, Vector3 position, float halfWidth)
        {
            var key = new PictureKey(1, look, skin, halfWidth, 0);
            var picture = Picture(key, out var shared);
            if (picture == null)
            {
                picture = GenesisEyes.Pair(look, skin, halfWidth);
                if (shared)
                    s_pictures[key] = picture;
            }
            return Facing("GenesisEyePair", material, picture, shared, position);
        }

        /// <summary>One eye of the hero on a disc of its skin, always facing the camera.</summary>
        public static GenesisShape EyeBadge(Material material, GenesisEyeLook look, Color skin, Vector3 position, float radius,
                                            bool second)
        {
            var key = new PictureKey(second ? 3 : 2, look, skin, radius, 0);
            var picture = Picture(key, out var shared);
            if (picture == null)
            {
                picture = GenesisEyes.Single(look, skin, radius, second);
                if (shared)
                    s_pictures[key] = picture;
            }
            return Facing("GenesisEyeBadge", material, picture, shared, position);
        }

        /// <summary>
        /// The sign of an Aura effect, always facing the camera. It pops in; with "seconds" above
        /// zero it leaves by itself after that long.
        /// </summary>
        public static GenesisShape Icon(Material material, GenesisEffectIcon icon, Color color, Vector3 position, float size,
                                        float seconds)
        {
            var key = new PictureKey(4, new GenesisEyeLook { Outer = color }, Color.clear, size, (int)icon);
            var picture = Picture(key, out var shared);
            if (picture == null)
            {
                picture = GenesisIcons.Mesh(icon, color, size);
                if (shared)
                    s_pictures[key] = picture;
            }
            var shape = Facing("GenesisIcon", material, picture, shared, position);
            shape._popStarted = Time.time;
            shape._releaseAt = seconds > 0f ? Time.time + seconds : -1f;
            shape.transform.localScale = shape._fullScale * PopFrom;
            return shape;
        }

        /// <summary>Moves a shape that lies on the ground to above that point.</summary>
        public void FollowOnGround(Vector3 point)
        {
            transform.position = new Vector3(point.x, point.y + GroundHeight, point.z);
        }

        /// <summary>The attack is over: the shape shrinks away, then is kept for the next one.</summary>
        public void Release()
        {
            if (_shrinkStarted < 0f)
                _shrinkStarted = Time.time;
        }

        private void LateUpdate()
        {
            if (_facesCamera)
            {
                if (_camera == null && Camera.main != null)
                    _camera = Camera.main.transform;
                if (_camera != null)
                    transform.rotation = _camera.rotation;
            }

            if (_releaseAt >= 0f && Time.time >= _releaseAt)
            {
                _releaseAt = -1f;
                Release();
            }

            if (_shrinkStarted < 0f)
            {
                if (_popStarted < 0f)
                    return;

                var grown = (Time.time - _popStarted) / PopSeconds;
                if (grown >= 1f)
                    _popStarted = -1f;
                transform.localScale = _fullScale * Mathf.Lerp(PopFrom, 1f, Mathf.Clamp01(grown));
                return;
            }

            var left = 1f - (Time.time - _shrinkStarted) / ShrinkSeconds;
            if (left <= 0f)
            {
                Recycle();
                return;
            }

            transform.localScale = _fullScale * left;
        }

        private void Recycle()
        {
            switch (_kind)
            {
                case Kind.Flat:
                    ChogEffectPool.Give(s_flat, this);
                    break;
                case Kind.Drop:
                    ChogEffectPool.Give(s_drop, this);
                    break;
                default:
                    ChogEffectPool.Give(s_facing, this);
                    break;
            }
        }

        // A shape back at its start: full size, not shrinking, not popping.
        private void Reset(bool facesCamera)
        {
            _shrinkStarted = -1f;
            _popStarted = -1f;
            _releaseAt = -1f;
            _facesCamera = facesCamera;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            _fullScale = Vector3.one;
        }

        // A fan or a disc on the ground: a pooled one, its own mesh filled with these.
        private static GenesisShape Flat(string name, Material material, Vector3[] vertices, Color[] colors, int[] triangles)
        {
            var shape = ChogEffectPool.Take(s_flat);
            if (shape == null)
            {
                var host = ChogEffectPool.Make(name, typeof(MeshFilter), typeof(MeshRenderer));
                shape = host.AddComponent<GenesisShape>();
                shape._kind = Kind.Flat;
                shape._filter = host.GetComponent<MeshFilter>();
                shape._renderer = host.GetComponent<MeshRenderer>();
                shape._filter.sharedMesh = new Mesh { name = "GenesisFlatShape" };
            }
            shape.name = name;
            shape.Reset(false);
            Fill(shape._filter.sharedMesh, vertices, colors, triangles);
            Configure(shape._renderer, material, 0);
            return shape;
        }

        private static GenesisShape Facing(string name, Material material, Mesh picture, bool shared, Vector3 position)
        {
            var shape = ChogEffectPool.Take(s_facing);
            if (shape == null)
            {
                var host = ChogEffectPool.Make(name, typeof(MeshFilter), typeof(MeshRenderer));
                shape = host.AddComponent<GenesisShape>();
                shape._kind = Kind.Facing;
                shape._filter = host.GetComponent<MeshFilter>();
                shape._renderer = host.GetComponent<MeshRenderer>();
            }
            shape.name = name;
            shape.Reset(true);
            if (shape._ownPicture != null && shape._ownPicture != picture)
                Destroy(shape._ownPicture);
            shape._ownPicture = shared ? null : picture;
            shape._filter.sharedMesh = picture;
            Configure(shape._renderer, material, 2);
            shape.transform.position = position;
            return shape;
        }

        // The picture kept for that key ("shared" true), or null, with "shared" telling whether the one the
        // caller makes next is to be kept under that key.
        private static Mesh Picture(PictureKey key, out bool shared)
        {
            if (s_pictures.TryGetValue(key, out var kept) && kept != null)
            {
                shared = true;
                return kept;
            }
            shared = s_pictures.Count < MaxCachedPictures;
            return null;
        }

        private void OnDestroy()
        {
            // The meshes this shape filled for itself go with it; the shared pictures stay.
            if (_kind == Kind.Flat && _filter != null && _filter.sharedMesh != null)
                Destroy(_filter.sharedMesh);
            if (_discs != null)
                foreach (var disc in _discs)
                    if (disc != null && disc.sharedMesh != null)
                        Destroy(disc.sharedMesh);
            if (_ownPicture != null)
                Destroy(_ownPicture);
        }

        private void SetDisc(int index, Material material, Color color, float radius)
        {
            var disc = _discs[index];
            if (disc == null)
            {
                var child = new GameObject("Disc", typeof(MeshFilter), typeof(MeshRenderer));
                child.transform.SetParent(transform, false);
                disc = child.GetComponent<MeshFilter>();
                disc.sharedMesh = new Mesh { name = "GenesisDropDisc" };
                _discs[index] = disc;
            }

            var vertices = Circle(radius, false, out var triangles);
            var colors = new Color[vertices.Length];
            for (var i = 0; i < colors.Length; i++)
                colors[i] = WithAlpha(color, 1f);
            Fill(disc.sharedMesh, vertices, colors, triangles);
            Configure(disc.GetComponent<MeshRenderer>(), material, index);
        }

        // The outline follows the rim of the shape; a fan closes back on its apex.
        private void SetOutline(Material material, Color color, Vector3[] vertices, float width, bool fromApex)
        {
            if (_outline == null)
            {
                var child = new GameObject("Outline");
                child.transform.SetParent(transform, false);
                _outline = child.AddComponent<LineRenderer>();
                _outline.useWorldSpace = false;
                _outline.loop = true;
                _outline.alignment = LineAlignment.TransformZ;
                child.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                _outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _outline.receiveShadows = false;
                _outline.sortingOrder = 1;
            }

            var line = _outline;
            line.sharedMaterial = material;
            line.startColor = color;
            line.endColor = color;
            line.widthMultiplier = width;

            var first = fromApex ? 0 : 1;
            line.positionCount = vertices.Length - first;
            for (var i = first; i < vertices.Length; i++)
            {
                // The line object is turned to lie flat: the shape's z is its y.
                var point = vertices[i];
                line.SetPosition(i - first, new Vector3(point.x, point.z, 0f));
            }
        }

        private static void Fill(Mesh mesh, Vector3[] vertices, Color[] colors, int[] triangles)
        {
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        // A filled circle around the origin: on the ground (x, z) or upright (x, y).
        private static Vector3[] Circle(float radius, bool onGround, out int[] triangles)
        {
            var vertices = new Vector3[CircleSegments + 1];
            triangles = new int[CircleSegments * 3];
            vertices[0] = Vector3.zero;
            for (var i = 0; i < CircleSegments; i++)
            {
                var angle = i / (float)CircleSegments * Mathf.PI * 2f;
                var x = Mathf.Sin(angle) * radius;
                var other = Mathf.Cos(angle) * radius;
                vertices[i + 1] = onGround ? new Vector3(x, 0f, other) : new Vector3(x, other, 0f);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i == CircleSegments - 1 ? 1 : i + 2;
            }

            return vertices;
        }

        private static void Configure(MeshRenderer renderer, Material material, int order)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = order;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        // What a picture that faces the camera is drawn from: its kind, the colours of the look and the skin,
        // its size in hundredths and one more number (the sign of an icon).
        private readonly struct PictureKey : IEquatable<PictureKey>
        {
            private readonly int _kind, _outline, _outer, _core, _other, _glyph, _skin, _size, _extra;

            public PictureKey(int kind, GenesisEyeLook look, Color skin, float size, int extra)
            {
                _kind = kind;
                _outline = Pack(look.Outline);
                _outer = Pack(look.Outer);
                _core = Pack(look.Core);
                _other = Pack(look.Other);
                _glyph = (int)look.Glyph;
                _skin = Pack(skin);
                _size = Mathf.RoundToInt(size * 100f);
                _extra = extra;
            }

            private static int Pack(Color colour)
            {
                Color32 c = colour;
                return (c.r << 24) | (c.g << 16) | (c.b << 8) | c.a;
            }

            public bool Equals(PictureKey other) =>
                _kind == other._kind && _outline == other._outline && _outer == other._outer && _core == other._core
                && _other == other._other && _glyph == other._glyph && _skin == other._skin && _size == other._size
                && _extra == other._extra;

            public override bool Equals(object obj) => obj is PictureKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = _kind;
                    hash = hash * 31 + _outline;
                    hash = hash * 31 + _outer;
                    hash = hash * 31 + _core;
                    hash = hash * 31 + _other;
                    hash = hash * 31 + _glyph;
                    hash = hash * 31 + _skin;
                    hash = hash * 31 + _size;
                    return hash * 31 + _extra;
                }
            }
        }
    }
}
