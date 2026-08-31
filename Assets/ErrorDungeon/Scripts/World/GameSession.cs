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
        private SoulsCamera cinematicCamera;
        private float cinematicUntil;
        private int cinematicVersion;

        public bool IsPlayerAtCheckpoint => player != null && Vector3.Distance(player.transform.position, checkpointPosition) <= 4f;
        public BossDoyunBrain ActiveBoss { get; private set; }
        public string NarrativeText { get; private set; }
        public float NarrativeUntil { get; private set; }
        public string CinematicChapter { get; private set; }
        public bool CinematicActive => Time.time < cinematicUntil ||
                                       (cinematicCamera != null && cinematicCamera.IsCinematic);
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
            cinematicCamera = FindObjectOfType<SoulsCamera>();

            var saved = SaveSystem.Load();
            var newJourney = saved?.player == null;
            if (saved?.player != null)
            {
                progression.Restore(saved.player);
                checkpointPosition = ResolveCheckpoint(saved.player.checkpoint.ToVector3(), checkpoint.RespawnPosition);
                doyunDefeated = saved.doyunDefeated;
                if (doyunDefeated && boss != null) boss.gameObject.SetActive(false);
                WorldStory.Instance?.Restore(saved.discoveredRelics, saved.memoryGateOpen);
            }
            else checkpointPosition = ResolveCheckpoint(checkpointPosition, checkpoint.RespawnPosition);
            player.Warp(checkpointPosition);
            if (newJourney) StartCoroutine(OpeningSequence());
            else ShowNarrative("불씨는 네가 되찾은 기억을 잊지 않았다.", 3.2f);
        }

        public void ActivateCheckpoint(Checkpoint checkpoint)
        {
            checkpointPosition = ResolveCheckpoint(checkpoint.RespawnPosition, checkpoint.RespawnPosition);
            playerVitals.RestoreAll();
            progression.RefillFlasks();
            foreach (var enemy in enemies)
            {
                if (doyunDefeated && ReferenceEquals(enemy, boss)) continue;
                enemy.ResetEnemy();
            }
            ActiveBoss = null;
            Save();
            ShowNarrative("불씨가 되살아났다. 쓰러진 것들도 다시 기억을 얻는다.", 3.1f);
        }

        public void BeginBossFight(BossDoyunBrain doyun)
        {
            if (doyunDefeated) return;
            ActiveBoss = doyun;
            const float duration = 5.2f;
            doyun.PlayIntro(duration);
            LockPlayerForCutscene(duration);
            var playerPosition = player.transform.position;
            var bossPosition = doyun.transform.position;
            var midpoint = Vector3.Lerp(playerPosition, bossPosition, 0.5f);
            PlayCinematic("제4장 · 문을 연 장군", duration,
                new CinematicShot(midpoint + new Vector3(8.5f, 5.2f, -7f), midpoint + Vector3.up * 1.4f, 1.7f, 58f),
                new CinematicShot(bossPosition + new Vector3(-3.8f, 2.1f, -4.6f), bossPosition + Vector3.up * 1.65f, 1.6f, 43f),
                new CinematicShot(playerPosition + new Vector3(2.8f, 2.3f, -4.2f), bossPosition + Vector3.up * 1.5f, 1.9f, 50f));
            StartCoroutine(BossIntroNarrative());
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
            LockPlayerForCutscene(3f);
            var focus = doyun.transform.position + Vector3.up * 1.25f;
            PlayCinematic("종장 · 실패를 기억하는 자", 3f,
                new CinematicShot(doyun.transform.position + new Vector3(4.8f, 2.6f, -4.8f), focus, 1.5f, 45f),
                new CinematicShot(doyun.transform.position + new Vector3(-2.6f, 5.4f, -6.2f), focus, 1.5f, 54f));
            ShowNarrative("도윤은 마지막으로 문밖을 바라보았다.\n도윤의 장창 · 실패한 왕국의 기억을 획득했다", 6f);
            Save();
            var bridge = FindObjectOfType<LeaderboardClient>();
            if (bridge != null) bridge.SubmitBossClear(progression.Level, Mathf.RoundToInt(Time.realtimeSinceStartup - startedAt));
        }

        public void ShowNarrative(string text, float duration)
        {
            NarrativeText = text;
            NarrativeUntil = Time.time + duration;
        }

        public void PlayMemoryCinematic(Transform memory, string title)
        {
            if (memory == null || player == null) return;
            const float duration = 2.8f;
            LockPlayerForCutscene(duration);
            var focus = memory.position + Vector3.up * 0.95f;
            var fromPlayer = Vector3.ProjectOnPlane(player.transform.position - memory.position, Vector3.up);
            if (fromPlayer.sqrMagnitude < 0.1f) fromPlayer = -memory.forward;
            fromPlayer.Normalize();
            var side = Vector3.Cross(Vector3.up, fromPlayer).normalized;
            PlayCinematic(title, duration,
                new CinematicShot(focus + fromPlayer * 4.2f + side * 1.8f + Vector3.up * 1.5f,
                    focus, 1.35f, 47f),
                new CinematicShot(focus + fromPlayer * 2.7f - side * 1.2f + Vector3.up * 0.5f,
                    focus, 1.45f, 38f));
        }

        public void PlayGateCinematic(Transform gateTransform)
        {
            if (gateTransform == null) return;
            const float duration = 3.6f;
            LockPlayerForCutscene(duration);
            var focus = gateTransform.position + Vector3.up * 4.6f;
            PlayCinematic("세 기억의 문", duration,
                new CinematicShot(gateTransform.position + new Vector3(-10f, 7.5f, -13f), focus, 1.8f, 56f),
                new CinematicShot(gateTransform.position + new Vector3(7f, 4.8f, -9f), focus, 1.8f, 44f));
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
                discoveredRelics = WorldStory.Instance?.CaptureDiscoveries(),
                memoryGateOpen = WorldStory.Instance != null && WorldStory.Instance.IsGateOpen,
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

        private IEnumerator OpeningSequence()
        {
            const float duration = 5.6f;
            LockPlayerForCutscene(duration);
            var origin = player.transform.position;
            var roadAhead = origin + Vector3.forward * 24f + Vector3.up * 1.4f;
            PlayCinematic("서장 · 이름이 지워진 순례자", duration,
                new CinematicShot(origin + new Vector3(-8f, 7f, -11f), origin + Vector3.up * 1.1f, 1.8f, 58f),
                new CinematicShot(origin + new Vector3(3.4f, 2.2f, -4.3f), origin + Vector3.up * 1.25f, 1.5f, 42f),
                new CinematicShot(origin + new Vector3(0f, 5.2f, 4.5f), roadAhead, 2.3f, 51f));
            ShowNarrative("왕국은 멸망했지만, 멸망을 명령한 기억은 죽지 않았다.", 2.1f);
            yield return new WaitForSeconds(2.1f);
            ShowNarrative("너의 이름은 지워졌다.\n그러나 도윤이라는 이름만 칼자국 아래 남아 있다.", 2.2f);
            yield return new WaitForSeconds(2.15f);
            ShowNarrative("세 번 버려진 기억을 되찾아 폐성의 문으로 가라.", 3.2f);
        }

        private IEnumerator BossIntroNarrative()
        {
            ShowNarrative("무너진 장군, 도윤\n\"그날 나는 명령을 지켰고, 사람을 버렸다.\"", 2.6f);
            yield return new WaitForSeconds(2.55f);
            ShowNarrative("도윤\n\"문을 다시 여는 자가 너라면, 내 실패부터 베고 지나가라.\"", 3.2f);
        }

        private void PlayCinematic(string chapter, float duration, params CinematicShot[] shots)
        {
            if (cinematicCamera == null) cinematicCamera = FindObjectOfType<SoulsCamera>();
            CinematicChapter = chapter;
            cinematicUntil = Time.time + duration;
            var version = ++cinematicVersion;
            cinematicCamera?.PlayCinematic(shots);
            StartCoroutine(ClearCinematicChapter(version, duration));
        }

        private IEnumerator ClearCinematicChapter(int version, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (version == cinematicVersion) CinematicChapter = null;
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
            if (!IsFinite(candidate) || Mathf.Abs(candidate.x) > 154f || Mathf.Abs(candidate.z) > 154f ||
                candidate.y < -4f || candidate.y > 80f)
                candidate = fallback;

            Physics.SyncTransforms();
            var origin = new Vector3(candidate.x, 84f, candidate.z);
            var hits = Physics.SphereCastAll(origin, 0.22f, Vector3.down, 100f, ~0,
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

            var proceduralY = ProceduralArt.GroundHeight(candidate.x, candidate.z) + 0.08f;
            if (Mathf.Abs(candidate.x) <= 159f && Mathf.Abs(candidate.z) <= 159f)
                return new Vector3(candidate.x, Mathf.Max(candidate.y, proceduralY), candidate.z);
            return fallback;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z) &&
                   !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
        }
    }
}
