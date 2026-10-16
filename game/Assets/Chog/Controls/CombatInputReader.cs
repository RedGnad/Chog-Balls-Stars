using UnityEngine;
using UnityEngine.InputSystem;

namespace Chog.Controls
{
    public struct CombatInputState
    {
        public Vector2 Move;
        public Vector2 StickAim;
        public Vector2 PointerPosition;
        public bool Fire;
        public bool AltFire;
        // Held through the mouse (or its keys) alone, not a stick or a pad: these aim at the pointer
        // when they are pressed, so the game can attack on the press.
        public bool PointerFire;
        public bool PointerSpecial;
        public bool UseStickAim;
    }

    // Presentation-side input only: Quantum remains responsible for combat rules.
    public sealed class CombatInputReader
    {
        private readonly InputAction _move, _aimBasic, _aimSpecial;
        private readonly InputAction _fire, _special, _mouseFire, _mouseSpecial, _point;
        private Vector2 _lastStickAim;
        private Vector2 _basicAim, _specialAim;
        private bool _fireWasHeld, _specialWasHeld;

        public CombatInputReader(InputActionAsset actions)
        {
            var map = actions.FindActionMap("GameplayActions", true);
            _move = map.FindAction("Move", true);
            _aimBasic = map.FindAction("AimBasic", true);
            _aimSpecial = map.FindAction("AimSpecial", true);
            _fire = map.FindAction("Fire", true);
            _special = map.FindAction("Special", true);
            _mouseFire = map.FindAction("MouseFire", true);
            _mouseSpecial = map.FindAction("MouseSpecial", true);
            _point = map.FindAction("Point", true);
        }

        public CombatInputState Read(bool touchControls)
        {
            bool fire = _fire.IsPressed();
            bool special = _special.IsPressed();
            if (fire) _basicAim = _aimBasic.ReadValue<Vector2>();
            if (special) _specialAim = _aimSpecial.ReadValue<Vector2>();
            if (_specialWasHeld && !special) _lastStickAim = _specialAim;
            else if (_fireWasHeld && !fire) _lastStickAim = _basicAim;
            else if (special) _lastStickAim = _specialAim;
            else if (fire) _lastStickAim = _basicAim;

            bool mouseFire = !touchControls && _mouseFire.IsPressed();
            bool mouseSpecial = !touchControls && _mouseSpecial.IsPressed();
            var state = new CombatInputState
            {
                Move = _move.ReadValue<Vector2>(),
                StickAim = _lastStickAim,
                PointerPosition = _point.ReadValue<Vector2>(),
                Fire = fire || mouseFire,
                AltFire = special || mouseSpecial,
                PointerFire = mouseFire && !fire,
                PointerSpecial = mouseSpecial && !special,
                // The sample casts on release. Keep the stick's aim for that input tick.
                UseStickAim = touchControls || fire || special || _fireWasHeld || _specialWasHeld
            };
            _fireWasHeld = fire;
            _specialWasHeld = special;
            return state;
        }

        public void Reset()
        {
            _lastStickAim = Vector2.zero;
            _basicAim = _specialAim = Vector2.zero;
            _fireWasHeld = _specialWasHeld = false;
        }
    }
}
