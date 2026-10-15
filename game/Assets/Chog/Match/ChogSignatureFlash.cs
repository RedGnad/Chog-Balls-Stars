using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The moment a hero fires its special: the special comes from its Eyes, so the eyes it wears (its
    /// visor, its headset, its glasses, its laser eyes, or its drawn eyes) pop up large above it in 3D,
    /// turned to the camera, hold for a breath, then rise and shrink away, with a burst of pieces in their
    /// colour. Drawn with the one material of the heroes, from the library of parts (ChogHeroModel.BuildEyes).
    /// </summary>
    public sealed class ChogSignatureFlash : MonoBehaviour
    {
        private const float Size = 1.5f;
        private const float GrowSeconds = 0.14f;
        private const float HoldSeconds = 0.45f;
        private const float GoSeconds = 0.22f;
        // Above the head of a hero (about 2.1 units tall in the arena).
        private const float Height = 2.8f;

        private Transform _eyes;
        private float _age;

        // When each hero last showed its eyes: a special made of several attacks (an echo) shows them once.
        private static readonly System.Collections.Generic.Dictionary<Quantum.EntityRef, float> s_shown =
            new System.Collections.Generic.Dictionary<Quantum.EntityRef, float>();

        /// <summary>Show, unless this hero showed its eyes less than a second ago.</summary>
        public static void ShowOnce(Quantum.EntityRef hero, Vector3 at, int metadataId)
        {
            if (s_shown.TryGetValue(hero, out var last) && Time.time - last < 1f)
                return;
            s_shown[hero] = Time.time;
            Show(at, metadataId);
        }

        /// <summary>Shows the eyes of that Genesis above the place of its hero (on the ground).</summary>
        public static void Show(Vector3 hero, int metadataId)
        {
            if (!GenesisEffects.Enabled)
                return;
            var host = new GameObject("ChogSignatureFlash");
            host.transform.position = hero + Vector3.up * Height;
            var eyes = ChogHeroModel.BuildEyes(metadataId, host.transform, Size, out var colour);
            if (eyes == null)
            {
                Destroy(host);
                return;
            }
            var flash = host.AddComponent<ChogSignatureFlash>();
            flash._eyes = eyes;
            eyes.localScale = Vector3.zero;
            ChogImpact.Burst(host.transform.position, colour);
        }

        private void LateUpdate()
        {
            _age += Time.deltaTime;
            float size;
            if (_age < GrowSeconds)
                size = 1.25f * Mathf.Sin(Mathf.PI * 0.5f * _age / GrowSeconds);
            else if (_age < GrowSeconds + HoldSeconds)
                size = Mathf.Lerp(1.25f, 1f, (_age - GrowSeconds) / 0.12f);
            else if (_age < GrowSeconds + HoldSeconds + GoSeconds)
                size = 1f - (_age - GrowSeconds - HoldSeconds) / GoSeconds;
            else
            {
                Destroy(gameObject);
                return;
            }

            var lift = _age > GrowSeconds + HoldSeconds ? (_age - GrowSeconds - HoldSeconds) * 2.5f : 0f;
            transform.position += Vector3.up * lift * Time.deltaTime;
            _eyes.localScale = Vector3.one * Mathf.Max(0f, size);

            // Its front (+Z, as the hero looks) turned to the camera.
            var camera = Camera.main;
            if (camera != null)
                _eyes.rotation = Quaternion.LookRotation(camera.transform.position - _eyes.position, camera.transform.up);
        }
    }
}
