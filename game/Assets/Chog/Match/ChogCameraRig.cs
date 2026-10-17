using System;
using System.Globalization;
using Chog.Diagnostics;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The angle of the match camera, in one place, read by the camera's follow script
    /// (TwinStickShooter's LocalPlayerCameraFollow). The sample looks down at 75 degrees with a
    /// vertical field of view of 35, 25 units over the player and 7.5 behind: from there the drawn
    /// eyes and mouths of the heroes are hidden under the fringe of their crown. A lower pitch shows
    /// faces; the offset is then worked out so that the ground seen around the player stays the
    /// sample's (same width at the player, same ground behind it), see CameraAngles.
    /// A page tries another angle without a build: "?camera=62" (pitch, the sample's field of view),
    /// "?camera=62,27.5" (pitch and vertical field of view), "?camera=62,27.5,33" (and the distance
    /// to the point looked at). Until the owner picks one, a page without the option keeps the sample's.
    /// </summary>
    public static class ChogCameraRig
    {
        public const string QueryParameter = "camera";

        private static CameraAngles.Rig? s_rig;

        private static CameraAngles.Rig Rig
        {
            get
            {
                if (s_rig == null)
                {
                    PerformanceSampling.TryGetQueryValue(Application.absoluteURL, QueryParameter, out var value);
                    s_rig = CameraAngles.Parse(value);
                }
                return s_rig.Value;
            }
        }

        /// <summary>Whether the page asked for another angle than the sample's.</summary>
        public static bool Overridden => !Rig.Equals(CameraAngles.Sample);

        /// <summary>Degrees the camera looks down (90 is straight down).</summary>
        public static float Pitch => Rig.Pitch;

        /// <summary>Vertical field of view, in degrees.</summary>
        public static float FieldOfView => Rig.FieldOfView;

        /// <summary>Where the camera stands from the player of team 0 (team 1 turns it: z changes sign).</summary>
        public static Vector3 FollowOffset
        {
            get
            {
                var (height, back) = CameraAngles.Offset(Rig);
                return new Vector3(0f, height, -back);
            }
        }

        /// <summary>
        /// A bound of the camera's z (the sample's MinZ, MaxZ, InvertedMinZ, InvertedMaxZ) moved by as
        /// much as the camera moved back, so that it still follows the player to both ends of the arena.
        /// </summary>
        public static float ShiftBound(float sampleBound, bool inverted)
        {
            var shift = FollowOffset.z - CameraAngles.SampleOffsetZ;
            return sampleBound + (inverted ? -shift : shift);
        }

        /// <summary>
        /// The view of the arena shown before play (the sample's InitialPosition, 0, 30, -6.75), moved
        /// back and up with the camera.
        /// </summary>
        public static Vector3 PresentationPosition(Vector3 samplePosition)
        {
            var offset = FollowOffset;
            return samplePosition + new Vector3(0f, offset.y - CameraAngles.SampleOffsetY, offset.z - CameraAngles.SampleOffsetZ);
        }

        /// <summary>
        /// How far shadows must reach to cover the ground the camera sees (Unity's shadow distance);
        /// the sample's angle needs less than the 40 of the quality level "Good".
        /// </summary>
        public static float ShadowReach => CameraAngles.FarthestGround(Rig) + 4f;
    }

    /// <summary>The camera's geometry, without the engine (tested outside Unity).</summary>
    public static class CameraAngles
    {
        public readonly struct Rig : IEquatable<Rig>
        {
            public readonly float Pitch;
            public readonly float FieldOfView;
            /// <summary>From the camera to the point of the floor it looks at, in world units.</summary>
            public readonly float Distance;

            public Rig(float pitch, float fieldOfView, float distance)
            {
                Pitch = pitch;
                FieldOfView = fieldOfView;
                Distance = distance;
            }

            public bool Equals(Rig other) => Math.Abs(Pitch - other.Pitch) < 1e-3f
                && Math.Abs(FieldOfView - other.FieldOfView) < 1e-3f && Math.Abs(Distance - other.Distance) < 1e-3f;

            public override bool Equals(object obj) => obj is Rig other && Equals(other);
            public override int GetHashCode() => Pitch.GetHashCode() ^ FieldOfView.GetHashCode() ^ Distance.GetHashCode();
        }

        // The sample (GameEssentials.prefab): camera pitched 75 degrees, field of view 35, at (0, 25, -7.5)
        // from the player; its axis meets the floor 0.80 behind the player, 25.88 away.
        public const float SamplePitch = 75f;
        public const float SampleFieldOfView = 35f;
        public const float SampleOffsetY = 25f;
        public const float SampleOffsetZ = -7.5f;

        /// <summary>How far behind the player the sample's camera looks: kept at every angle.</summary>
        public static readonly float Lead = SampleOffsetY / (float)Math.Tan(SamplePitch * Math.PI / 180.0) + SampleOffsetZ;

        public static readonly float SampleDistance = (float)Math.Sqrt(SampleOffsetY * SampleOffsetY
            + (SampleOffsetZ - Lead) * (SampleOffsetZ - Lead));

        public static readonly Rig Sample = new Rig(SamplePitch, SampleFieldOfView, SampleDistance);

        /// <summary>
        /// The distance that keeps the sample's width of floor at the point looked at, for a field of view.
        /// </summary>
        public static float DistanceFor(float fieldOfView)
        {
            return SampleDistance * (float)(Math.Tan(SampleFieldOfView * Math.PI / 360.0) / Math.Tan(fieldOfView * Math.PI / 360.0));
        }

        /// <summary>Height over the player and distance behind it of the camera.</summary>
        public static (float Height, float Back) Offset(Rig rig)
        {
            var pitch = rig.Pitch * Math.PI / 180.0;
            var height = rig.Distance * Math.Sin(pitch);
            var back = rig.Distance * Math.Cos(pitch) - Lead;
            return ((float)height, (float)back);
        }

        /// <summary>The distance from the camera to the far corners of the floor it sees (16:9).</summary>
        public static float FarthestGround(Rig rig)
        {
            var (height, _) = Offset(rig);
            var top = (rig.Pitch - rig.FieldOfView / 2f) * Math.PI / 180.0;
            if (top <= 0.05)
                return 200f;
            var along = height / Math.Sin(top);
            var side = Math.Tan(rig.FieldOfView * Math.PI / 360.0) * 16.0 / 9.0;
            return (float)(along * Math.Sqrt(1.0 + side * side));
        }

        /// <summary>
        /// The angle a page's "camera=" option asks for: "pitch", "pitch,fov" or "pitch,fov,distance".
        /// Without a distance, the one that keeps the sample's width of floor; anything out of range
        /// (pitch 30 to 90, field of view 15 to 70, distance 8 to 80) or not a number gives the sample's.
        /// </summary>
        public static Rig Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Sample;
            var parts = Uri.UnescapeDataString(value).Split(',');
            if (parts.Length > 3)
                return Sample;
            var numbers = new float[parts.Length];
            for (var i = 0; i < parts.Length; i++)
                if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[i]))
                    return Sample;

            var pitch = numbers[0];
            var fieldOfView = parts.Length > 1 ? numbers[1] : SampleFieldOfView;
            var distance = parts.Length > 2 ? numbers[2] : DistanceFor(fieldOfView);
            if (pitch < 30f || pitch > 90f || fieldOfView < 15f || fieldOfView > 70f || distance < 8f || distance > 80f)
                return Sample;
            return new Rig(pitch, fieldOfView, distance);
        }
    }
}
