using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ErrorDungeon
{
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        private PlayerMotor player;
        private PlayerProgression progression;
        private CombatVitals playerVitals;
        private readonly List<IResettableEnemy> enemies = new List<IResettableEnemy>();
        private BossDoyunBrain boss;
        private DroppedSouls droppedSouls;
        private Vector3 checkpointPosition;
        private float startedAt;
        private bool respawning;
        private bool doyunDefeated;

        public bool IsPlayerAtCheckpoint => player != null && Vector3.Distance(player.transform.position, checkpointPosition) <= 4f;
        public BossDoyunBrain ActiveBoss { get; private set; }
        public string NarrativeText { get; private set; }
        public float NarrativeUntil { get; private set; }
        public Vector3 CheckpointPosition => checkpointPosition;
        public bool DoyunDefeated => doyunDefeated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            startedAt = Time.realtimeSinceStartup;
        }

        public void RegisterWorld(PlayerMotor playerMotor, Checkpoint checkpoint, BossDoyunBrain doyun,
            IEnumerable<IResettableEnemy> resettableEnemies)
        {
            player = playerMotor;
            progression = player.GetComponent<PlayerProgression>();
            playerVitals = player.GetComponent<CombatVitals>();
            playerVitals.Died += OnPlayerDied;
            checkpointPosition = checkpoint.RespawnPosition;
            boss = doyun;
            enemies.Clear();
            enemies.AddRange(resettableEnemies);
            if (doyun != null) enemies.Add(doyun);

            var saved = SaveSystem.Load();
            if (saved?.player != null)
            {
                progression.Restore(saved.player);
                checkpointPosition = ResolveCheckpoint(saved.player.checkpoint.ToVector3(), checkpoint.RespawnPosition);
                doyunDefeated = saved.doyunDefeated;
                if (doyunDefeated && boss != null) boss.gameObject.SetActive(false);
            }
            else checkpointPosition = ResolveCheckpoint(checkpointPosition, checkpoint.RespawnPosition);
            player.Warp(checkpointPosition);
            ShowNarrative("오류에 잠식된 폐성에서 깨어났다.", 3.2f);
        }

        public void ActivateCheckpoint(Checkpoint checkpoint)
        {
            checkpointPosition = checkpoint.RespawnPosition;
            playerVitals.RestoreAll();
            progression.RefillFlasks();
            foreach (var enemy in enemies)
            {
                if (doyunDefeated && ReferenceEquals(enemy, boss)) continue;
                enemy.ResetEnemy();
            }
            ActiveBoss = null;
            Save();
            ShowNarrative("불씨가 되살아났다.  [L] 레벨 올리기", 2.4f);
        }

        public void BeginBossFight(BossDoyunBrain doyun)
        {
            if (doyunDefeated) return;
            ActiveBoss = doyun;
            doyun.PlayIntro(3f);
            LockPlayerForCutscene(3f);
            ShowNarrative("무너진 장군, 도윤\n\"여기서 더는 나아갈 수 없다.\"", 3f);
        }

        public void LockPlayerForCutscene(float duration)
        {
            if (player == null || playerVitals.IsDead) return;
            player.GetComponent<ActorStateMachine>().Force(ActorState.Cutscene, duration);
            StartCoroutine(ReleasePlayerCutscene(duration));
        }

        public void CompleteBossFight(BossDoyunBrain doyun)
        {
            doyunDefeated = true;
            ActiveBoss = null;
            ShowNarrative("위대한 적을 쓰러뜨렸다\n도윤의 장창을 획득했다", 4f);
            Save();
            var bridge = FindObjectOfType<LeaderboardClient>();
            if (bridge != null) bridge.SubmitBossClear(progression.Level, Mathf.RoundToInt(Time.realtimeSinceStartup - startedAt));
        }

        public void ShowNarrative(string text, float duration)
        {
            NarrativeText = text;
            NarrativeUntil = Time.time + duration;
        }

        public void RecoverDroppedSouls(DroppedSouls target)
        {
            if (target == null || target != droppedSouls) return;
            progression.AddSouls(target.Amount);
            Destroy(target.gameObject);
            droppedSouls = null;
            ShowNarrative("잃어버린 혼을 되찾았다", 2f);
        }

        public void Save()
        {
            if (progression == null) return;
            SaveSystem.Save(new GameSaveData
            {
                player = progression.Capture(checkpointPosition),
                doyunDefeated = doyunDefeated,
                totalPlayTime = Time.realtimeSinceStartup - startedAt
            });
        }

        private void OnPlayerDied(CombatVitals dead, GameObject killer)
        {
            if (!respawning) StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            respawning = true;
            var deathPosition = player.transform.position;
            var souls = progression.RemoveAllSouls();
            if (droppedSouls != null) Destroy(droppedSouls.gameObject);
            if (souls > 0) droppedSouls = GameBootstrap.CreateDroppedSouls(deathPosition, souls);
            ShowNarrative("사망", 2.5f);
            yield return new WaitForSeconds(2.4f);
            player.Warp(checkpointPosition);
            playerVitals.Revive();
            progression.RefillFlasks();
            foreach (var enemy in enemies)
            {
                if (doyunDefeated && ReferenceEquals(enemy, boss)) continue;
                enemy.ResetEnemy();
            }
            ActiveBoss = null;
            respawning = false;
        }

        private IEnumerator ReleasePlayerCutscene(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (player == null || playerVitals.IsDead) yield break;
            var state = player.GetComponent<ActorStateMachine>();
            if (state.Current == ActorState.Cutscene) state.Force(ActorState.Idle);
        }

        private static Vector3 ResolveCheckpoint(Vector3 candidate, Vector3 fallback)
        {
            if (!IsFinite(candidate) || Mathf.Abs(candidate.x) > 54f || Mathf.Abs(candidate.z) > 54f ||
                candidate.y < -2f || candidate.y > 20f)
                candidate = fallback;

            Physics.SyncTransforms();
            var origin = new Vector3(candidate.x, 24f, candidate.z);
            var hits = Physics.SphereCastAll(origin, 0.22f, Vector3.down, 40f, ~0,
                QueryTriggerInteraction.Ignore);
            var foundGround = false;
            var groundHeight = float.NegativeInfinity;
            foreach (var hit in hits)
            {
                if (hit.collider == null || !hit.collider.gameObject.isStatic || hit.normal.y < 0.55f) continue;
                if (hit.point.y <= groundHeight) continue;
                groundHeight = hit.point.y;
                foundGround = true;
            }
            if (foundGround) return new Vector3(candidate.x, groundHeight + 0.08f, candidate.z);

            return fallback;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z) &&
                   !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
        }
    }
}
