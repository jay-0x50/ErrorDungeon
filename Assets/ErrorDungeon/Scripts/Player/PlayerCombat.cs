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
        private ProceduralHumanoidAnimator combatAnimator;
        private bool guarding;
        private float invulnerableFrom;
        private float invulnerableUntil;
        private float parryFrom;
        private float parryUntil;
        private int nextAttackId;
        private int nextComboStep;
        private int activeComboStep = -1;
        private float comboExpiresAt;
        private bool lightAttackBuffered;
        private Coroutine attackRoutine;

        public int ActiveComboStep => activeComboStep;
        public bool ComboWindowOpen => activeComboStep >= 0 || Time.time <= comboExpiresAt;

        private void Awake()
        {
            vitals = GetComponent<CombatVitals>();
            state = GetComponent<ActorStateMachine>();
            motor = GetComponent<PlayerMotor>();
            progression = GetComponent<PlayerProgression>();
            lockOn = GetComponent<LockOnSystem>();
            vitals.Damaged += OnDamaged;
        }

        private void Start() => combatAnimator = GetComponentInChildren<ProceduralHumanoidAnimator>();

        private void OnDestroy()
        {
            if (vitals != null) vitals.Damaged -= OnDamaged;
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
                else QueueLightAttack();
            }

            if (state.Current != ActorState.Attack && Time.time > comboExpiresAt && activeComboStep < 0)
                nextComboStep = 0;
        }

        public HitOutcome ResolveIncomingHit(ref DamageInfo hit)
        {
            if (invulnerableUntil > invulnerableFrom && Time.time >= invulnerableFrom && Time.time <= invulnerableUntil)
                return HitOutcome.Evaded;

            if (hit.Parryable && parryUntil > parryFrom && Time.time >= parryFrom && Time.time <= parryUntil)
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

        private void QueueLightAttack()
        {
            if (state.Current == ActorState.Attack && attackRoutine != null)
            {
                lightAttackBuffered = true;
                return;
            }
            TryAttack(AttackKind.Light);
        }

        private void TryAttack(AttackKind kind)
        {
            if (guarding || state.IsLocked || state.Current == ActorState.Dodge || state.Current == ActorState.Parry)
                return;

            if (kind == AttackKind.Light)
            {
                if (Time.time > comboExpiresAt) nextComboStep = 0;
                if (!vitals.TrySpendStamina(LightStamina(nextComboStep))) return;
                attackRoutine = StartCoroutine(LightComboRoutine(nextComboStep));
                return;
            }

            if (!vitals.TrySpendStamina(22f)) return;
            ResetCombo();
            attackRoutine = StartCoroutine(HeavyAttackRoutine());
        }

        private IEnumerator LightComboRoutine(int startingStep)
        {
            var step = Mathf.Clamp(startingStep, 0, 2);
            while (!vitals.IsDead)
            {
                activeComboStep = step;
                lightAttackBuffered = false;
                var windup = step == 0 ? 0.13f : step == 1 ? 0.12f : 0.18f;
                var recovery = step == 0 ? 0.21f : step == 1 ? 0.2f : 0.32f;
                var motion = step == 0 ? 0.92f : step == 1 ? 1.04f : 1.34f;
                var poiseDamage = step == 2 ? 42f : step == 1 ? 27f : 22f;
                var reach = step == 2 ? 1.32f : step == 1 ? 1.14f : 1.08f;
                var radius = step == 2 ? 1.18f : 1.05f;

                BeginAttackAnimation(windup, recovery, step);
                state.Force(ActorState.Attack, windup + recovery);
                FaceLockedTarget();
                yield return new WaitForSeconds(windup);
                if (vitals.IsDead || state.Current != ActorState.Attack)
                {
                    CancelComboAfterInterrupt();
                    yield break;
                }

                motor.AttackStep(step == 2 ? 0.48f : 0.2f);
                DealAttackDamage(motion, 20f + step * 4f, poiseDamage, reach, radius);
                yield return new WaitForSeconds(recovery);
                if (vitals.IsDead || state.Current != ActorState.Attack)
                {
                    CancelComboAfterInterrupt();
                    yield break;
                }

                var nextStep = (step + 1) % 3;
                nextComboStep = nextStep;
                comboExpiresAt = Time.time + (step == 2 ? 0.36f : 0.58f);
                if (!lightAttackBuffered)
                {
                    activeComboStep = -1;
                    attackRoutine = null;
                    state.ReleaseToLocomotion();
                    yield break;
                }

                if (!vitals.TrySpendStamina(LightStamina(nextStep)))
                {
                    activeComboStep = -1;
                    attackRoutine = null;
                    state.ReleaseToLocomotion();
                    yield break;
                }
                step = nextStep;
            }
            CancelComboAfterInterrupt();
        }

        private IEnumerator HeavyAttackRoutine()
        {
            const float windup = 0.32f;
            const float recovery = 0.46f;
            BeginAttackAnimation(windup, recovery, 5);
            state.Force(ActorState.Attack, windup + recovery);
            FaceLockedTarget();
            yield return new WaitForSeconds(windup);
            if (vitals.IsDead || state.Current != ActorState.Attack)
            {
                attackRoutine = null;
                yield break;
            }

            motor.AttackStep(0.38f);
            DealAttackDamage(1.62f, 38f, 50f, 1.38f, 1.34f);
            yield return new WaitForSeconds(recovery);
            attackRoutine = null;
            if (!state.IsDead && state.Current == ActorState.Attack) state.ReleaseToLocomotion();
        }

        private void BeginAttackAnimation(float windup, float recovery, int variant)
        {
            if (combatAnimator == null) combatAnimator = GetComponentInChildren<ProceduralHumanoidAnimator>();
            combatAnimator?.BeginAttack(windup, recovery, variant);
        }

        private void FaceLockedTarget()
        {
            if (lockOn.Target != null) motor.FaceTarget(lockOn.Target.position);
        }

        private void DealAttackDamage(float motion, float staminaDamage, float poiseDamage, float reach,
            float radius)
        {
            var attackId = ++nextAttackId;
            var center = transform.position + Vector3.up * 1.05f + transform.forward * reach;
            var hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
            var damaged = new HashSet<CombatVitals>();
            foreach (var collider in hits)
            {
                var target = collider.GetComponentInParent<CombatVitals>();
                if (target == null || target.Faction != Faction.Enemy || !damaged.Add(target)) continue;
                var targetState = target.GetComponent<ActorStateMachine>();
                var critical = targetState != null && targetState.Current == ActorState.Knockdown;
                var info = new DamageInfo(gameObject, progression.WeaponAttack * motion, staminaDamage,
                    poiseDamage, false, collider.ClosestPoint(center), transform.forward, attackId, critical);
                target.ReceiveHit(info);
            }
        }

        private static float LightStamina(int step) => step == 2 ? 17f : step == 1 ? 13f : 12f;

        private void ResetCombo()
        {
            lightAttackBuffered = false;
            nextComboStep = 0;
            activeComboStep = -1;
            comboExpiresAt = 0f;
        }

        private void CancelComboAfterInterrupt()
        {
            attackRoutine = null;
            ResetCombo();
        }

        private void OnDamaged(CombatVitals damaged, DamageInfo hit, float damage)
        {
            StopAllCoroutines();
            guarding = false;
            invulnerableFrom = 0f;
            invulnerableUntil = 0f;
            parryFrom = 0f;
            parryUntil = 0f;
            attackRoutine = null;
            ResetCombo();
        }

        private void TryDodge()
        {
            if (guarding) EndGuard();
            if (state.IsLocked && state.Current != ActorState.Attack) return;
            if (!vitals.TrySpendStamina(18f)) return;
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
                ResetCombo();
            }
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
