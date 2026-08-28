using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ErrorDungeon
{
    [RequireComponent(typeof(CombatVitals), typeof(ActorStateMachine), typeof(PlayerMotor))]
    public sealed class PlayerCombat : MonoBehaviour, IDefenseResolver
    {
        private CombatVitals vitals;
        private ActorStateMachine state;
        private PlayerMotor motor;
        private PlayerProgression progression;
        private LockOnSystem lockOn;
        private bool guarding;
        private float invulnerableFrom;
        private float invulnerableUntil;
        private float parryFrom;
        private float parryUntil;
        private int nextAttackId;

        private void Awake()
        {
            vitals = GetComponent<CombatVitals>();
            state = GetComponent<ActorStateMachine>();
            motor = GetComponent<PlayerMotor>();
            progression = GetComponent<PlayerProgression>();
            lockOn = GetComponent<LockOnSystem>();
        }

        private void Update()
        {
            if (Application.isBatchMode || state.IsDead || state.Current == ActorState.Cutscene) return;

            if (Input.GetKeyDown(KeyCode.Space)) TryDodge();
            if (Input.GetKeyDown(KeyCode.F)) TryParry();
            if (Input.GetKeyDown(KeyCode.R)) TryUseFlask();

            var wantsGuard = Input.GetMouseButton(1);
            if (wantsGuard && !guarding) BeginGuard();
            if (!wantsGuard && guarding) EndGuard();
            if (wantsGuard && guarding && !state.IsLocked && state.Current != ActorState.Guard)
                state.Force(ActorState.Guard);
            if (guarding && !vitals.TrySpendStamina(2f * Time.deltaTime)) EndGuard();

            if (Input.GetMouseButtonDown(0))
            {
                if (Input.GetKey(KeyCode.LeftShift)) TryAttack(AttackKind.Heavy);
                else TryAttack(AttackKind.Light);
            }
        }

        public HitOutcome ResolveIncomingHit(ref DamageInfo hit)
        {
            if (Time.time >= invulnerableFrom && Time.time <= invulnerableUntil) return HitOutcome.Evaded;

            if (hit.Parryable && Time.time >= parryFrom && Time.time <= parryUntil)
            {
                var receiver = hit.Attacker != null ? hit.Attacker.GetComponentInParent<IParryReceiver>() : null;
                receiver?.OnParried(gameObject);
                state.Force(ActorState.Idle);
                return HitOutcome.Parried;
            }

            if (!guarding || !IsFacing(hit.Attacker)) return HitOutcome.Damaged;
            var weapon = GameDataRepository.Weapon(progression.WeaponId);
            var staminaCost = Mathf.Max(hit.StaminaDamage, hit.RawDamage * 0.35f) * (weapon?.guard.staminaMul ?? 1f);
            if (!vitals.TrySpendStamina(staminaCost))
            {
                guarding = false;
                hit.RawDamage *= 1.2f;
                state.Force(ActorState.Knockdown, 1.2f);
                return HitOutcome.Damaged;
            }

            hit.RawDamage *= 1f - Mathf.Clamp(weapon?.guard.damageCut ?? 0.55f, 0.55f, 0.85f);
            return HitOutcome.Damaged;
        }

        private void TryAttack(AttackKind kind)
        {
            if (guarding || state.IsLocked || state.Current == ActorState.Dodge || state.Current == ActorState.Parry) return;
            var staminaCost = kind == AttackKind.Light ? 14f : 22f;
            if (!vitals.TrySpendStamina(staminaCost)) return;
            StartCoroutine(AttackRoutine(kind));
        }

        private IEnumerator AttackRoutine(AttackKind kind)
        {
            var heavy = kind != AttackKind.Light;
            var windup = heavy ? 0.48f : 0.24f;
            var recovery = heavy ? 0.72f : 0.42f;
            var motion = heavy ? 1.6f : 1f;
            state.Force(ActorState.Attack, windup + recovery);
            if (lockOn.Target != null) motor.FaceTarget(lockOn.Target.position);
            yield return new WaitForSeconds(windup);

            var attackId = ++nextAttackId;
            var center = transform.position + Vector3.up * 1.05f + transform.forward * (heavy ? 1.35f : 1.05f);
            var radius = heavy ? 1.35f : 1.05f;
            var hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
            var damaged = new HashSet<CombatVitals>();
            foreach (var collider in hits)
            {
                var target = collider.GetComponentInParent<CombatVitals>();
                if (target == null || target.Faction != Faction.Enemy || !damaged.Add(target)) continue;
                var targetState = target.GetComponent<ActorStateMachine>();
                var critical = targetState != null && targetState.Current == ActorState.Knockdown;
                var info = new DamageInfo(gameObject, progression.WeaponAttack * motion, heavy ? 38f : 20f,
                    heavy ? 48f : 24f, false, collider.ClosestPoint(center), transform.forward, attackId, critical);
                target.ReceiveHit(info);
            }
            yield return new WaitForSeconds(recovery);
            if (!state.IsDead && state.Current == ActorState.Attack) state.ReleaseToLocomotion();
        }

        private void TryDodge()
        {
            if (guarding) EndGuard();
            if (state.IsLocked && state.Current != ActorState.Attack) return;
            if (!vitals.TrySpendStamina(18f)) return;
            var load = progression.LoadClass;
            var duration = load == EquipmentLoad.Heavy ? 0.58f : 0.46f;
            var distance = load == EquipmentLoad.Light ? 5.1f : load == EquipmentLoad.Heavy ? 3.4f : 4.3f;
            var iframeCount = load == EquipmentLoad.Light ? 12 : load == EquipmentLoad.Heavy ? 8 : 10;
            invulnerableFrom = Time.time + 4f / 60f;
            invulnerableUntil = invulnerableFrom + iframeCount / 60f;
            state.Force(ActorState.Dodge, duration);
            motor.BeginDodge(motor.MoveInputWorld, duration, distance);
            StartCoroutine(ReleaseStateAfter(ActorState.Dodge, duration));
        }

        private void TryParry()
        {
            if (state.IsLocked || !vitals.TrySpendStamina(12f)) return;
            if (guarding) EndGuard();
            const float duration = 0.56f;
            parryFrom = Time.time + 0.12f;
            parryUntil = parryFrom + 6f / 60f;
            state.Force(ActorState.Parry, duration);
            StartCoroutine(ReleaseStateAfter(ActorState.Parry, duration));
        }

        private void BeginGuard()
        {
            if (state.IsLocked || state.Current == ActorState.Dodge || state.Current == ActorState.Attack) return;
            guarding = true;
            state.Force(ActorState.Guard);
        }

        private void EndGuard()
        {
            guarding = false;
            if (state.Current == ActorState.Guard) state.ReleaseToLocomotion();
        }

        private void TryUseFlask()
        {
            if (state.IsLocked || vitals.Health >= vitals.MaxHealth || !progression.ConsumeFlask()) return;
            var item = GameDataRepository.Item("it_heal_flask");
            var duration = item?.use.castTimeSec ?? 1.1f;
            state.Force(ActorState.UseItem, duration);
            StartCoroutine(FlaskRoutine(duration, item?.use.healHpFlat ?? 220f));
        }

        private IEnumerator FlaskRoutine(float duration, float healing)
        {
            yield return new WaitForSeconds(duration);
            if (!state.IsDead) vitals.Heal(healing);
            if (state.Current == ActorState.UseItem) state.ReleaseToLocomotion();
        }

        private IEnumerator ReleaseStateAfter(ActorState expected, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (state.Current == expected) state.ReleaseToLocomotion();
        }

        private bool IsFacing(GameObject attacker)
        {
            if (attacker == null) return true;
            var direction = Vector3.ProjectOnPlane(attacker.transform.position - transform.position, Vector3.up);
            return direction.sqrMagnitude < 0.001f || Vector3.Angle(transform.forward, direction) <= 100f;
        }
    }
}
