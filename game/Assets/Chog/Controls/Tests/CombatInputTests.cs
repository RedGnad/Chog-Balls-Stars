using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Chog.Controls.Tests
{
    // The official InputTestFixture isolates and restores Unity's real input runtime.
    public sealed class CombatInputTests : InputTestFixture
    {
        private InputActionAsset _actions;
        private CombatInputReader _reader;
        private Gamepad _pad;
        private Joystick _specialStick;
        private Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            _pad = InputSystem.AddDevice<Gamepad>();
            _specialStick = InputSystem.AddDevice<Joystick>();
            _mouse = InputSystem.AddDevice<Mouse>();
            var path = Path.Combine(Application.dataPath, "TwinStickShooter/PlayerControls.inputactions");
            _actions = InputActionAsset.FromJson(File.ReadAllText(path));
            _actions.Enable();
            _reader = new CombatInputReader(_actions);
        }

        public override void TearDown()
        {
            if (_actions != null)
            {
                _actions.Disable();
                Object.DestroyImmediate(_actions);
            }
            base.TearDown();
        }

        [Test]
        public void DesktopMouseKeepsBothActionsAndPointerPosition()
        {
            Set(_mouse.position, new Vector2(200, 100));
            Press(_mouse.leftButton);
            var state = _reader.Read(false);
            Assert.That(state.Fire, Is.True);
            Assert.That(state.AltFire, Is.False);
            Assert.That(state.UseStickAim, Is.False);
            Assert.That(state.PointerPosition, Is.EqualTo(new Vector2(200, 100)));
            Release(_mouse.leftButton);
            Press(_mouse.rightButton);
            state = _reader.Read(false);
            Assert.That(state.Fire, Is.False);
            Assert.That(state.AltFire, Is.True);
        }

        [Test]
        public void TouchModeReadsMovementAndBasicAimSimultaneously()
        {
            Set(_pad.leftStick, Vector2.up);
            Set(_pad.rightStick, Vector2.right);
            Press(_pad.rightTrigger);
            var state = _reader.Read(true);
            Assert.That(state.Move, Is.EqualTo(Vector2.up));
            Assert.That(state.StickAim, Is.EqualTo(Vector2.right));
            Assert.That(state.Fire, Is.True);
            Assert.That(state.UseStickAim, Is.True);
        }

        [Test]
        public void ReleasingBasicPreservesAimForTheReleaseCast()
        {
            Set(_pad.rightStick, Vector2.up);
            Press(_pad.rightTrigger);
            _reader.Read(true);
            Set(_pad.rightStick, Vector2.zero);
            Release(_pad.rightTrigger);
            var released = _reader.Read(true);
            Assert.That(released.Fire, Is.False);
            Assert.That(released.StickAim, Is.EqualTo(Vector2.up));
        }

        [Test]
        public void SpecialReadsItsSeparateJoystick()
        {
            Set(_pad.rightStick, Vector2.right);
            Set(_specialStick.stick, Vector2.left);
            Press(_pad.leftTrigger);
            var state = _reader.Read(true);
            Assert.That(state.Fire, Is.False);
            Assert.That(state.AltFire, Is.True);
            Assert.That(state.StickAim, Is.EqualTo(Vector2.left));
        }

        [Test]
        public void SpecialReleaseKeepsItsAimEvenWhenBasicIsStillHeld()
        {
            Set(_pad.rightStick, Vector2.right);
            Set(_specialStick.stick, Vector2.left);
            Press(_pad.rightTrigger);
            Press(_pad.leftTrigger);
            _reader.Read(true);
            Set(_specialStick.stick, Vector2.zero);
            Release(_pad.leftTrigger);
            var state = _reader.Read(true);
            Assert.That(state.Fire, Is.True);
            Assert.That(state.AltFire, Is.False);
            Assert.That(state.StickAim, Is.EqualTo(Vector2.left));
        }

        [Test]
        public void TouchModeIgnoresMouseClicksEmulatedByTheBrowser()
        {
            Press(_mouse.leftButton);
            Press(_mouse.rightButton);
            var state = _reader.Read(true);
            Assert.That(state.Fire, Is.False);
            Assert.That(state.AltFire, Is.False);
        }

        [Test]
        public void ResetDropsCachedAimAfterControlsAreCancelled()
        {
            Set(_pad.rightStick, Vector2.up);
            Press(_pad.rightTrigger);
            _reader.Read(true);
            InputSystem.ResetDevice(_pad);
            InputSystem.ResetDevice(_specialStick);
            _reader.Reset();
            var state = _reader.Read(true);
            Assert.That(state.Fire, Is.False);
            Assert.That(state.StickAim, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void AnotherFingerCannotStealOrReleaseACapturedControl()
        {
            var pointer = new PointerCapture();
            Assert.That(pointer.TryCapture(11), Is.True);
            Assert.That(pointer.TryCapture(12), Is.False);
            Assert.That(pointer.Release(12), Is.False);
            Assert.That(pointer.Owns(11), Is.True);
            Assert.That(pointer.Release(11), Is.True);
            Assert.That(pointer.TryCapture(12), Is.True);
        }

        [Test]
        public void CancellationAllowsANewFingerWithoutAnOldRelease()
        {
            var pointer = new PointerCapture();
            pointer.TryCapture(11);
            pointer.Reset();
            Assert.That(pointer.TryCapture(12), Is.True);
            Assert.That(pointer.Release(11), Is.False);
            Assert.That(pointer.Owns(12), Is.True);
        }
    }
}
