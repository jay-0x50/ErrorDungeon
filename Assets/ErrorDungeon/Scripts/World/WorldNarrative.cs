using System.Collections.Generic;
using UnityEngine;

namespace ErrorDungeon
{
    public sealed class WorldStory : MonoBehaviour
    {
        public static WorldStory Instance { get; private set; }

        private readonly HashSet<string> discoveries = new HashSet<string>();
        private readonly List<StoryRelic> relics = new List<StoryRelic>();
        private SealedWorldGate gate;

        public int SealCount { get; private set; }
        public int SealTarget { get; private set; } = 3;
        public string RegionName { get; private set; } = "재의 변경";
        public string Objective
        {
            get
            {
                if (GameSession.Instance != null && GameSession.Instance.DoyunDefeated)
                    return "도윤의 기억을 품고, 붉은 등대 너머를 바라보라";
                if (SealCount < SealTarget)
                    return $"봉인의 기억을 수습하라  {SealCount}/{SealTarget}";
                if (gate != null && !gate.IsOpen)
                    return "세 기억을 폐성의 봉인문에 돌려놓아라";
                return "무너진 장군 도윤을 해방하라";
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void RegisterGate(SealedWorldGate value) => gate = value;

        public void RegisterRelic(StoryRelic relic)
        {
            if (relic != null && !relics.Contains(relic)) relics.Add(relic);
        }

        public string[] CaptureDiscoveries()
        {
            var values = new string[discoveries.Count];
            discoveries.CopyTo(values);
            return values;
        }

        public bool IsGateOpen => gate != null && gate.IsOpen;

        public void Restore(string[] savedDiscoveries, bool gateOpen)
        {
            discoveries.Clear();
            if (savedDiscoveries != null)
                foreach (var id in savedDiscoveries)
                    if (!string.IsNullOrEmpty(id)) discoveries.Add(id);
            SealCount = 0;
            foreach (var relic in relics)
            {
                var restored = discoveries.Contains(relic.RelicId);
                relic.RestoreDiscovered(restored);
                if (restored && relic.GrantsSeal) SealCount++;
            }
            SealCount = Mathf.Min(SealTarget, SealCount);
            if (gateOpen) gate?.RestoreOpen();
        }

        public bool Discover(string id, string title, string lore, bool grantsSeal, Transform memory = null)
        {
            if (string.IsNullOrEmpty(id) || !discoveries.Add(id)) return false;
            if (grantsSeal) SealCount = Mathf.Min(SealTarget, SealCount + 1);
            var revelation = string.Empty;
            if (grantsSeal && SealCount == 1)
                revelation = "\n\n도윤은 처음에는 명령보다 성밖의 사람을 택했다.";
            else if (grantsSeal && SealCount == 2)
                revelation = "\n\n그러나 왕의 마지막 명령이 도착하자, 그는 문을 닫았다.";
            else if (grantsSeal && SealCount == 3)
                revelation = "\n\n그 명령을 성까지 운반한 자의 이름은 기록에서 도려내졌다.";
            var suffix = grantsSeal ? $"\n\n[봉인의 기억 {SealCount}/{SealTarget}]{revelation}" : string.Empty;
            GameSession.Instance?.PlayMemoryCinematic(memory, title);
            GameSession.Instance?.ShowNarrative($"{title}\n\"{lore}\"{suffix}", 6.5f);
            GameSession.Instance?.Save();
            if (SealCount == SealTarget)
                Invoke(nameof(AnnounceGate), 2.2f);
            return true;
        }

        public void EnterRegion(string region, string epigraph)
        {
            if (string.IsNullOrEmpty(region) || RegionName == region) return;
            RegionName = region;
            GameSession.Instance?.ShowNarrative($"— {region} —\n{epigraph}", 4.2f);
        }

        public void GateOpened()
        {
            GameSession.Instance?.ShowNarrative("세 개의 기억이 맞물렸다.\n폐성은 마지막 패배를 다시 열어 보였다.", 5f);
        }

        private void AnnounceGate()
        {
            GameSession.Instance?.ShowNarrative("멀리서 돌이 우는 소리가 들렸다.\n이제 폐성의 봉인을 열 수 있다.", 4.5f);
        }
    }

    public sealed class StoryRelic : MonoBehaviour, IInteractable
    {
        private string relicId;
        private string title;
        private string lore;
        private bool grantsSeal;
        private bool discovered;
        private Renderer[] renderers;

        public string RelicId => relicId;
        public bool GrantsSeal => grantsSeal;
        public string Prompt => discovered ? $"[E] {title} 다시 읽기" : $"[E] {title} 조사";

        public void Configure(string id, string displayTitle, string loreText, bool isSeal)
        {
            relicId = id;
            title = displayTitle;
            lore = loreText;
            grantsSeal = isSeal;
            renderers = GetComponentsInChildren<Renderer>();
            WorldStory.Instance?.RegisterRelic(this);
        }

        public void RestoreDiscovered(bool value)
        {
            discovered = value;
            if (!discovered || renderers == null) return;
            foreach (var target in renderers)
                if (target.material.HasProperty("_EmissionColor"))
                    target.material.SetColor("_EmissionColor", new Color(0.08f, 0.22f, 0.25f));
        }

        public void Interact(PlayerInteractor player)
        {
            if (!discovered)
            {
                discovered = WorldStory.Instance != null &&
                             WorldStory.Instance.Discover(relicId, title, lore, grantsSeal, transform);
                if (discovered)
                {
                    foreach (var target in renderers)
                    {
                        if (target.material.HasProperty("_EmissionColor"))
                            target.material.SetColor("_EmissionColor", new Color(0.08f, 0.22f, 0.25f));
                    }
                }
                return;
            }
            GameSession.Instance?.ShowNarrative($"{title}\n\"{lore}\"", 5f);
        }
    }

    public sealed class SealedWorldGate : MonoBehaviour, IInteractable
    {
        private readonly List<GameObject> blockers = new List<GameObject>();
        public bool IsOpen { get; private set; }
        public string Prompt
        {
            get
            {
                var story = WorldStory.Instance;
                if (IsOpen) return null;
                if (story == null || story.SealCount < story.SealTarget)
                    return $"[E] 봉인의 문  ({story?.SealCount ?? 0}/{story?.SealTarget ?? 3})";
                return "[E] 기억을 돌려놓아 봉인을 푼다";
            }
        }

        public void RegisterBlocker(GameObject blocker)
        {
            if (blocker != null) blockers.Add(blocker);
        }

        public void Interact(PlayerInteractor player)
        {
            if (IsOpen) return;
            var story = WorldStory.Instance;
            if (story == null || story.SealCount < story.SealTarget)
            {
                GameSession.Instance?.ShowNarrative("홈은 세 곳인데, 돌아온 기억은 아직 부족하다.", 3.2f);
                return;
            }

            IsOpen = true;
            GameSession.Instance?.PlayGateCinematic(transform);
            foreach (var blocker in blockers)
                if (blocker != null) blocker.SetActive(false);
            story.GateOpened();
            GameSession.Instance?.Save();
        }

        public void RestoreOpen()
        {
            IsOpen = true;
            foreach (var blocker in blockers)
                if (blocker != null) blocker.SetActive(false);
        }
    }

    public sealed class GuidingEcho : MonoBehaviour, IInteractable
    {
        private readonly string[] lines =
        {
            "성은 벽으로 닫힌 것이 아니야. 장군이 버리지 못한 세 기억으로 닫혔지.",
            "서쪽 회백숲은 그의 맹세를, 동쪽 침몰기록소는 명령을 삼켰어.",
            "북쪽의 부서진 마을에는 마지막 후퇴가 남아 있어. 셋을 모아 문으로 가.",
            "네 이름이 지워진 건 죽어서가 아니야. 마지막 폐문 명령서를 운반한 사람이 너였기 때문이야.",
            "도윤은 네가 가져온 명령으로 문을 닫았고, 너무 늦게 그 문을 다시 열었어.",
            "오류는 거짓말을 만들지 않아. 우리가 숨긴 것을 너무 오래 되풀이할 뿐이야."
        };
        private int lineIndex;
        public string Prompt => "[E] 불씨의 안내자와 대화";

        public void Interact(PlayerInteractor player)
        {
            GameSession.Instance?.ShowNarrative($"불씨의 안내자\n\"{lines[lineIndex]}\"", 6f);
            lineIndex = (lineIndex + 1) % lines.Length;
        }
    }

    [RequireComponent(typeof(Collider))]
    public sealed class RegionVolume : MonoBehaviour
    {
        [SerializeField] private string regionName;
        [SerializeField] private string epigraph;
        private bool entered;

        public void Configure(string region, string subtitle)
        {
            regionName = region;
            epigraph = subtitle;
            var trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (entered || !other.CompareTag("Player")) return;
            entered = true;
            WorldStory.Instance?.EnterRegion(regionName, epigraph);
        }
    }

    public sealed class AmbientMotion : MonoBehaviour
    {
        [SerializeField] private Vector3 rotation = new Vector3(0f, 18f, 0f);
        [SerializeField] private float hover = 0.12f;
        [SerializeField] private float frequency = 1.4f;
        private Vector3 origin;
        private float phase;

        public void Configure(Vector3 rotationSpeed, float hoverAmount, float hoverFrequency, float phaseOffset = 0f)
        {
            rotation = rotationSpeed;
            hover = hoverAmount;
            frequency = hoverFrequency;
            phase = phaseOffset;
        }

        private void Start() => origin = transform.localPosition;

        private void Update()
        {
            transform.Rotate(rotation * Time.deltaTime, Space.Self);
            transform.localPosition = origin + Vector3.up * (Mathf.Sin(Time.time * frequency + phase) * hover);
        }
    }

    public sealed class WorldDetailChunk : MonoBehaviour
    {
        private Transform player;
        private GameObject content;
        private float activeDistance;
        private float nextCheck;

        public void Configure(GameObject detailContent, float distance)
        {
            content = detailContent;
            activeDistance = distance;
        }

        private void Update()
        {
            if (Application.isBatchMode || Time.time < nextCheck || content == null) return;
            nextCheck = Time.time + 0.55f;
            if (player == null)
            {
                var candidate = GameObject.FindGameObjectWithTag("Player");
                player = candidate != null ? candidate.transform : null;
            }
            if (player == null) return;
            var delta = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up);
            var shouldBeActive = delta.sqrMagnitude <= activeDistance * activeDistance;
            if (content.activeSelf != shouldBeActive) content.SetActive(shouldBeActive);
        }
    }
}
