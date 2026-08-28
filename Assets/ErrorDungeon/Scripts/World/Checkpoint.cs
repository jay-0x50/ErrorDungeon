using UnityEngine;

namespace ErrorDungeon
{
    public sealed class Checkpoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private Vector3 respawnOffset = new Vector3(0f, 1.1f, 2f);
        public string Prompt => "[E] 불씨에서 휴식";
        public Vector3 RespawnPosition => transform.position + respawnOffset;

        public void Interact(PlayerInteractor player) => GameSession.Instance?.ActivateCheckpoint(this);
    }
}

