using Quantum;
using UnityEngine;
using UnityEngine.Scripting;

namespace Chog.Diagnostics
{
    /// <summary>
    /// While an attack button of the local hero is down in the simulation, or was a tick ago, writes
    /// what the simulation sees of that hero once per predicted tick: the two attack buttons, whether
    /// they are aimed with a pointer, the attack lock, the energy against the cost of the basic
    /// attack, whether a bot drives the hero, and the flags of its last verified input. It answers
    /// "why did this press start no attack". Only with the performance overlay, at most 80 lines.
    /// Line format: "CHOG_PROBE tick {n} fire {state} alt {state} pointer {0|1} lock {seconds}
    /// energy {current}/{cost} bot {0|1} verified {flags}", a state being "D"
    /// (down) or "-", then "p" when pressed on that tick and "r" when released.
    /// </summary>
    [Preserve]
    public sealed class InputProbe : MonoBehaviour
    {
        private const int MostLines = 80;

        private int _lines;
        private int _lastTick;
        private bool _wasDown;

        private void Update()
        {
            if (_lines >= MostLines)
                return;
            var game = QuantumRunner.Default != null ? QuantumRunner.Default.Game : null;
            var frame = game != null && game.Frames != null ? game.Frames.Predicted : null;
            if (frame == null || frame.Number == _lastTick)
                return;

            var heroes = frame.Filter<PlayerLink, InputContainer>();
            while (heroes.Next(out var hero, out var link, out var container))
            {
                if (!game.PlayerIsLocal(link.PlayerRef))
                    continue;

                var input = container.Input;
                var down = input.Fire.IsDown || input.AltFire.IsDown || input.Fire.WasReleased || input.AltFire.WasReleased;
                if (!down && !_wasDown)
                    return;
                _wasDown = down;
                _lastTick = frame.Number;
                _lines++;

                var cost = 0f;
                if (frame.TryGet<CharacterAttacks>(hero, out var attacks))
                {
                    var basic = frame.FindAsset<SkillData>(attacks.BasicSkillData.Id);
                    cost = basic != null ? basic.Cost.AsFloat : 0f;
                }
                var energy = AttributesHelper.Has(frame, hero, EAttributeType.Energy)
                    ? AttributesHelper.GetCurrentValue(frame, hero, EAttributeType.Energy).AsFloat
                    : -1f;
                var lockTime = frame.TryGet<MovementData>(hero, out var movement) ? movement.DirectionTimer.AsFloat : -1f;
                var bot = frame.TryGet<Bot>(hero, out var driver) && driver.IsActive;
                var verified = game.Frames.Verified != null ? game.Frames.Verified.GetPlayerInputFlags(link.PlayerRef).ToString() : "none";

                Debug.Log("CHOG_PROBE tick " + frame.Number + " fire " + State(input.Fire) + " alt " + State(input.AltFire) +
                          " pointer " + (input.AimsWithPointer.IsDown ? 1 : 0) + " lock " + lockTime.ToString("0.00") +
                          " energy " + energy.ToString("0.00") + "/" + cost.ToString("0.00") + " bot " + (bot ? 1 : 0) +
                          " verified " + verified);
                return;
            }
        }

        private static string State(Button button)
        {
            return (button.IsDown ? "D" : "-") + (button.WasPressed ? "p" : "") + (button.WasReleased ? "r" : "");
        }
    }
}
