using System.Collections;
using UnityEngine;

namespace ErrorDungeon
{
    [RequireComponent(typeof(CharacterController), typeof(CombatVitals), typeof(ActorStateMachine))]
    public sealed class EnemyBrain : MonoBehaviour, IParryReceiver, IResettableEnemy
    {
        private CharacterController controller;
        private CombatVitals vitals;
        private ActorStateMachine state;
        private ProceduralHumanoidAnimator combatAnimator;
        private Transform player;
        private Vector3 home;
        private string dataId;
        private float attackPower;
        private float nextAttackTime;
        private bool initialized;

        public void Initialize(string enemyDataId, Vector3 spawn)
        {
            dataId = enemyDataId;
            home = spawn;
            controller = GetComponent<CharacterController>();
            vitals = GetComponent<CombatVitals>();
            state = GetComponent<ActorStateMachine>();
            var data = GameDataRepository.Enemy(dataId);
            if (data == null) return;
            attackPower = data.@base.attack;
            var reward = data.tier == "elite" ? 420 : 55;
            vitals.Configure(Faction.Enemy, data.@base.hp, data.@base.stamina, data.@base.defense, data.@base.poise, reward);
            vitals.Damaged -= OnDamaged;
            vitals.Died -= OnDeath;
            vitals.Damaged += OnDamaged;
            vitals.Died += OnDeath;
            initialized = true;
        }

        private void Start()
        {
            if (!initialized) Initialize("soldier_spear_01", transform.position);
            combatAnimator = GetComponentInChildren<ProceduralHumanoidAnimator>();
            FindPlayer();
        }

        private void OnDestroy()
        {
            if (vitals == null) return;
            vitals.Damaged -= OnDamaged;
            vitals.Died -= OnDeath;
        }

        private void Update()
        {
            if (vitals.IsDead || Application.isBatchMode) return;
            if (player == null) FindPlayer();
            if (player == null || state.IsLocked) return;

            var delta = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up);
            var distance = delta.magnitude;
            if (distance > 16f)
            {
                if (Vector3.Distance(transform.position, home) > 1f) MoveTowards(home, 2.3f);
                return;
            }
            if (distance > 2.35f) MoveTowards(player.position, dataId == "elite_castle_guard" ? 3.0f : 2.6f);
            else
            {
                Face(player.position);
                if (Time.time >= nextAttackTime) StartCoroutine(AttackRoutine());
            }
        }

        private IEnumerator AttackRoutine()
        {
            var elite = dataId == "elite_castle_guard";
            var windup = elite ? 0.42f : 0.28f;
            var recovery = elite ? 0.64f : 0.44f;
            if (combatAnimator == null) combatAnimator = GetComponentInChildren<ProceduralHumanoidAnimator>();
            combatAnimator?.BeginAttack(windup, recovery, GetInstanceID() ^ Time.frameCount);
            state.Force(ActorState.Attack, windup + recovery);
            yield return new WaitForSeconds(windup);
            if (!vitals.IsDead && state.Current == ActorState.Attack)
            {
                var center = transform.position + Vector3.up + transform.forward * 1.25f;
                foreach (var hit in Physics.OverlapSphere(center, elite ? 1.3f : 1.0f, ~0, QueryTriggerInteraction.Ignore))
                {
                    var target = hit.GetComponentInParent<CombatVitals>();
                    if (target == null || target.Faction != Faction.Player) continue;
                    target.ReceiveHit(new DamageInfo(gameObject, attackPower * (elite ? 1.2f : 1f),
                        attackPower * 0.35f, elite ? 48f : 24f, true, hit.ClosestPoint(center), transform.forward,
                        GetInstanceID() ^ Time.frameCount));
                    break;
                }
            }
            yield return new WaitForSeconds(recovery);
            nextAttackTime = Time.time + Random.Range(0.35f, 0.9f);
            if (!vitals.IsDead && state.Current == ActorState.Attack) state.ReleaseToLocomotion();
        }

        private void OnDamaged(CombatVitals damaged, DamageInfo hit, float damage)
        {
            StopAllCoroutines();
            nextAttackTime = Time.time + Random.Range(0.35f, 0.55f);
        }

        public void OnParried(GameObject byActor)
        {
            StopAllCoroutines();
            state.Force(ActorState.Knockdown, 1.8f);
        }

        public void ResetEnemy()
        {
            gameObject.SetActive(true);
            if (controller == null) controller = GetComponent<CharacterController>();
            controller.enabled = false;
            transform.position = home;
            controller.enabled = true;
            GetComponent<CombatVitals>().Revive();
            nextAttackTime = Time.time + 1f;
        }

        private void OnDeath(CombatVitals dead, GameObject killer)
        {
            StopAllCoroutines();
            if (killer != null && killer.TryGetComponent<PlayerProgression>(out var progression))
            {
                progression.AddSouls(dead.SoulsReward);
                var data = GameDataRepository.Enemy(dataId);
                if (data?.drops != null)
                {
                    foreach (var drop in data.drops)
                    {
                        if (drop.itemId != "mat_iron_shard" || Random.value > drop.chance) continue;
                        progression.AddIronShards(Random.Range(drop.min, drop.max + 1));
                    }
                }
            }
            StartCoroutine(HideAfterDeath());
        }

        private IEnumerator HideAfterDeath()
        {
            yield return new WaitForSeconds(2.5f);
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
            if (direction.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 8f);
        }

        private void FindPlayer()
        {
            var playerVitals = System.Array.Find(FindObjectsOfType<CombatVitals>(), v => v.Faction == Faction.Player);
            player = playerVitals != null ? playerVitals.transform : null;
        }
    }
}
