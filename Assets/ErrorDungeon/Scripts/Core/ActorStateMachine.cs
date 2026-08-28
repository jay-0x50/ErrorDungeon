using System;
using UnityEngine;

namespace ErrorDungeon
{
    public sealed class ActorStateMachine : MonoBehaviour
    {
        public ActorState Current { get; private set; } = ActorState.Idle;
        public float LockedUntil { get; private set; }
        public event Action<ActorState, ActorState> Changed;

        public bool IsLocked => Time.time < LockedUntil;
        public bool IsDead => Current == ActorState.Dead;
        public bool CanMove => !IsDead && Current != ActorState.Attack && Current != ActorState.Dodge &&
                               Current != ActorState.Parry && Current != ActorState.UseItem &&
                               Current != ActorState.HitReact && Current != ActorState.Knockdown &&
                               Current != ActorState.Cutscene;

        public bool TryEnter(ActorState next, float lockSeconds = 0f, bool force = false)
        {
            if (!force && !CanTransition(next)) return false;
            SetState(next, lockSeconds);
            return true;
        }

        public void Force(ActorState next, float lockSeconds = 0f) => SetState(next, lockSeconds);

        public void ReleaseToLocomotion()
        {
            if (IsDead || Current == ActorState.Cutscene) return;
            SetState(ActorState.Idle, 0f);
        }

        private void Update()
        {
            if (IsLocked) return;
            if (Current == ActorState.HitReact || Current == ActorState.Knockdown ||
                Current == ActorState.Parry || Current == ActorState.UseItem)
                ReleaseToLocomotion();
        }

        private bool CanTransition(ActorState next)
        {
            if (Current == ActorState.Dead) return false;
            if (next == ActorState.Dead || next == ActorState.Cutscene) return true;
            if (Current == ActorState.Cutscene) return false;
            if (Current == ActorState.HitReact || Current == ActorState.Knockdown) return !IsLocked;
            if (IsLocked) return next == ActorState.Dodge && Current == ActorState.Attack;
            return true;
        }

        private void SetState(ActorState next, float lockSeconds)
        {
            var previous = Current;
            Current = next;
            LockedUntil = Mathf.Max(LockedUntil, Time.time + Mathf.Max(0f, lockSeconds));
            if (next == ActorState.Idle || next == ActorState.Locomotion) LockedUntil = Time.time;
            Changed?.Invoke(previous, next);
        }
    }
}
