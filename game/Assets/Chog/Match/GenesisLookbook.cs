using System.Collections.Generic;
using Chog.Diagnostics;
using Quantum;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Match
{
    /// <summary>
    /// One screen with every drawing the attacks are made of, to review them against the art:
    /// the sign of each Aura effect in its colour, then the pair of eyes authored for each Eyes
    /// value and for each 1:1 Genesis. Opened with the page option "?lookbook=1"; it covers the
    /// menu and is not part of a match.
    /// </summary>
    public sealed class GenesisLookbook : MonoBehaviour
    {
        public const string QueryParameter = "lookbook";

        private const string RosterResource = "GenesisRoster";
        private const int Columns = 9;
        private const float CellWidth = 2.4f;
        private const float CellHeight = 1.75f;
        private const float EyesHalfWidth = 0.95f;
        private const float IconSize = 0.55f;

        // Far from any scene, so the camera of the lookbook sees nothing else.
        private static readonly Vector3 Origin = new Vector3(0f, 5000f, 0f);
        private static readonly Color Background = new Color(0.16f, 0.14f, 0.2f);

        private RenderTexture _picture;
        private readonly List<Mesh> _meshes = new List<Mesh>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Open()
        {
            if (!PerformanceSampling.HasQueryFlag(Application.absoluteURL, QueryParameter))
                return;

            var host = new GameObject("Chog Lookbook");
            DontDestroyOnLoad(host);
            host.AddComponent<GenesisLookbook>();
        }

        private void Start()
        {
            var roster = Resources.Load<GenesisRoster>(RosterResource);
            if (roster == null || roster.ShapeMaterial == null)
            {
                Debug.LogWarning("CHOG_LOOKBOOK no roster or no shape material");
                return;
            }

            // What is shown, in reading order: a label and a mesh.
            var entries = new List<(string Label, Mesh Mesh)>();
            for (var i = 0; roster.EffectIcons != null && i < roster.EffectIcons.Length; i++)
            {
                var color = roster.EffectColors != null && i < roster.EffectColors.Length ? roster.EffectColors[i] : Color.clear;
                entries.Add((roster.EffectIds[i], GenesisIcons.Mesh(roster.EffectIcons[i], color, IconSize)));
            }

            for (var i = 0; roster.EyeLooks != null && i < roster.EyeLooks.Length; i++)
            {
                if (roster.EyeLooks[i].Glyph != GenesisEyeGlyph.None)
                    entries.Add((roster.EyeValues[i], GenesisEyes.Pair(roster.EyeLooks[i], Color.clear, EyesHalfWidth)));
            }

            for (var i = 0; roster.UniqueLooks != null && i < roster.UniqueLooks.Length; i++)
            {
                var unique = roster.UniqueLooks[i];
                var index = roster.IndexOf(unique.TokenId);
                if (unique.Look.Glyph != GenesisEyeGlyph.None && index >= 0)
                    entries.Add((roster.Entries[index].Name, GenesisEyes.Pair(unique.Look, unique.Skin, EyesHalfWidth)));
            }

            var rows = (entries.Count + Columns - 1) / Columns;
            var width = Columns * CellWidth;
            var height = Mathf.Max(rows * CellHeight, width * 9f / 16f);

            var root = new GameObject("Lookbook Content").transform;
            root.SetParent(transform, false);
            root.position = Origin;

            _picture = new RenderTexture(1920, 1080, 16);
            var view = new GameObject("Lookbook Camera").AddComponent<Camera>();
            view.transform.SetParent(root, false);
            view.transform.localPosition = new Vector3(0f, 0f, -10f);
            view.orthographic = true;
            view.orthographicSize = height * 0.5f;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Background;
            view.nearClipPlane = 1f;
            view.farClipPlane = 20f;
            view.targetTexture = _picture;

            var canvasHost = new GameObject("Lookbook Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasHost.transform.SetParent(transform, false);
            var canvas = canvasHost.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var shown = new GameObject("Picture", typeof(RectTransform)).AddComponent<RawImage>();
            Stretch(shown.rectTransform, canvasHost.transform, Vector2.zero, Vector2.one);
            shown.texture = _picture;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (var i = 0; i < entries.Count; i++)
            {
                int column = i % Columns, row = i / Columns;
                var x = -width * 0.5f + (column + 0.5f) * CellWidth;
                var y = height * 0.5f - (row + 0.5f) * CellHeight;

                var drawn = new GameObject(entries[i].Label, typeof(MeshFilter), typeof(MeshRenderer));
                drawn.transform.SetParent(root, false);
                drawn.transform.localPosition = new Vector3(x, y + 0.15f, 0f);
                drawn.GetComponent<MeshFilter>().sharedMesh = entries[i].Mesh;
                drawn.GetComponent<MeshRenderer>().sharedMaterial = roster.ShapeMaterial;
                _meshes.Add(entries[i].Mesh);

                // The label sits under its drawing, at the same place on the screen.
                var label = new GameObject("Label", typeof(RectTransform)).AddComponent<Text>();
                var left = (x - CellWidth * 0.5f + width * 0.5f) / width;
                var bottom = (y - CellHeight * 0.5f + height * 0.5f) / height;
                Stretch(label.rectTransform, canvasHost.transform, new Vector2(left, bottom),
                    new Vector2(left + CellWidth / width, bottom + 0.22f * CellHeight / height));
                label.font = font;
                label.text = entries[i].Label;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 8;
                label.resizeTextMaxSize = 22;
                label.raycastTarget = false;
            }

            Debug.Log("CHOG_LOOKBOOK " + entries.Count + " drawings");
        }

        private void OnDestroy()
        {
            foreach (var mesh in _meshes)
                Destroy(mesh);
            if (_picture != null)
                _picture.Release();
        }

        private static void Stretch(RectTransform area, Transform parent, Vector2 min, Vector2 max)
        {
            area.SetParent(parent, false);
            area.anchorMin = min;
            area.anchorMax = max;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
        }
    }
}
