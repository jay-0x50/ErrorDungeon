using UnityEngine;

namespace ErrorDungeon
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float radius = 2.6f;
        public IInteractable Current { get; private set; }
        public string CurrentPrompt => Current?.Prompt;
        public PlayerProgression Progression { get; private set; }

        private void Awake() => Progression = GetComponent<PlayerProgression>();

        private void Update()
        {
            if (Application.isBatchMode) return;
            Current = FindClosest();
            if (Current != null && Input.GetKeyDown(KeyCode.E)) Current.Interact(this);
            if (Input.GetKeyDown(KeyCode.L)) Progression.TryLevelUp();
            if (Input.GetKeyDown(KeyCode.U)) Progression.TryUpgradeWeapon();
        }

        private IInteractable FindClosest()
        {
            IInteractable best = null;
            var bestDistance = float.MaxValue;
            foreach (var hit in Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Collide))
            {
                var interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable == null) continue;
                var component = interactable as Component;
                if (component == null) continue;
                var distance = (component.transform.position - transform.position).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = interactable;
            }
            return best;
        }
    }
}
