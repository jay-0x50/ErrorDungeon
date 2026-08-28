using System;
using UnityEngine;

namespace ErrorDungeon
{
    [RequireComponent(typeof(ActorStateMachine))]
    public sealed class CombatVitals : MonoBehaviour
    {
        [SerializeField] private Faction faction;
        [SerializeField] private int vitality;
        [SerializeField] private int endurance;
        [SerializeField] private float baseHealth = 400f;
        [SerializeField] private float baseStamina = 100f;
        [SerializeField] private float defense = 12f;
        [SerializeField] private float maxPoise = 35f;
        [SerializeField] private int soulsReward = 50;

        private ActorStateMachine state;
        private float currentPoise;
        private float lastStaminaUse;
        private float lastPoiseDamage;

        public Faction Faction => faction;
        public float Health { get; private set; }
        public float Stamina { get; private set; }
        public float MaxHealth => baseHealth + vitality * 25f;
        public float MaxStamina => baseStamina + endurance * 2f;
        public float Health01 => MaxHealth <= 0f ? 0f : Health / MaxHealth;
        public float Stamina01 => MaxStamina <= 0f ? 0f : Stamina / MaxStamina;
        public bool IsDead => state != null && state.IsDead;
        public int SoulsReward => soulsReward;
        public event Action<CombatVitals, DamageInfo, float> Damaged;
        public event Action<CombatVitals, GameObject> Died;

        private void Awake()
        {
            state = GetComponent<ActorStateMachine>();
            Health = MaxHealth;
            Stamina = MaxStamina;
            currentPoise = maxPoise;
        }

        private void Update()
        {
            if (IsDead) return;
            if (Time.time - lastStaminaUse > 0.65f && state.Current != ActorState.Guard)
            {
                var load = GetComponent<PlayerProgression>()?.LoadClass ?? EquipmentLoad.Medium;
                var multiplier = load == EquipmentLoad.Light ? 1.1f : load == EquipmentLoad.Heavy ? 0.85f : 1f;
                Stamina = Mathf.MoveTowards(Stamina, MaxStamina, 24f * multiplier * Time.deltaTime);
            }
            if (Time.time - lastPoiseDamage > 4f) currentPoise = Mathf.MoveTowards(currentPoise, maxPoise, 30f * Time.deltaTime);
        }

        public void Configure(Faction newFaction, float hp, float stamina, float armor, float poise, int reward)
        {
            faction = newFaction;
            baseHealth = hp;
            baseStamina = stamina;
            defense = armor;
            maxPoise = poise;
            soulsReward = reward;
            Health = MaxHealth;
            Stamina = MaxStamina;
            currentPoise = maxPoise;
        }

        public bool TrySpendStamina(float amount)
        {
            if (amount <= 0f) return true;
            if (Stamina + 0.001f < amount) return false;
            Stamina = Mathf.Max(0f, Stamina - amount);
            lastStaminaUse = Time.time;
            return true;
        }

        public HitOutcome ReceiveHit(DamageInfo hit)
        {
            if (IsDead || hit.Attacker == gameObject) return HitOutcome.Ignored;
            var attackerVitals = hit.Attacker != null ? hit.Attacker.GetComponentInParent<CombatVitals>() : null;
            if (attackerVitals != null && attackerVitals.Faction == faction) return HitOutcome.Ignored;

            var resolver = GetComponent<IDefenseResolver>();
            if (resolver != null)
            {
                var resolved = resolver.ResolveIncomingHit(ref hit);
                if (resolved != HitOutcome.Damaged) return resolved;
            }

            var damage = Mathf.Max(1f, hit.RawDamage * (hit.Critical ? 2.2f : 1f) - defense);
            Health = Mathf.Max(0f, Health - damage);
            currentPoise -= Mathf.Max(0f, hit.PoiseDamage);
            lastPoiseDamage = Time.time;
            Damaged?.Invoke(this, hit, damage);

            if (Health <= 0f)
            {
                state.Force(ActorState.Dead);
                Died?.Invoke(this, hit.Attacker);
                return HitOutcome.Damaged;
            }

            if (currentPoise <= 0f)
            {
                currentPoise = maxPoise;
                state.Force(ActorState.HitReact, 0.55f);
            }
            return HitOutcome.Damaged;
        }

        public void Heal(float amount) => Health = Mathf.Min(MaxHealth, Health + Mathf.Max(0f, amount));

        public void RestoreAll()
        {
            Health = MaxHealth;
            Stamina = MaxStamina;
            currentPoise = maxPoise;
        }

        public void Revive()
        {
            state.Force(ActorState.Idle);
            RestoreAll();
        }

        public void SetAttributes(int newVitality, int newEndurance)
        {
            var healthRatio = Health01;
            var staminaRatio = Stamina01;
            vitality = Mathf.Max(0, newVitality);
            endurance = Mathf.Max(0, newEndurance);
            Health = Mathf.Max(1f, MaxHealth * healthRatio);
            Stamina = MaxStamina * staminaRatio;
        }
    }
}
