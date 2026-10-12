using System;
using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Chog.Arena
{
    public sealed class ArenaHud : MonoBehaviour
    {
        private ArenaSession _session;
        private Text _status;
        private Text _resources;
        private InputField _room;
        private GameObject _menu, _pads;
        private bool _connecting;

        private void Start()
        {
            if (Application.isBatchMode) return;
            _session = GetComponent<ArenaSession>();
            var canvas = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform);
            var statusPanel = Rect("Status panel", canvas.transform, new Vector2(10, -10), new Vector2(1050, 112));
            statusPanel.AddComponent<Image>().color = new Color(.035f, .07f, .105f, .9f);
            _status = Label(canvas.transform, "CHOG ARENA — Archer", new Vector2(20, -20), new Vector2(950, 60));
            _resources = Label(canvas.transform, "", new Vector2(20, -65), new Vector2(950, 50));
            _menu = Rect("Menu", canvas.transform, new Vector2(20, -90), new Vector2(520, 280));
            _room = Rect("Room", _menu.transform, Vector2.zero, new Vector2(350, 50)).AddComponent<InputField>();
            _room.gameObject.AddComponent<Image>().color = new Color(.1f, .15f, .2f, .95f);
            _room.textComponent = Label(_room.transform, "chog-dev", new Vector2(8, -8), new Vector2(330, 40));
            _room.text = "chog-dev";
            Button(_menu.transform, "Rejoindre la partie", -65, () => Connect(GameMode.Client));
            Button(_menu.transform, "Entraînement hors ligne", -125, () => Connect(GameMode.Single));
#if UNITY_EDITOR
            Button(_menu.transform, "Démarrer serveur (développement)", -185, () => Connect(GameMode.Server));
#endif
            var hintPanel = Rect("Help panel", canvas.transform, new Vector2(10, -635), new Vector2(1240, 72));
            hintPanel.AddComponent<Image>().color = new Color(.035f, .07f, .105f, .82f);
            Label(canvas.transform, "WASD : bouger · viser puis relâcher clic gauche : volée · clic droit : zone\nMobile : joystick gauche · viser puis relâcher à droite · bouton ZONE",
                new Vector2(20, -644), new Vector2(1200, 65));
            _pads = new GameObject("Touch controls", typeof(RectTransform));
            _pads.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)_pads.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            Stick(_pads.transform, false);
            Stick(_pads.transform, true);
            var special = new GameObject("Special", typeof(RectTransform), typeof(Image), typeof(ArenaTouchAbility));
            special.transform.SetParent(_pads.transform, false);
            var specialRect = (RectTransform)special.transform;
            specialRect.anchorMin = specialRect.anchorMax = new Vector2(1, 0);
            specialRect.anchoredPosition = new Vector2(-300, 260);
            specialRect.sizeDelta = new Vector2(105, 105);
            special.GetComponent<Image>().color = new Color(.6f, .2f, .55f, .85f);
            special.GetComponent<ArenaTouchAbility>().Controls = GetComponent<ArenaControls>();
            Label(special.transform, "ZONE", new Vector2(17, -33), new Vector2(90, 45));
        }

        private async void Connect(GameMode mode)
        {
            if (_connecting || _session.Runner != null) return;
            _connecting = true;
            try { await _session.StartSession(mode, _room.text.Trim()); }
            catch (Exception e) { Debug.LogException(e); }
            finally { _connecting = false; }
        }

        private void Update()
        {
            if (_status == null) return;
            var runner = _session.Runner;
            bool running = runner != null && runner.IsRunning;
            _menu.SetActive(!running);
            _pads.SetActive(running && runner.GameMode != GameMode.Server && Touchscreen.current != null &&
                (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld));
            _status.text = "CHOG ARENA · " + _session.Status;
            _resources.text = "";
            if (running && runner.TryGetPlayerObject(runner.LocalPlayer, out var player))
            {
                var hero = player.GetComponent<ArenaHero>();
                _status.text += " · PV " + hero.Health + " · volées " + hero.PrimaryCasts +
                    " · zones " + hero.SpecialCasts;
                _resources.text = "ÉNERGIE " + hero.Energy.ToString("0.0") + "/" + ArcherRules.MaxEnergy.ToString("0") +
                    "     ZONE " + hero.Special.ToString("0.0") + "/" + ArcherRules.MaxSpecial.ToString("0") +
                    "     IMPACTS " + hero.ReceivedHits;
            }
        }

        private void Stick(Transform parent, bool aim)
        {
            var pad = new GameObject(aim ? "Aim" : "Move", typeof(RectTransform), typeof(Image), typeof(ArenaTouchStick));
            pad.transform.SetParent(parent, false);
            var rect = (RectTransform)pad.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(aim ? 1 : 0, 0);
            rect.anchoredPosition = new Vector2(aim ? -120 : 120, 140); rect.sizeDelta = new Vector2(170, 170);
            pad.GetComponent<Image>().color = new Color(.1f, .15f, .2f, .6f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(pad.transform, false);
            ((RectTransform)handle.transform).sizeDelta = new Vector2(50, 50);
            handle.GetComponent<Image>().color = new Color(.4f, .8f, 1, .8f);
            handle.GetComponent<Image>().raycastTarget = false;
            var stick = pad.GetComponent<ArenaTouchStick>();
            stick.Controls = GetComponent<ArenaControls>(); stick.IsAim = aim; stick.Handle = (RectTransform)handle.transform;
        }

        private static GameObject Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return obj;
        }

        private static Text Label(Transform parent, string text, Vector2 position, Vector2 size)
        {
            var label = Rect("Label", parent, position, size).AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 22;
            label.text = text; label.color = Color.white; label.raycastTarget = false;
            return label;
        }

        private static void Button(Transform parent, string text, float y, UnityEngine.Events.UnityAction action)
        {
            var button = Rect(text, parent, new Vector2(0, y), new Vector2(500, 50)).AddComponent<Button>();
            button.gameObject.AddComponent<Image>().color = new Color(.1f, .4f, .6f, .95f);
            Label(button.transform, text, new Vector2(8, -8), new Vector2(480, 40));
            button.onClick.AddListener(action);
        }
    }
}
