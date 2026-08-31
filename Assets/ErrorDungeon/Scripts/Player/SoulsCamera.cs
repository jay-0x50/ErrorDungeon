using System.Collections;
using UnityEngine;

namespace ErrorDungeon
{
    public readonly struct CinematicShot
    {
        public readonly Vector3 Position;
        public readonly Vector3 LookAt;
        public readonly float Duration;
        public readonly float FieldOfView;

        public CinematicShot(Vector3 position, Vector3 lookAt, float duration, float fieldOfView = 52f)
        {
            Position = position;
            LookAt = lookAt;
            Duration = Mathf.Max(0.05f, duration);
            FieldOfView = Mathf.Clamp(fieldOfView, 28f, 75f);
        }
    }

    public sealed class SoulsCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.55f, 0f);
        [SerializeField] private float distance = 5.5f;
        [SerializeField] private float sensitivity = 2.1f;
        [SerializeField] private float smooth = 15f;
        [SerializeField] private LayerMask collisionMask = ~0;
        private float yaw;
        private float pitch = 16f;
        private LockOnSystem lockOn;
        private Camera controlledCamera;
        private Coroutine cinematicRoutine;

        public bool IsCinematic { get; private set; }

        public void SetTarget(Transform value)
        {
            target = value;
            lockOn = value != null ? value.GetComponent<LockOnSystem>() : null;
            yaw = value != null ? value.eulerAngles.y : 0f;
        }

        private void Start()
        {
            if (Application.isBatchMode) return;
            controlledCamera = GetComponent<Camera>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void PlayCinematic(params CinematicShot[] shots)
        {
            if (shots == null || shots.Length == 0 || Application.isBatchMode) return;
            if (cinematicRoutine != null) StopCoroutine(cinematicRoutine);
            cinematicRoutine = StartCoroutine(CinematicRoutine(shots));
        }

        private void LateUpdate()
        {
            if (Application.isBatchMode || IsCinematic || target == null) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                var unlock = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = unlock ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = unlock;
            }

            if (lockOn != null && lockOn.Target != null)
            {
                var look = lockOn.Target.position + Vector3.up - (target.position + pivotOffset);
                var angles = Quaternion.LookRotation(look).eulerAngles;
                yaw = Mathf.LerpAngle(yaw, angles.y, Time.deltaTime * 5f);
                pitch = Mathf.LerpAngle(pitch, NormalizeAngle(angles.x), Time.deltaTime * 5f);
            }
            else if (Cursor.lockState == CursorLockMode.Locked)
            {
                yaw += Input.GetAxis("Mouse X") * sensitivity;
                pitch -= Input.GetAxis("Mouse Y") * sensitivity;
                pitch = Mathf.Clamp(pitch, -20f, 65f);
            }

            var pivot = target.position + pivotOffset;
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var desired = pivot - rotation * Vector3.forward * distance;
            if (Physics.Linecast(pivot, desired, out var hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                desired = hit.point + hit.normal * 0.2f;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smooth * Time.deltaTime));
            transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        }

        private IEnumerator CinematicRoutine(CinematicShot[] shots)
        {
            IsCinematic = true;
            if (controlledCamera == null) controlledCamera = GetComponent<Camera>();
            var gameplayFov = controlledCamera != null ? controlledCamera.fieldOfView : 60f;

            foreach (var shot in shots)
            {
                var startPosition = transform.position;
                var startRotation = transform.rotation;
                var startFov = controlledCamera != null ? controlledCamera.fieldOfView : gameplayFov;
                var targetDirection = shot.LookAt - shot.Position;
                if (targetDirection.sqrMagnitude < 0.001f) targetDirection = transform.forward;
                var targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
                var elapsed = 0f;
                while (elapsed < shot.Duration)
                {
                    elapsed += Time.deltaTime;
                    var normalized = Mathf.Clamp01(elapsed / shot.Duration);
                    var eased = normalized * normalized * (3f - 2f * normalized);
                    transform.position = Vector3.Lerp(startPosition, shot.Position, eased);
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, eased);
                    if (controlledCamera != null)
                        controlledCamera.fieldOfView = Mathf.Lerp(startFov, shot.FieldOfView, eased);
                    yield return null;
                }
            }

            if (controlledCamera != null) controlledCamera.fieldOfView = gameplayFov;
            IsCinematic = false;
            cinematicRoutine = null;
        }

        private void OnDisable()
        {
            IsCinematic = false;
            cinematicRoutine = null;
        }

        private static float NormalizeAngle(float value) => value > 180f ? value - 360f : value;
    }
}
