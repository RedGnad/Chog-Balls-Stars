using Quantum;
using UnityEngine;
using UnityEngine.Scripting;

namespace Chog.Diagnostics
{
    /// <summary>
    /// Writes one console line per skill use and per damage event, so a test run can show which
    /// attacks fired and what they did. It lives next to the performance overlay and is only
    /// active with it ("?perf=1" or the Editor menu).
    /// Line format: "CHOG_COMBAT skill {asset name} tick {simulation tick}",
    /// "CHOG_COMBAT damage {amount} tick {simulation tick}" and, for each attack a character
    /// starts, basic or special, "CHOG_COMBAT cast by {local|other} {token id, 0 for none} tick
    /// {simulation tick}". These events are raised as soon as a frame is predicted: when a
    /// prediction is corrected, the same use can be written again with another tick, so count
    /// distinct ticks, not lines.
    /// </summary>
    [Preserve]
    public sealed class CombatLog : MonoBehaviour
    {
        // Attack starts of the local player's character that wait to be confirmed or undone, by
        // tick, and how many ended each way since the page opened. An undone start is one the
        // game drew from a prediction that the verified match then corrected.
        private static readonly System.Collections.Generic.Dictionary<int, int> LocalStarts =
            new System.Collections.Generic.Dictionary<int, int>();
        private static int s_confirmed;
        private static int s_undone;
        private static bool s_reported;

        private void Start()
        {
            // One line in the overlay, which the minute report of a phone carries as well.
            if (!s_reported)
            {
                s_reported = true;
                PerformanceHud.StatusLines.Add(() => "own attacks  confirmed " + s_confirmed + "  undone " + s_undone);
            }

            QuantumEvent.Subscribe<EventSkillAction>(this, OnSkillAction);
            QuantumEvent.Subscribe<EventCharacterDamaged>(this, OnCharacterDamaged);
            QuantumEvent.Subscribe<EventCharacterSkill>(this, OnCharacterSkill);
            QuantumCallback.Subscribe(this, (CallbackEventConfirmed callback) => LogOutcome(callback.EventKey, "confirmed"));
            QuantumCallback.Subscribe(this, (CallbackEventCanceled callback) => LogOutcome(callback.EventKey, "canceled"));
        }

        // Whether an attack start that was written when its frame was predicted stood once the
        // frame was verified, or was undone: "CHOG_COMBAT cast confirmed|canceled tick {tick}".
        private static void LogOutcome(EventKey key, string outcome)
        {
            if (key.Id != EventCharacterSkill.ID)
                return;

            Debug.Log("CHOG_COMBAT cast " + outcome + " tick " + key.Tick);

            // Counted for the local player by tick: another character starting an attack at the
            // very same tick can be counted in its place, which hardly moves the totals.
            if (!LocalStarts.TryGetValue(key.Tick, out var waiting) || waiting <= 0)
                return;

            if (waiting == 1)
                LocalStarts.Remove(key.Tick);
            else
                LocalStarts[key.Tick] = waiting - 1;

            if (outcome == "confirmed")
                s_confirmed++;
            else
                s_undone++;
        }

        // Who starts an attack: the Genesis it is, and whether the local player plays it.
        private static void OnCharacterSkill(EventCharacterSkill e)
        {
            var frame = e.Game.Frames.Predicted;
            if (frame == null || !frame.Exists(e.character))
                return;

            var token = frame.TryGet<GenesisHero>(e.character, out var hero) ? hero.TokenId : 0;
            var local = frame.TryGet<PlayerLink>(e.character, out var link) && e.Game.PlayerIsLocal(link.PlayerRef);
            if (local)
            {
                LocalStarts.TryGetValue(e.Tick, out var waiting);
                LocalStarts[e.Tick] = waiting + 1;
            }

            Debug.Log("CHOG_COMBAT cast by " + (local ? "local " : "other ") + token + " tick " + e.Tick);
        }

        private static void OnSkillAction(EventSkillAction e)
        {
            var skill = QuantumUnityDB.GetGlobalAsset<SkillData>(e.skillData);
            Debug.Log("CHOG_COMBAT skill " + (skill == null ? e.skillData.ToString() : skill.name) + " tick " + e.Tick);
        }

        private static void OnCharacterDamaged(EventCharacterDamaged e)
        {
            Debug.Log("CHOG_COMBAT damage " + e.damage.AsFloat.ToString("0", System.Globalization.CultureInfo.InvariantCulture) +
                      " tick " + e.Tick);
        }
    }
}
