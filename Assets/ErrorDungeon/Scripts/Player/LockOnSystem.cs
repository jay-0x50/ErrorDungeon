using UnityEngine;

namespace ErrorDungeon
{
    public sealed class LockOnSystem : MonoBehaviour
    {
        [SerializeField] private float maxDistance = 28f;
        public Transform Target { get; private set; }

        private void Update()
        {
            if (Application.isBatchMode) return;
            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (Target != null) Target = null;
                else Target = FindBestTarget();
            }
            if (Target == null) return;
            var vitals = Target.GetComponent<CombatVitals>();
            if (vitals == null || vitals.IsDead || Vector3.Distance(transform.position, Target.position) > maxDistance * 1.25f)
                Target = null;
        }

        private Transform FindBestTarget()
        {
            var forward = Camera.main != null ? Camera.main.transform.forward : transform.forward;
            Transform best = null;
            var bestScore = float.MaxValue;
            foreach (var vitals in FindObjectsOfType<CombatVitals>())
            {
                if (vitals.Faction != Faction.Enemy || vitals.IsDead) continue;
                var delta = vitals.transform.position - transform.position;
                var distance = delta.magnitude;
                if (distance > maxDistance) continue;
                var angle = Vector3.Angle(forward, delta);
                var score = distance + angle * 0.15f;
                if (score >= bestScore) continue;
                bestScore = score;
                best = vitals.transform;
            }
            return best;
        }
    }
}

