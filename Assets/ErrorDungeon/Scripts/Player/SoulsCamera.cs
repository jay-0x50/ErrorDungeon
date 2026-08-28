using UnityEngine;

namespace ErrorDungeon
{
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

        public void SetTarget(Transform value)
        {
            target = value;
            lockOn = value != null ? value.GetComponent<LockOnSystem>() : null;
            yaw = value != null ? value.eulerAngles.y : 0f;
        }

        private void Start()
        {
            if (Application.isBatchMode) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            if (target == null || Application.isBatchMode) return;
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

        private static float NormalizeAngle(float value) => value > 180f ? value - 360f : value;
    }
}
