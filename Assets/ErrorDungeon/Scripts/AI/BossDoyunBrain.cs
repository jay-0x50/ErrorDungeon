using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ErrorDungeon
{
    [RequireComponent(typeof(CharacterController), typeof(CombatVitals), typeof(ActorStateMachine))]
    public sealed class BossDoyunBrain : MonoBehaviour, IParryReceiver, IResettableEnemy
    {
        private CharacterController controller;
        private CombatVitals vitals;
        private ActorStateMachine state;
        private Transform player;
        private Vector3 home;
        private float attackPower;
        private float nextPatternTime;
        private bool activated;
        private bool phaseTwo;
        private bool initialized;
        private Renderer[] renderers;

        public bool IsActivated => activated && !vitals.IsDead;
        public bool IsPhaseTwo => phaseTwo;
        public CombatVitals Vitals => vitals;

        public void PlayIntro(float duration)
        {
            state.Force(ActorState.Cutscene, duration);
            nextPatternTime = Time.time + duration + 0.4f;
            StartCoroutine(ReleaseCutsceneAfter(duration));
        }

        public void Initialize(Vector3 spawn)
        {
            home = spawn;
            controller = GetComponent<CharacterController>();
            vitals = GetComponent<CombatVitals>();
            state = GetComponent<ActorStateMachine>();
            var data = GameDataRepository.Enemy("boss_doyun");
            attackPower = data?.@base.attack ?? 92f;
            vitals.Configure(Faction.Enemy, data?.@base.hp ?? 6200f, data?.@base.stamina ?? 180f,
                data?.@base.defense ?? 30f, data?.@base.poise ?? 220f, 12000);
            vitals.Died += OnDeath;
            renderers = GetComponentsInChildren<Renderer>();
            initialized = true;
        }

        private void Start()
        {
            if (!initialized) Initialize(transform.position);
            renderers = GetComponentsInChildren<Renderer>();
            FindPlayer();
        }

        private void Update()
        {
            if (vitals.IsDead || Application.isBatchMode) return;
            if (player == null) FindPlayer();
            if (player == null) return;
            var distance = Vector3.Distance(transform.position, player.position);
            if (!activated && distance < 22f)
            {
                activated = true;
                GameSession.Instance?.BeginBossFight(this);
            }
            if (!activated || state.IsLocked) return;
            if (!phaseTwo && vitals.Health01 <= 0.5f)
            {
                StartCoroutine(EnterPhaseTwo());
                return;
            }

            if (distance > 4.8f) MoveTowards(player.position, phaseTwo ? 4.1f : 3.2f);
            else if (Time.time >= nextPatternTime) StartCoroutine(ChoosePattern(distance));
        }

        private IEnumerator ChoosePattern(float distance)
        {
            Face(player.position);
            var roll = Random.value;
            if (phaseTwo && distance > 3.2f && roll < 0.34f) yield return LightningCharge();
            else if (phaseTwo && roll < 0.64f) yield return Combo(5, 0.22f, 0.64f, 0.82f);
            else if (roll < 0.45f) yield return Combo(2, 0.32f, 0.48f, 0.62f);
            else if (roll < 0.75f) yield return Sweep();
            else yield return JumpSlam();
            nextPatternTime = Time.time + Random.Range(0.45f, 0.9f);
        }

        private IEnumerator Combo(int count, float gap, float windup, float recovery)
        {
            state.Force(ActorState.Attack, windup + gap * count + recovery);
            yield return new WaitForSeconds(windup);
            for (var i = 0; i < count && !vitals.IsDead; i++)
            {
                Face(player.position);
                DealDamage(2.2f, 1.2f, attackPower * (i == count - 1 ? 1.2f : 0.82f), true, 44f);
                yield return new WaitForSeconds(gap);
            }
            yield return new WaitForSeconds(recovery);
            ReleaseAttack();
        }

        private IEnumerator Sweep()
        {
            state.Force(ActorState.Attack, 1.55f);
            yield return new WaitForSeconds(0.62f);
            DealDamage(2.8f, 1.2f, attackPower * 1.15f, true, 58f);
            yield return new WaitForSeconds(0.93f);
            ReleaseAttack();
        }

        private IEnumerator JumpSlam()
        {
            state.Force(ActorState.Attack, 1.85f);
            yield return new WaitForSeconds(0.7f);
            var direction = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up).normalized;
            var travelEnds = Time.time + 0.34f;
            while (Time.time < travelEnds)
            {
                controller.Move(direction * 8f * Time.deltaTime);
                yield return null;
            }
            DealDamage(3.4f, 0f, attackPower * 1.45f, false, 76f);
            yield return new WaitForSeconds(0.8f);
            ReleaseAttack();
        }

        private IEnumerator LightningCharge()
        {
            state.Force(ActorState.Attack, 2.1f);
            yield return new WaitForSeconds(0.7f);
            var direction = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up).normalized;
            Face(transform.position + direction);
            var travelEnds = Time.time + 0.58f;
            var damaged = false;
            while (Time.time < travelEnds)
            {
                controller.Move(direction * 14f * Time.deltaTime);
                if (!damaged) damaged = DealDamage(2.1f, 1.0f, attackPower * 1.55f, false, 85f);
                yield return null;
            }
            yield return new WaitForSeconds(0.82f);
            ReleaseAttack();
        }

        private IEnumerator EnterPhaseTwo()
        {
            phaseTwo = true;
            state.Force(ActorState.Cutscene, 1f);
            GameSession.Instance?.LockPlayerForCutscene(1f);
            GameSession.Instance?.ShowNarrative("오류가... 나를 삼키는구나.", 1.8f);
            foreach (var renderer in renderers)
                renderer.material.color = Color.Lerp(renderer.material.color, new Color(0.25f, 0.5f, 1f), 0.62f);
            yield return new WaitForSeconds(1f);
            state.Force(ActorState.Idle);
            nextPatternTime = Time.time + 0.35f;
        }

        private bool DealDamage(float forward, float radius, float damage, bool parryable, float poiseDamage)
        {
            var center = transform.position + Vector3.up + transform.forward * forward;
            foreach (var hit in Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                var target = hit.GetComponentInParent<CombatVitals>();
                if (target == null || target.Faction != Faction.Player) continue;
                target.ReceiveHit(new DamageInfo(gameObject, damage, damage * 0.35f, poiseDamage, parryable,
                    hit.ClosestPoint(center), transform.forward, GetInstanceID() ^ Time.frameCount));
                return true;
            }
            return false;
        }

        public void OnParried(GameObject byActor)
        {
            StopAllCoroutines();
            state.Force(ActorState.Knockdown, 1.8f);
            nextPatternTime = Time.time + 2.2f;
        }

        public void ResetEnemy()
        {
            gameObject.SetActive(true);
            if (controller == null) controller = GetComponent<CharacterController>();
            controller.enabled = false;
            transform.position = home;
            controller.enabled = true;
            GetComponent<CombatVitals>().Revive();
            activated = false;
            phaseTwo = false;
            nextPatternTime = Time.time + 1f;
            if (renderers != null)
                foreach (var renderer in renderers) renderer.material.color = new Color(0.2f, 0.2f, 0.23f);
        }

        private void OnDeath(CombatVitals dead, GameObject killer)
        {
            StopAllCoroutines();
            if (killer != null && killer.TryGetComponent<PlayerProgression>(out var progression))
            {
                progression.AddSouls(dead.SoulsReward);
                progression.EquipBossSpear();
            }
            GameSession.Instance?.CompleteBossFight(this);
            StartCoroutine(HideAfterDeath());
        }

        private IEnumerator HideAfterDeath()
        {
            yield return new WaitForSeconds(3f);
            gameObject.SetActive(false);
        }

        private void MoveTowards(Vector3 target, float speed)
        {
            var direction = Vector3.ProjectOnPlane(target - transform.position, Vector3.up).normalized;
            Face(target);
            controller.Move((direction * speed + Vector3.down * 2f) * Time.deltaTime);
            state.TryEnter(ActorState.Locomotion);
        }

        private void Face(Vector3 target)
        {
            var direction = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 7f);
        }

        private void ReleaseAttack()
        {
            if (!vitals.IsDead && state.Current == ActorState.Attack) state.ReleaseToLocomotion();
        }

        private IEnumerator ReleaseCutsceneAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (!vitals.IsDead && state.Current == ActorState.Cutscene) state.Force(ActorState.Idle);
        }

        private void FindPlayer()
        {
            var candidates = FindObjectsOfType<CombatVitals>();
            foreach (var candidate in candidates)
                if (candidate.Faction == Faction.Player) { player = candidate.transform; break; }
        }
    }
}
