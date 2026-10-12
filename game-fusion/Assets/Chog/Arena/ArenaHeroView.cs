using UnityEngine;

namespace Chog.Arena
{
    // All feedback follows replicated state. The server never needs this component.
    public sealed class ArenaHeroView : MonoBehaviour
    {
        public AudioClip VolleySound;
        public AudioClip SpecialSound;
        public AudioClip ImpactSound;
        private ArenaHero _hero;
        private ArenaControls _controls;
        private Animator _animator;
        private LineRenderer _aimLine, _zoneRing;
        private Transform _healthFill;
        private int _lastPrimary, _lastSpecial, _lastHits;
        private bool _wasAlive;
        private Vector3 _lastPosition;
        private bool _initialized;

        private void Start()
        {
            if (Application.isBatchMode) return;
            _hero = GetComponent<ArenaHero>();
            _animator = GetComponentInChildren<Animator>();
            var session = FindFirstObjectByType<ArenaSession>();
            _controls = session != null ? session.GetComponent<ArenaControls>() : null;
            _lastPrimary = _hero.PrimaryCasts;
            _lastSpecial = _hero.SpecialCasts;
            _lastHits = _hero.ReceivedHits;
            _wasAlive = _hero.Health > 0;
            _lastPosition = transform.position;
            MakeWorldBars();
            if (_hero.HasInputAuthority) MakePreview();
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized || _hero == null || _hero.Object == null) return;
            bool alive = _hero.Health > 0;
            if (_animator != null)
            {
                var displacement = transform.position - _lastPosition;
                displacement.y = 0;
                float speed = alive ? displacement.magnitude / Mathf.Max(Time.deltaTime, .001f) : 0;
                _animator.SetFloat("Speed", speed);
                _animator.SetBool("StopRun", speed < .15f);
                if (_hero.PrimaryCasts != _lastPrimary || _hero.SpecialCasts != _lastSpecial) _animator.SetTrigger("Attack");
                if (!alive && _wasAlive) _animator.SetTrigger("Defeated");
            }
            if (_hero.PrimaryCasts != _lastPrimary) Play(VolleySound, .45f);
            if (_hero.SpecialCasts != _lastSpecial) Play(SpecialSound, .65f);
            if (_hero.ReceivedHits != _lastHits) Play(ImpactSound, .35f);
            _lastPrimary = _hero.PrimaryCasts;
            _lastSpecial = _hero.SpecialCasts;
            _lastHits = _hero.ReceivedHits;
            _wasAlive = alive;
            _lastPosition = transform.position;
            if (_healthFill != null)
            {
                float ratio = Mathf.Clamp01(_hero.Health / (float)ArcherRules.MaxHealth);
                _healthFill.localScale = new Vector3(ratio, 1, 1);
                _healthFill.localPosition = new Vector3(-(1 - ratio) * .45f, 2.35f, 0);
            }
            RenderPreview(alive);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position, volume);
        }

        private static GameObject BarPart(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            Destroy(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().material.color = color;
            return obj;
        }

        private void MakeWorldBars()
        {
            var team = _hero.Team == 0 ? new Color(.22f, .65f, 1) : new Color(1, .35f, .32f);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Team marker";
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = new Vector3(0, .05f, 0);
            marker.transform.localScale = new Vector3(.9f, .012f, .9f);
            Destroy(marker.GetComponent<Collider>());
            marker.GetComponent<Renderer>().material.color = team;
            BarPart("Health background", transform, new Vector3(0, 2.35f, 0), new Vector3(.95f, .12f, .08f), Color.black);
            _healthFill = BarPart("Health", transform, new Vector3(0, 2.35f, -.05f),
                new Vector3(.9f, .09f, .08f), team).transform;
        }

        private void MakePreview()
        {
            _aimLine = MakeLine("Aim preview", 2, .045f);
            _zoneRing = MakeLine("Zone preview", 33, .035f);
            _zoneRing.loop = true;
        }

        private LineRenderer MakeLine(string name, int count, float width)
        {
            var obj = new GameObject(name, typeof(LineRenderer));
            obj.transform.SetParent(transform, false);
            var line = obj.GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = count;
            line.widthMultiplier = width;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.enabled = false;
            return line;
        }

        private void RenderPreview(bool alive)
        {
            if (_aimLine == null || _controls == null) return;
            bool showing = alive && (_controls.PrimaryHeld || _controls.SpecialHeld || _controls.TouchAiming);
            _aimLine.enabled = showing;
            _zoneRing.enabled = showing && _controls.SpecialHeld;
            if (!showing) return;
            Vector3 offset = Vector3.ClampMagnitude(_controls.AimOffset, ArcherRules.SpecialRange);
            Vector3 origin = transform.position + Vector3.up * .12f;
            _aimLine.startColor = _aimLine.endColor = _hero.Energy >= 1 ? Color.yellow : Color.gray;
            _aimLine.SetPosition(0, origin);
            _aimLine.SetPosition(1, origin + offset);
            if (!_zoneRing.enabled) return;
            _zoneRing.startColor = _zoneRing.endColor = _hero.Special >= 1 ? Color.magenta : Color.gray;
            for (int i = 0; i < 33; i++)
            {
                float angle = i * Mathf.PI * 2 / 32;
                _zoneRing.SetPosition(i, origin + offset + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * ArcherRules.SpecialRadius);
            }
        }
    }
}
