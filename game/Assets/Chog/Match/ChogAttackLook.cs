using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The basic attack of a hero drawn as what the hat of its Genesis gives it
    /// (docs/ATTACKS-FROM-THE-ART.md): the snowball of a Russian Hat, the coin of a Dollar Cap, the cap
    /// itself thrown flat, a leaf of its crown for a bare head. The object comes from the library of
    /// parts (ChogHeroModel.BuildAttack), in the hero's colours, drawn with the one material of the heroes.
    /// It replaces what the sample drew for that attack (its arrow, its slash): the attack itself, where
    /// it goes and what it hits, is the simulation's and does not change. A thrown object turns as its
    /// style says; a strike (the attack of the brute, which does not travel) pops out in front, turns
    /// and fades; where an attack ends, a few small pieces in its colour fly out (ChogImpact).
    /// </summary>
    public sealed class ChogAttackLook : MonoBehaviour
    {
        // The basic attacks of the three classes, by the name of their data in the project.
        private static readonly string[] Basic =
            { "ArcherBasicAttackData", "KnightBasicAttackData", "SpellcasterBasicFirstAttackData" };
        private const string Strike = "KnightBasicAttackData";

        private const float GrowSeconds = 0.08f;
        private const float SpinDegreesPerSecond = 720f;
        private const float TumbleDegreesPerSecond = 540f;
        private const float StrikeSeconds = 0.25f;
        // Above the ground, at the height of the hands of a hero.
        private const float Height = 0.9f;

        private Transform _object;
        private ChogHeroModel.AttackStyle _style;
        private Color _impact;
        private float _age;
        private bool _strike;
        private bool _shown;

        /// <summary>
        /// Dresses the view of an attack whose data is named "dataName", thrown by the hero of
        /// "metadataId" (ChogHeroModel.Recruit for a recruit, below 0 for no hero). Does nothing for an
        /// attack that is not a basic attack, or a hero the library does not know.
        /// </summary>
        public static void Dress(Transform view, string dataName, int metadataId)
        {
            if (view == null || metadataId < 0 || !GenesisEffects.Enabled || System.Array.IndexOf(Basic, dataName) < 0)
                return;
            var look = view.GetComponent<ChogAttackLook>();
            if (look == null)
                look = view.gameObject.AddComponent<ChogAttackLook>();
            look.Show(metadataId, dataName == Strike);
        }

        private void Show(int metadataId, bool strike)
        {
            if (_object != null)
                Destroy(_object.gameObject);
            _object = ChogHeroModel.BuildAttack(metadataId, transform, out _style, out _impact);
            _shown = _object != null;
            if (!_shown)
                return;

            // What the sample drew for this attack steps aside.
            foreach (var drawn in GetComponentsInChildren<Renderer>(true))
                if (!drawn.transform.IsChildOf(_object))
                    drawn.enabled = false;
            foreach (var particles in GetComponentsInChildren<ParticleSystem>(true))
                particles.gameObject.SetActive(false);

            _strike = strike;
            _age = 0f;
            _object.localPosition = new Vector3(0f, Height, 0f);
            _object.localScale = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (!_shown || _object == null)
                return;
            _age += Time.deltaTime;
            var grown = Mathf.Clamp01(_age / GrowSeconds);
            // The library already made every object as large as the others (ChogHeroModel.BuildAttack).
            var size = 1f;
            if (_strike)
            {
                // Out, a little larger than thrown, then gone: the blow lasts as long as the attack.
                var t = Mathf.Clamp01(_age / StrikeSeconds);
                size *= 1.3f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t * 1.1f + 0.05f));
                _object.localRotation = Quaternion.Euler(0f, 360f * t, 0f) * Rest();
            }
            else
            {
                size *= grown;
                _object.localRotation = Turned(_age) * Rest();
            }
            _object.localScale = Vector3.one * Mathf.Max(0f, size);
        }

        // How the object stands before it turns: a thing that points looks where the attack goes.
        private Quaternion Rest()
        {
            return Quaternion.identity;
        }

        private Quaternion Turned(float age)
        {
            switch (_style)
            {
                case ChogHeroModel.AttackStyle.Spin:
                    return Quaternion.Euler(0f, SpinDegreesPerSecond * age, 0f);
                case ChogHeroModel.AttackStyle.Tumble:
                    return Quaternion.Euler(TumbleDegreesPerSecond * age, 0f, 0f);
                case ChogHeroModel.AttackStyle.Float:
                    return Quaternion.Euler(0f, 0f, Mathf.Sin(age * 14f) * 12f);
                default:
                    return Quaternion.Euler(0f, 0f, age * 90f);
            }
        }

        // Where the attack ends (it hit, or it went as far as it goes), its pieces fly out.
        private void OnDisable()
        {
            if (!_shown || _object == null || !gameObject.scene.isLoaded)
                return;
            _shown = false;
            ChogImpact.Burst(_object.position, _impact);
        }
    }
}
