using UnityEngine;

namespace ErrorDungeon
{
    public sealed class DroppedSouls : MonoBehaviour, IInteractable
    {
        public int Amount { get; private set; }
        public string Prompt => $"[E] 잃어버린 혼 {Amount:N0} 회수";
        public void Initialize(int amount) => Amount = Mathf.Max(0, amount);
        public void Interact(PlayerInteractor player) => GameSession.Instance?.RecoverDroppedSouls(this);

        private void Update()
        {
            transform.Rotate(Vector3.up, 55f * Time.deltaTime, Space.World);
            transform.position += Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.002f);
        }
    }
}

