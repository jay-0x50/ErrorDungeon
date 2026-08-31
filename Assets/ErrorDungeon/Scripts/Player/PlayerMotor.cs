using UnityEngine;

namespace ErrorDungeon
{
    [RequireComponent(typeof(CharacterController), typeof(ActorStateMachine), typeof(CombatVitals))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 4.2f;
        [SerializeField] private float sprintSpeed = 7.2f;
        [SerializeField] private float rotationSpeed = 14f;
        [SerializeField] private float gravity = -28f;

        private CharacterController controller;
        private ActorStateMachine state;
        private CombatVitals vitals;
        private PlayerProgression progression;
        private LockOnSystem lockOn;
        private Vector3 verticalVelocity;
        private Vector3 dodgeVelocity;
        private Vector3 lastSafePosition;
        private bool hasSafePosition;
        private float dodgeEndsAt;
        private const float WorldHalfExtent = 159f;
        private const float GroundClearance = 0.08f;

        public Vector3 MoveInputWorld { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            state = GetComponent<ActorStateMachine>();
            vitals = GetComponent<CombatVitals>();
            progression = GetComponent<PlayerProgression>();
            lockOn = GetComponent<LockOnSystem>();
            controller.minMoveDistance = 0f;
            controller.skinWidth = 0.045f;
            controller.stepOffset = 0.32f;
            controller.slopeLimit = 48f;
        }

        private void Update()
        {
            if (Application.isBatchMode || vitals.IsDead) return;
            var cameraTransform = Camera.main != null ? Camera.main.transform : transform;
            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            var raw = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            raw = Vector2.ClampMagnitude(raw, 1f);
            MoveInputWorld = (forward * raw.y + right * raw.x).normalized;

            if (Time.time < dodgeEndsAt)
            {
                controller.Move(dodgeVelocity * Time.deltaTime);
                ApplyGravity();
                return;
            }

            if (state.CanMove)
            {
                var guarding = state.Current == ActorState.Guard;
                var sprinting = Input.GetKey(KeyCode.LeftShift) && raw.y > 0.1f && !guarding;
                var speed = guarding ? walkSpeed * 0.45f : sprinting ? sprintSpeed : walkSpeed;
                if (sprinting && !vitals.TrySpendStamina(6f * Time.deltaTime)) speed = walkSpeed;
                controller.Move(MoveInputWorld * speed * Time.deltaTime);
                if (MoveInputWorld.sqrMagnitude > 0.01f && !guarding) state.TryEnter(ActorState.Locomotion);
                else if (state.Current == ActorState.Locomotion) state.ReleaseToLocomotion();
                Rotate(MoveInputWorld);
            }
            ApplyGravity();
        }

        public void BeginDodge(Vector3 direction, float duration, float distance)
        {
            if (direction.sqrMagnitude < 0.01f) direction = -transform.forward;
            dodgeVelocity = direction.normalized * (distance / Mathf.Max(0.05f, duration));
            dodgeEndsAt = Time.time + duration;
            Rotate(direction);
        }

        public void FaceTarget(Vector3 target)
        {
            var direction = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
        }

        public void AttackStep(float distance)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (controller == null || !controller.enabled || distance <= 0f) return;
            controller.Move(transform.forward * distance);
        }

        public void Warp(Vector3 position)
        {
            if (TryGetProceduralFloor(position, out var floor)) position.y = Mathf.Max(position.y, floor);
            if (controller == null) controller = GetComponent<CharacterController>();
            if (controller == null)
            {
                transform.position = position;
                return;
            }
            controller.enabled = false;
            transform.position = position;
            Physics.SyncTransforms();
            controller.enabled = true;
            verticalVelocity = Vector3.zero;
            lastSafePosition = position;
            hasSafePosition = true;
        }

        private void Rotate(Vector3 movement)
        {
            Vector3 direction;
            if (lockOn != null && lockOn.Target != null)
                direction = Vector3.ProjectOnPlane(lockOn.Target.position - transform.position, Vector3.up);
            else direction = movement;
            if (direction.sqrMagnitude < 0.01f) return;
            var target = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        private void ApplyGravity()
        {
            var hasProceduralFloor = TryGetProceduralFloor(transform.position, out var proceduralFloor);
            var grounded = controller.isGrounded;
            if (hasProceduralFloor && transform.position.y < proceduralFloor)
            {
                PinToFloor(proceduralFloor);
                grounded = true;
            }

            if (grounded)
            {
                if (verticalVelocity.y < 0f) verticalVelocity.y = -2f;
                lastSafePosition = transform.position;
                hasSafePosition = true;
            }

            var deltaTime = Mathf.Min(Time.deltaTime, 0.05f);
            verticalVelocity.y = Mathf.Max(verticalVelocity.y + gravity * deltaTime, -42f);
            controller.Move(verticalVelocity * deltaTime);

            if (hasProceduralFloor && transform.position.y < proceduralFloor)
            {
                PinToFloor(proceduralFloor);
                return;
            }

            var recoveryHeight = hasProceduralFloor ? proceduralFloor - 2f : -8f;
            if (transform.position.y >= recoveryHeight) return;
            var recovery = GameSession.Instance != null
                ? GameSession.Instance.CheckpointPosition
                : hasSafePosition ? lastSafePosition : new Vector3(0f, 1.1f, -30f);
            Warp(recovery);
            GameSession.Instance?.ShowNarrative("세계의 틈에서 불씨로 되돌아왔다.", 2.2f);
        }

        private void PinToFloor(float floor)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (controller == null) return;
            controller.enabled = false;
            var position = transform.position;
            position.y = floor;
            transform.position = position;
            Physics.SyncTransforms();
            controller.enabled = true;
            verticalVelocity = new Vector3(0f, -2f, 0f);
            lastSafePosition = position;
            hasSafePosition = true;
        }

        private static bool TryGetProceduralFloor(Vector3 position, out float floor)
        {
            floor = 0f;
            if (float.IsNaN(position.x) || float.IsNaN(position.z) || float.IsInfinity(position.x) ||
                float.IsInfinity(position.z) || Mathf.Abs(position.x) > WorldHalfExtent ||
                Mathf.Abs(position.z) > WorldHalfExtent)
                return false;
            floor = ProceduralArt.GroundHeight(position.x, position.z) + GroundClearance;
            return true;
        }
    }

    [DefaultExecutionOrder(900)]
    public sealed class ProceduralGroundClamp : MonoBehaviour
    {
        private const float WorldHalfExtent = 159f;
        private CharacterController controller;
        private float clearance = 0.08f;

        public void Configure(float groundClearance)
        {
            clearance = Mathf.Max(0.02f, groundClearance);
            controller = GetComponent<CharacterController>();
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void LateUpdate()
        {
            RecoverNow();
        }

        public void RecoverNow()
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (controller == null || !controller.enabled || !IsInsideWorld(transform.position)) return;
            if (!TryResolveFloor(transform, controller, clearance, out var floor)) return;
            if (transform.position.y >= floor - 0.025f) return;

            controller.enabled = false;
            var position = transform.position;
            position.y = floor;
            transform.position = position;
            Physics.SyncTransforms();
            controller.enabled = true;
        }

        internal static bool TryResolveFloor(Transform actor, CharacterController characterController,
            float groundClearance, out float floor)
        {
            var position = actor.position;
            floor = ProceduralArt.GroundHeight(position.x, position.z) + groundClearance;
            var castStartY = Mathf.Max(position.y + 2.4f, floor + 3.5f);
            var origin = new Vector3(position.x, castStartY, position.z);
            var radius = Mathf.Max(0.16f, characterController.radius * 0.72f);
            var hits = Physics.SphereCastAll(origin, radius, Vector3.down, 12f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider == characterController ||
                    hit.collider.transform.IsChildOf(actor) || !hit.collider.gameObject.isStatic || hit.normal.y < 0.48f)
                    continue;
                if (hit.point.y > position.y + characterController.stepOffset + 0.55f) continue;
                floor = Mathf.Max(floor, hit.point.y + groundClearance);
            }
            return true;
        }

        private static bool IsInsideWorld(Vector3 position)
        {
            return !float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z) &&
                   !float.IsInfinity(position.x) && !float.IsInfinity(position.y) && !float.IsInfinity(position.z) &&
                   Mathf.Abs(position.x) <= WorldHalfExtent && Mathf.Abs(position.z) <= WorldHalfExtent;
        }
    }
}
