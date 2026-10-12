using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// Flat cartoon shapes an attack is drawn with, built from the colours of the caster's eyes
    /// instead of a particle effect: the fan of light of a visor or a pair of glasses, the tear
    /// that is lobbed, the puddle of tears it leaves, the eyes themselves for a stare. A few
    /// triangles each, one unlit material. A released shape shrinks away and removes itself.
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

        private float _shrinkStarted = -1f;
        private float _popStarted = -1f;
        private float _releaseAt = -1f;
        private Vector3 _fullScale;
        private Transform _camera;
        private bool _facesCamera;

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

            var shape = Create("GenesisFan", material, vertices, colors, triangles);
            shape.transform.position = apex + Vector3.up * GroundHeight;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                shape.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            shape.AddOutline(material, look.Outline, vertices, 0.1f, true);
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

            var shape = Create("GenesisGroundDisc", material, vertices, colors, triangles);
            shape.transform.position = center + Vector3.up * GroundHeight;
            shape.AddOutline(material, look.Outline, vertices, 0.14f, false);
            return shape;
        }

        /// <summary>A round drop that always faces the camera: outline, colour and a lighter heart.</summary>
        public static GenesisShape Drop(Material material, GenesisEyeLook look, Vector3 position, float radius)
        {
            var host = new GameObject("GenesisDrop");
            var shape = host.AddComponent<GenesisShape>();
            shape._facesCamera = true;
            shape.AddFacingDisc(material, look.Outline, radius, 0);
            shape.AddFacingDisc(material, look.Outer, radius * 0.8f, 1);
            shape.AddFacingDisc(material, look.Core, radius * 0.4f, 2);
            host.transform.position = position;
            shape._fullScale = host.transform.localScale;
            return shape;
        }

        /// <summary>The hero's own pair of eyes on a strip of its skin, always facing the camera.</summary>
        public static GenesisShape EyePair(Material material, GenesisEyeLook look, Color skin, Vector3 position, float halfWidth)
        {
            return Facing("GenesisEyePair", material, GenesisEyes.Pair(look, skin, halfWidth), position);
        }

        /// <summary>One eye of the hero on a disc of its skin, always facing the camera.</summary>
        public static GenesisShape EyeBadge(Material material, GenesisEyeLook look, Color skin, Vector3 position, float radius,
                                            bool second)
        {
            return Facing("GenesisEyeBadge", material, GenesisEyes.Single(look, skin, radius, second), position);
        }

        /// <summary>
        /// The sign of an Aura effect, always facing the camera. It pops in; with "seconds" above
        /// zero it leaves by itself after that long.
        /// </summary>
        public static GenesisShape Icon(Material material, GenesisEffectIcon icon, Color color, Vector3 position, float size,
                                        float seconds)
        {
            var shape = Facing("GenesisIcon", material, GenesisIcons.Mesh(icon, color, size), position);
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

        private static GenesisShape Facing(string name, Material material, Mesh mesh, Vector3 position)
        {
            var host = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            host.GetComponent<MeshFilter>().sharedMesh = mesh;
            Configure(host.GetComponent<MeshRenderer>(), material, 2);
            host.transform.position = position;

            var shape = host.AddComponent<GenesisShape>();
            shape._facesCamera = true;
            shape._fullScale = host.transform.localScale;
            return shape;
        }

        /// <summary>The attack is over: the shape shrinks away, then removes itself.</summary>
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
                Destroy(gameObject);
                return;
            }

            transform.localScale = _fullScale * left;
        }

        private static GenesisShape Create(string name, Material material, Vector3[] vertices, Color[] colors, int[] triangles)
        {
            var host = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            host.GetComponent<MeshFilter>().sharedMesh = mesh;
            Configure(host.GetComponent<MeshRenderer>(), material, 0);

            var shape = host.AddComponent<GenesisShape>();
            shape._fullScale = host.transform.localScale;
            return shape;
        }

        private void OnDestroy()
        {
            // The meshes are built for this one shape.
            foreach (var filter in GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh != null)
                    Destroy(filter.sharedMesh);
        }

        private void AddFacingDisc(Material material, Color color, float radius, int order)
        {
            var vertices = Circle(radius, false, out var triangles);
            var colors = new Color[vertices.Length];
            for (var i = 0; i < colors.Length; i++)
                colors[i] = WithAlpha(color, 1f);

            var child = new GameObject("Disc", typeof(MeshFilter), typeof(MeshRenderer));
            child.transform.SetParent(transform, false);
            var mesh = new Mesh { name = "GenesisDropDisc" };
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            child.GetComponent<MeshFilter>().sharedMesh = mesh;
            Configure(child.GetComponent<MeshRenderer>(), material, order);
        }

        // The outline follows the rim of the shape; a fan closes back on its apex.
        private void AddOutline(Material material, Color color, Vector3[] vertices, float width, bool fromApex)
        {
            var child = new GameObject("Outline");
            child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.alignment = LineAlignment.TransformZ;
            child.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.startColor = color;
            line.endColor = color;
            line.widthMultiplier = width;
            line.sortingOrder = 1;

            var first = fromApex ? 0 : 1;
            line.positionCount = vertices.Length - first;
            for (var i = first; i < vertices.Length; i++)
            {
                // The line object is turned to lie flat: the shape's z is its y.
                var point = vertices[i];
                line.SetPosition(i - first, new Vector3(point.x, point.z, 0f));
            }
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
    }
}
