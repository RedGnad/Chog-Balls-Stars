using Fusion;
using UnityEngine;

namespace Chog.Arena
{
    public struct ArenaInput : INetworkInput
    {
        public Vector2 Move;
        // Planar aim offset retains distance for the Archer's ballistic special.
        public Vector3 Aim;
        // Monotonic release counters preserve taps that begin and end between network polls.
        public uint PrimaryRelease;
        public uint SpecialRelease;
        public NetworkBool Valid;

        public ArenaInput Sanitized()
        {
            var value = this;
            value.Move = Finite(Move.x) && Finite(Move.y) ? Vector2.ClampMagnitude(Move, 1) : Vector2.zero;
            value.Aim = Finite(Aim.x) && Finite(Aim.z)
                ? Vector3.ClampMagnitude(new Vector3(Aim.x, 0, Aim.z), ArcherRules.SpecialRange) : Vector3.forward;
            return value;
        }

        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
    }
}
