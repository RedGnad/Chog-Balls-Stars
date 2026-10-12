using Fusion;
using UnityEngine;

namespace Chog.Arena
{
    [RequireComponent(typeof(Camera))]
    public sealed class ArenaCameraFollow : MonoBehaviour
    {
        private ArenaSession _session;

        private void Awake()
        {
            var camera = GetComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 35;
            transform.rotation = Quaternion.Euler(75, 0, 0);
        }

        private void LateUpdate()
        {
            if (Application.isBatchMode) return;
            if (_session == null) _session = FindFirstObjectByType<ArenaSession>();
            var runner = _session != null ? _session.Runner : null;
            if (runner == null || !runner.IsRunning || runner.GameMode == GameMode.Server ||
                !runner.TryGetPlayerObject(runner.LocalPlayer, out var player) || player == null) return;
            var hero = player.GetComponent<ArenaHero>();
            if (hero == null || hero.Health <= 0) return;
            Vector3 target = hero.transform.position + new Vector3(0, 25, -7.5f);
            target.x = 0;
            target.z = Mathf.Clamp(target.z, -18, 3);
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 5);
        }
    }
}
