using System;
using UnityEngine;

namespace ErrorDungeon
{
    public enum Faction { Player, Enemy }
    public enum ActorState { Idle, Locomotion, Attack, Dodge, Guard, Parry, UseItem, HitReact, Knockdown, Cutscene, Dead }
    public enum EquipmentLoad { Light, Medium, Heavy }
    public enum AttackKind { Light, Heavy, Charged, Boss }
    public enum HitOutcome { Damaged, Guarded, GuardBroken, Evaded, Parried, Ignored }

    [Serializable]
    public struct DamageInfo
    {
        public GameObject Attacker;
        public float RawDamage;
        public float StaminaDamage;
        public float PoiseDamage;
        public bool Parryable;
        public bool Critical;
        public Vector3 HitPoint;
        public Vector3 HitDirection;
        public int AttackId;

        public DamageInfo(GameObject attacker, float damage, float staminaDamage, float poiseDamage,
            bool parryable, Vector3 point, Vector3 direction, int attackId, bool critical = false)
        {
            Attacker = attacker;
            RawDamage = damage;
            StaminaDamage = staminaDamage;
            PoiseDamage = poiseDamage;
            Parryable = parryable;
            Critical = critical;
            HitPoint = point;
            HitDirection = direction;
            AttackId = attackId;
        }
    }

    public interface IDefenseResolver
    {
        HitOutcome ResolveIncomingHit(ref DamageInfo hit);
    }

    public interface IInteractable
    {
        string Prompt { get; }
        void Interact(PlayerInteractor player);
    }

    public interface IResettableEnemy
    {
        void ResetEnemy();
    }

    public interface IParryReceiver
    {
        void OnParried(GameObject byActor);
    }
}
