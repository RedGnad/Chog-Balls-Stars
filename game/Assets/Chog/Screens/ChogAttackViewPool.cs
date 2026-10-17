using System.Collections.Generic;
using Chog.Match;
using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chog.Screens
{
    /// <summary>
    /// Keeps the view of an attack for the next attack of the same kind, instead of making one and destroying
    /// it for every attack (docs/PERF-AUDIT.md, D1; Photon: "For best performance QuantumEntityViews should be
    /// pooled"). An attack's view is a prefab with the sample's particle systems and models inside, made and
    /// destroyed several times a second in a fight. Only attacks are kept (a view with AttackView or
    /// GenesisAttackView); a hero, a coin or a structure is made and destroyed as before. The kept views wait
    /// under this object, in the match's scene, and go away with it. Their components start again on each
    /// activation (AttackView, ChogAttackLook, GenesisAttackView, BallisticAttackView). A private test page
    /// turns the pool off with "viewpool=0".
    /// </summary>
    public sealed class ChogAttackViewPool : MonoBehaviour, IQuantumEntityViewPool
    {
        private const string ObjectName = "Chog Attack View Pool";
        private const string QueryParameter = "viewpool";

        private readonly Dictionary<GameObject, Stack<GameObject>> _free = new Dictionary<GameObject, Stack<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> _borrowed = new Dictionary<GameObject, GameObject>();
        private readonly Dictionary<GameObject, bool> _kept = new Dictionary<GameObject, bool>();
        private readonly List<(GameObject Instance, float At)> _later = new List<(GameObject, float)>();
        private int _made;
        private int _reused;

        public int PooledCount => _made;
        public int BorrowedCount => _borrowed.Count;

        /// <summary>What the pool did, for a test page's console (logged at the end of a match scene).</summary>
        public static string Status { get; private set; } = "view pool off";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Watch()
        {
            var url = Application.absoluteURL ?? "";
            var off = LocalGenesis.TestEntriesAllowed(url, Application.isEditor) && MatchModeChoice.QueryValue(url, QueryParameter) == "0";
            if (off)
                return;
            SceneManager.sceneLoaded += (scene, mode) => Attach(scene);
        }

        // Called once the scene's objects woke up, before any view is made: the view updater found no pool
        // of its own (QuantumEntityViewUpdater.Awake) and takes this one.
        private static void Attach(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var updater = root.GetComponentInChildren<QuantumEntityViewUpdater>(true);
                if (updater == null || updater.Pool != null)
                    continue;
                var host = new GameObject(ObjectName);
                SceneManager.MoveGameObjectToScene(host, scene);
                updater.Pool = host.AddComponent<ChogAttackViewPool>();
                Status = "view pool on";
                return;
            }
        }

        public T Create<T>(T prefab, bool activate = true, bool createIfEmpty = true) where T : Component
            => Create(prefab, null, activate, createIfEmpty);

        public GameObject Create(GameObject prefab, bool activate = true, bool createIfEmpty = true)
            => Create(prefab, null, activate, createIfEmpty);

        public T Create<T>(T prefab, Transform parent, bool activate = true, bool createIfEmpty = true) where T : Component
        {
            var made = Create(prefab.gameObject, parent, activate, createIfEmpty);
            return made != null ? made.GetComponent<T>() : null;
        }

        public GameObject Create(GameObject prefab, Transform parent, bool activate = true, bool createIfEmpty = true)
        {
            if (!Kept(prefab))
            {
                // Not an attack: made as the view updater makes it without a pool.
                var made = parent != null ? Instantiate(prefab, parent) : Instantiate(prefab);
                if (!activate)
                    made.SetActive(false);
                return made;
            }

            if (!_free.TryGetValue(prefab, out var free))
                _free[prefab] = free = new Stack<GameObject>();
            GameObject instance = null;
            while (free.Count > 0 && instance == null)
                instance = free.Pop();
            if (instance == null)
            {
                if (!createIfEmpty)
                    return null;
                instance = Instantiate(prefab, transform);
                _made++;
            }
            else
            {
                _reused++;
            }

            _borrowed[instance] = prefab;
            var placed = instance.transform;
            if (parent != null)
                placed.SetParent(parent, false);
            placed.localPosition = Vector3.zero;
            placed.localRotation = Quaternion.identity;
            if (activate)
                instance.SetActive(true);
            Status = "view pool made " + _made + "  reused " + _reused + "  out " + _borrowed.Count;
            return instance;
        }

        public void Destroy(Component component, bool deactivate = true) => Destroy(component.gameObject, deactivate);

        public void Destroy(GameObject instance, bool deactivate = true)
        {
            if (instance == null)
                return;
            if (!_borrowed.TryGetValue(instance, out var prefab))
            {
                Object.Destroy(instance);
                return;
            }
            _borrowed.Remove(instance);
            if (deactivate)
                instance.SetActive(false);
            instance.transform.SetParent(transform, false);
            _free[prefab].Push(instance);
        }

        public void Destroy(GameObject instance, float delay)
        {
            if (instance != null && _borrowed.ContainsKey(instance))
                _later.Add((instance, Time.time + Mathf.Max(0f, delay)));
            else if (instance != null)
                Object.Destroy(instance, delay);
        }

        public void Prepare(GameObject prefab, int desiredCount)
        {
            if (prefab == null || !Kept(prefab))
                return;
            if (!_free.TryGetValue(prefab, out var free))
                _free[prefab] = free = new Stack<GameObject>();
            while (free.Count < desiredCount)
            {
                var instance = Instantiate(prefab, transform);
                instance.SetActive(false);
                free.Push(instance);
                _made++;
            }
        }

        private void OnDestroy() => Debug.Log("CHOG_VIEWPOOL " + Status);

        private void Update()
        {
            for (var i = _later.Count - 1; i >= 0; i--)
            {
                if (Time.time < _later[i].At)
                    continue;
                var instance = _later[i].Instance;
                _later.RemoveAt(i);
                Destroy(instance, true);
            }
        }

        // Whether views of this prefab are kept: the views of attacks.
        private bool Kept(GameObject prefab)
        {
            if (prefab == null)
                return false;
            if (!_kept.TryGetValue(prefab, out var kept))
            {
                kept = prefab.GetComponentInChildren<TwinStickShooter.AttackView>(true) != null
                       || prefab.GetComponentInChildren<GenesisAttackView>(true) != null;
                _kept[prefab] = kept;
            }
            return kept;
        }
    }
}
