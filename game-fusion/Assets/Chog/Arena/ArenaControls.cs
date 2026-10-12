using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Chog.Arena
{
    public sealed class ArenaControls : MonoBehaviour
    {
        public Vector2 TouchMove;
        public Vector2 TouchAim;
        public bool TouchAiming { get; private set; }
        public bool TouchSpecialHeld { get; private set; }
        public bool PrimaryHeld { get; private set; }
        public bool SpecialHeld { get; private set; }
        public Vector3 AimOffset { get; private set; } = Vector3.right * ArcherRules.SpecialRange;
        private uint _primaryRelease, _specialRelease;
        private bool _wasPrimaryHeld, _wasSpecialHeld;
        private float _suppressReleaseUntil;
        private bool _touchReleasePending;
        private InputAction _move;

        private void OnEnable()
        {
            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick");
            _move.Enable();
        }

        private void Update()
        {
            if (Time.unscaledTime < _suppressReleaseUntil) return;
            bool mobileTouch = Touchscreen.current != null &&
                (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld);
            bool primary = !mobileTouch && Mouse.current != null && Mouse.current.leftButton.isPressed;
            bool special = !mobileTouch && Mouse.current != null && Mouse.current.rightButton.isPressed;
            if (Gamepad.current != null)
            {
                primary |= Gamepad.current.rightTrigger.isPressed;
                special |= Gamepad.current.leftTrigger.isPressed;
            }
            if (!_touchReleasePending && ((_wasPrimaryHeld && !primary) || (!mobileTouch && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                || (Gamepad.current != null && Gamepad.current.rightTrigger.wasReleasedThisFrame))) _primaryRelease++;
            if ((_wasSpecialHeld && !special) || (!mobileTouch && Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame)
                || (Gamepad.current != null && Gamepad.current.leftTrigger.wasReleasedThisFrame)) _specialRelease++;
            _wasPrimaryHeld = primary;
            _wasSpecialHeld = special;
            PrimaryHeld = primary;
            SpecialHeld = special || TouchSpecialHeld;
        }

        public void SetTouchAim(Vector2 value)
        {
            TouchAim = value;
            TouchAiming = value.sqrMagnitude > .04f;
            if (TouchAiming) AimOffset = new Vector3(value.x, 0, value.y) * ArcherRules.SpecialRange;
        }

        public void QueuePrimaryRelease() { _primaryRelease++; _touchReleasePending = true; }
        public void QueueSpecialRelease() { _specialRelease++; }
        public void SetTouchSpecial(bool held) { TouchSpecialHeld = held; SpecialHeld = held; }

        public ArenaInput Read(NetworkRunner runner)
        {
            bool touch = Touchscreen.current != null &&
                (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld);
            Vector2 move = touch ? TouchMove : _move.ReadValue<Vector2>();
            if (!touch && !_touchReleasePending && Mouse.current != null && Camera.main != null && runner.TryGetPlayerObject(runner.LocalPlayer, out var hero))
            {
                var plane = new Plane(Vector3.up, hero.transform.position);
                var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (plane.Raycast(ray, out float distance))
                {
                    var offset = ray.GetPoint(distance) - hero.transform.position;
                    if (offset.sqrMagnitude > .04f) AimOffset = Vector3.ClampMagnitude(offset, ArcherRules.SpecialRange);
                }
            }
            if (Gamepad.current != null)
            {
                var aim = Gamepad.current.rightStick.ReadValue();
                if (aim.sqrMagnitude > .01f) AimOffset = new Vector3(aim.x, 0, aim.y) * ArcherRules.SpecialRange;
            }
            _touchReleasePending = false;
            return new ArenaInput { Move = move, Aim = AimOffset, PrimaryRelease = _primaryRelease,
                SpecialRelease = _specialRelease, Valid = true };
        }

        public void BeginSession()
        {
            _primaryRelease = _specialRelease = 0;
            _suppressReleaseUntil = Time.unscaledTime + .2f;
            _touchReleasePending = false;
            ResetTouch();
        }

        private void OnDisable() { _move?.Dispose(); ResetTouch(); }
        private void OnApplicationFocus(bool focused) { if (!focused) ResetTouch(); }
        private void OnApplicationPause(bool paused) { if (paused) ResetTouch(); }
        public void ResetTouch() { TouchMove = TouchAim = Vector2.zero; TouchAiming = TouchSpecialHeld = false;
            PrimaryHeld = SpecialHeld = _wasPrimaryHeld = _wasSpecialHeld = false; }
    }
}
