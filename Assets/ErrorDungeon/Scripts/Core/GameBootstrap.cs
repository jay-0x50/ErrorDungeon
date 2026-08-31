using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ErrorDungeon
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const float WorldSize = 320f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<GameBootstrap>() != null) return;
            new GameObject("ErrorDungeon_Bootstrap").AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            Application.runInBackground = true;
#if UNITY_SERVER
            StartHeadlessService();
            return;
#endif
            Application.targetFrameRate = 60;
            ProceduralArt.Initialize();
            BuildLighting();
            BuildWorld();
        }

#if UNITY_SERVER
        private static void StartHeadlessService()
        {
            // This project is single-player. The dedicated Linux process only advertises
            // build health to the API, so it must not construct or simulate the visual world.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 10;

            var online = new GameObject("LinuxServerBridge").AddComponent<LeaderboardClient>();
            online.StartServerHeartbeat();
            Debug.Log("ERROR_DUNGEON_HEADLESS_READY mode=heartbeat target_fps=10");
        }
#endif

        private static void BuildLighting()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.026f, 0.034f, 0.052f);
            RenderSettings.fogDensity = 0.0062f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.105f, 0.125f, 0.17f);
            RenderSettings.ambientEquatorColor = new Color(0.055f, 0.065f, 0.08f);
            RenderSettings.ambientGroundColor = new Color(0.022f, 0.023f, 0.026f);
            RenderSettings.reflectionIntensity = 0.42f;

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader) { name = "Error Dungeon night sky" };
                sky.SetColor("_SkyTint", new Color(0.10f, 0.13f, 0.20f));
                sky.SetColor("_GroundColor", new Color(0.025f, 0.023f, 0.029f));
                sky.SetFloat("_AtmosphereThickness", 0.48f);
                sky.SetFloat("_Exposure", 0.62f);
                RenderSettings.skybox = sky;
            }

            var moon = new GameObject("Cold moonlight");
            var moonLight = moon.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = new Color(0.56f, 0.65f, 0.86f);
            moonLight.intensity = 1.05f;
            moonLight.shadows = LightShadows.Soft;
            moonLight.shadowStrength = 0.76f;
            moon.transform.rotation = Quaternion.Euler(38f, -31f, 0f);
            RenderSettings.sun = moonLight;

            QualitySettings.shadowDistance = 125f;
            QualitySettings.antiAliasing = Application.platform == RuntimePlatform.WebGLPlayer ? 2 : 4;
        }

        private static void BuildWorld()
        {
            var world = new GameObject("World_The_Failed_Kingdom");
            new GameObject("WorldStory").AddComponent<WorldStory>();
            ProceduralArt.CreateTerrain("Weathered open-world terrain", world.transform, WorldSize, 96);
            ProceduralArt.Block("Drowned lowland water", world.transform, new Vector3(0f, -1.75f, 0f),
                new Vector3(WorldSize - 8f, 0.16f, WorldSize - 8f), ProceduralArt.Water, collider: false);

            BuildPilgrimRoad(world.transform);
            BuildWorldBoundary(world.transform);
            var firstCheckpoint = BuildAshenVerge(world.transform);
            BuildGreywood(world.transform);
            BuildSunkenArchive(world.transform);
            BuildBrokenVillage(world.transform);
            var gate = BuildCastleAndGate(world.transform);
            WorldStory.Instance.RegisterGate(gate);
            ProceduralArt.CreateAtmosphere(world.transform, new Vector3(0f, 9f, 12f), new Vector3(255f, 22f, 280f), 420);

            Physics.SyncTransforms();
            var session = new GameObject("GameSession").AddComponent<GameSession>();
            var player = CreatePlayer(firstCheckpoint.RespawnPosition);
            var enemies = SpawnWorldEnemies();
            var boss = CreateBoss(GroundPoint(0f, 132f));

            Physics.SyncTransforms();
            var online = new GameObject("LinuxServerBridge").AddComponent<LeaderboardClient>();
            var hud = new GameObject("SoulsHUD").AddComponent<SoulsHUD>();
            hud.Bind(player.gameObject);
            CreateCamera(player.transform);
            session.RegisterWorld(player, firstCheckpoint, boss, enemies);
            Debug.Log($"ERROR_DUNGEON_WORLD_READY size={WorldSize:0} regions=5 enemies={enemies.Count + 1} " +
                      $"renderers={FindObjectsOfType<Renderer>().Length} relics={FindObjectsOfType<StoryRelic>().Length}");
            if (Application.isBatchMode) online.StartServerHeartbeat();
        }

        private static void BuildPilgrimRoad(Transform world)
        {
            var road = new GameObject("The broken pilgrim road");
            road.transform.SetParent(world, false);
            for (var i = 0; i < 34; i++)
            {
                var z = -118f + i * 6.9f;
                var x = Mathf.Sin(i * 1.47f) * (i % 5 == 0 ? 1.7f : 0.72f);
                var y = ProceduralArt.GroundHeight(x, z) + 0.11f;
                var slab = ProceduralArt.Block($"Road stone {i:00}", road.transform, new Vector3(x, y, z),
                    new Vector3(7.2f + i % 3, 0.18f, 5.3f + (i + 1) % 2),
                    i % 6 == 0 ? ProceduralArt.Moss : ProceduralArt.DarkStone,
                    new Vector3(0f, Mathf.Sin(i * 0.8f) * 4f, Mathf.Cos(i) * 0.7f));
                slab.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            }

            for (var i = 0; i < 18; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var z = -108f + i * 12.2f;
                var x = side * (7.8f + (i % 4) * 1.25f);
                var y = ProceduralArt.GroundHeight(x, z);
                ProceduralArt.Rock($"Road marker {i:00}", road.transform, new Vector3(x, y + 0.65f, z),
                    new Vector3(0.55f, 1.5f + i % 3 * 0.25f, 0.5f), ProceduralArt.Stone, 310 + i);
                if (i % 3 == 0)
                {
                    ProceduralArt.Block("Torn oath banner", road.transform, new Vector3(x + side * 0.42f, y + 2.5f, z),
                        new Vector3(0.72f, 1.65f, 0.035f), ProceduralArt.Cloth,
                        new Vector3(0f, side < 0 ? 18f : -18f, side * 7f), false);
                }
            }
        }

        private static void BuildWorldBoundary(Transform world)
        {
            var boundary = new GameObject("Distant black cliffs");
            boundary.transform.SetParent(world, false);
            for (var i = 0; i < 36; i++)
            {
                var angle = i * Mathf.PI * 2f / 36f;
                var radius = 151f + Mathf.Sin(i * 4.3f) * 5f;
                var position = new Vector3(Mathf.Sin(angle) * radius, 3.5f, Mathf.Cos(angle) * radius);
                var scale = new Vector3(9f + i % 4 * 2.5f, 12f + i % 7 * 3f, 7f + i % 3 * 2f);
                ProceduralArt.Rock($"Cliff tooth {i:00}", boundary.transform, position, scale, ProceduralArt.DarkStone, 800 + i, true);
            }

            BuildLandmarkKeep(boundary.transform);
            BuildErrorMoon(boundary.transform);
        }

        private static Checkpoint BuildAshenVerge(Transform world)
        {
            var region = new GameObject("Region_Ashen_Verger");
            region.transform.SetParent(world, false);
            AddRegionVolume(region.transform, "재의 변경", "꺼진 순례길에 아직 하나의 불씨가 남았다.", new Vector3(0f, 2f, -103f), new Vector3(54f, 8f, 43f));

            ProceduralArt.RuinArch("Gate of waking", region.transform, GroundPoint(0f, -91f), 11f, 8.2f, -4f, ProceduralArt.Stone);
            for (var i = 0; i < 12; i++)
            {
                var x = -25f + i * 4.8f;
                var z = -78f + Mathf.Sin(i * 1.8f) * 7f;
                var point = GroundPoint(x, z);
                ProceduralArt.Rock($"Wake ruin {i}", region.transform, point + Vector3.up * (1f + i % 3 * 0.4f),
                    new Vector3(1.2f + i % 2, 2.4f + i % 3, 1.1f), i % 4 == 0 ? ProceduralArt.Moss : ProceduralArt.Stone, 30 + i);
            }

            var checkpoint = CreateCheckpoint(GroundPoint(0f, -111f), "마지막 순례자의 불씨");
            CreateGuidingEcho(GroundPoint(-4.5f, -105f));
            CreateLoreRelic(region.transform, "record_waking", "젖은 이름표",
                "내 이름은 지워졌는데, 도윤이라는 이름만 칼자국 아래 남아 있다.", false,
                GroundPoint(8f, -84f));
            return checkpoint;
        }

        private static void BuildGreywood(Transform world)
        {
            var region = new GameObject("Region_Greywood");
            region.transform.SetParent(world, false);
            AddRegionVolume(region.transform, "회백숲", "나무는 타지 않았으나 모든 잎이 재가 되었다.", new Vector3(-72f, 2f, -24f), new Vector3(72f, 10f, 84f));
            var detail = CreateDetailChunk(region.transform, "Greywood silhouettes", new Vector3(-72f, 0f, -24f), 122f);
            for (var i = 0; i < 38; i++)
            {
                var angle = i * 2.39996f;
                var radius = 13f + i * 1.36f;
                var x = -74f + Mathf.Cos(angle) * radius;
                var z = -27f + Mathf.Sin(angle) * radius * 0.72f;
                ProceduralArt.DeadTree($"Ash tree {i:00}", detail.transform, GroundPoint(x, z), 0.68f + i % 5 * 0.11f, 150 + i);
            }
            ProceduralArt.RuinArch("Oath shrine", region.transform, GroundPoint(-73f, -28f), 9f, 7f, 22f, ProceduralArt.Moss);
            BuildCorruptionCluster(region.transform, new Vector3(-93f, ProceduralArt.GroundHeight(-93f, -8f), -8f), 14, false);
            CreateLoreRelic(region.transform, "seal_oath", "첫 번째 기억 · 지키겠다는 맹세",
                "백성이 아니라 성문을 지키라는 명이었다. 나는 명을 어기고 사람 곁에 섰다.", true,
                GroundPoint(-74f, -24f) + Vector3.up * 0.15f);
        }

        private static void BuildSunkenArchive(Transform world)
        {
            var region = new GameObject("Region_Sunken_Archive");
            region.transform.SetParent(world, false);
            AddRegionVolume(region.transform, "침몰기록소", "물이 기록을 지웠고, 오류는 지워진 문장을 되풀이했다.", new Vector3(70f, 2f, 4f), new Vector3(76f, 10f, 88f));
            var detail = CreateDetailChunk(region.transform, "Archive columns", new Vector3(70f, 0f, 4f), 124f);
            ProceduralArt.Block("Archive black pool", region.transform, new Vector3(70f, -0.82f, 5f), new Vector3(52f, 0.15f, 45f), ProceduralArt.Water, collider: false);
            for (var i = 0; i < 20; i++)
            {
                var row = i / 5;
                var column = i % 5;
                var x = 50f + column * 10f + (row % 2) * 2f;
                var z = -12f + row * 12f;
                var ground = ProceduralArt.GroundHeight(x, z);
                var height = 3.5f + (i * 7 % 5) * 1.35f;
                ProceduralArt.Frustum($"Drowned column {i:00}", detail.transform,
                    new Vector3(x, ground - 0.2f, z), 0.72f, 0.52f, height, 8,
                    i % 4 == 0 ? ProceduralArt.Moss : ProceduralArt.Stone,
                    new Vector3(i % 3 * 3f, i * 13f, (i % 4 - 2) * 2f));
                ProceduralArt.Block("Column capital", detail.transform, new Vector3(x, ground + height - 0.15f, z),
                    new Vector3(2.2f, 0.38f, 2.2f), ProceduralArt.DarkStone, new Vector3(0f, i * 13f, 0f), false);
            }
            ProceduralArt.RuinArch("Archive threshold", region.transform, GroundPoint(69f, -19f), 13f, 8.5f, 0f, ProceduralArt.DarkStone);
            BuildCorruptionCluster(region.transform, new Vector3(88f, ProceduralArt.GroundHeight(88f, 26f), 26f), 18, true);
            CreateLoreRelic(region.transform, "seal_order", "두 번째 기억 · 닫으라는 명령",
                "대문을 닫으면 성은 산다. 문밖의 이름 삼천은 기록에서 지워라.", true,
                GroundPoint(70f, 8f) + Vector3.up * 0.2f);
            CreateCheckpoint(GroundPoint(42f, -18f), "물먹은 기록자의 불씨");
        }

        private static void BuildBrokenVillage(Transform world)
        {
            var region = new GameObject("Region_Broken_Village");
            region.transform.SetParent(world, false);
            AddRegionVolume(region.transform, "부서진 마을", "후퇴의 종은 울리지 않았다. 종지기까지 성 안으로 달아났으므로.", new Vector3(0f, 2f, 61f), new Vector3(76f, 10f, 58f));
            var detail = CreateDetailChunk(region.transform, "Village remains", new Vector3(0f, 0f, 61f), 130f);
            for (var i = 0; i < 14; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var x = side * (14f + i % 4 * 6.3f);
                var z = 39f + (i / 2) * 7.4f;
                var ground = ProceduralArt.GroundHeight(x, z);
                var wallHeight = 2.4f + i % 4 * 0.65f;
                ProceduralArt.Block($"House wall {i:00}", detail.transform, new Vector3(x, ground + wallHeight * 0.5f, z),
                    new Vector3(6.4f, wallHeight, 0.7f), i % 3 == 0 ? ProceduralArt.Moss : ProceduralArt.Stone,
                    new Vector3(0f, side * (8f + i % 3 * 5f), (i % 3 - 1) * 2f), false);
                ProceduralArt.Frustum("Collapsed roof", detail.transform, new Vector3(x, ground + wallHeight, z),
                    4.3f, 0.35f, 2.4f, 4, ProceduralArt.DarkStone, new Vector3(0f, 45f + side * 7f, i % 2 * 8f));
            }
            BuildBellTower(region.transform, GroundPoint(-28f, 72f));
            CreateLoreRelic(region.transform, "seal_retreat", "세 번째 기억 · 울리지 못한 후퇴",
                "나는 문을 열었다. 너무 늦어 살아 있는 이는 없었고, 죽은 자만 내 이름을 불렀다.", true,
                GroundPoint(-27f, 70f) + Vector3.up * 0.2f);
            CreateLoreRelic(region.transform, "record_child", "작은 나무 병정",
                "장군님이 오면 성문이 열릴 거야. 어머니는 그 말을 끝까지 믿었다.", false,
                GroundPoint(29f, 55f));
        }

        private static SealedWorldGate BuildCastleAndGate(Transform world)
        {
            var region = new GameObject("Region_Failed_Keep");
            region.transform.SetParent(world, false);
            AddRegionVolume(region.transform, "실패한 왕국의 폐성", "모든 방어는 끝났으나 장군 한 사람만 끝나지 못했다.", new Vector3(0f, 3f, 113f), new Vector3(70f, 12f, 62f));

            for (var i = 0; i < 9; i++)
            {
                var x = -48f + i * 12f;
                var ground = ProceduralArt.GroundHeight(x, 101f);
                if (Mathf.Abs(x) < 8f) continue;
                ProceduralArt.Block($"Outer curtain wall {i}", region.transform, new Vector3(x, ground + 5.3f, 101f),
                    new Vector3(11.4f, 10.5f + i % 2 * 2f, 4.2f), ProceduralArt.DarkStone,
                    new Vector3(0f, Mathf.Sin(i) * 2f, 0f));
                for (var merlon = 0; merlon < 3; merlon++)
                    ProceduralArt.Block("Wall merlon", region.transform, new Vector3(x - 3.2f + merlon * 3.2f, ground + 11.4f, 101f),
                        new Vector3(1.55f, 2.4f, 4.7f), ProceduralArt.Stone);
            }
            ProceduralArt.RuinArch("Great seal arch", region.transform, GroundPoint(0f, 99f), 18f, 14f, 0f, ProceduralArt.Stone);

            var gateRoot = new GameObject("Gate of three memories");
            gateRoot.transform.SetParent(region.transform, false);
            gateRoot.transform.position = GroundPoint(0f, 100.7f);
            var gateTrigger = gateRoot.AddComponent<SphereCollider>();
            gateTrigger.radius = 4.6f;
            gateTrigger.center = Vector3.up * 2.5f;
            gateTrigger.isTrigger = true;
            var gate = gateRoot.AddComponent<SealedWorldGate>();
            var left = ProceduralArt.Block("Sealed door left", gateRoot.transform, gateRoot.transform.position + new Vector3(-4.1f, 5.1f, 0f),
                new Vector3(8f, 10.2f, 1.05f), ProceduralArt.Rust, new Vector3(0f, 0f, -2f));
            var right = ProceduralArt.Block("Sealed door right", gateRoot.transform, gateRoot.transform.position + new Vector3(4.1f, 5.1f, 0f),
                new Vector3(8f, 10.2f, 1.05f), ProceduralArt.Rust, new Vector3(0f, 0f, 2f));
            gate.RegisterBlocker(left);
            gate.RegisterBlocker(right);
            for (var i = 0; i < 3; i++)
            {
                var seal = ProceduralArt.Frustum($"Empty memory socket {i + 1}", gateRoot.transform,
                    new Vector3(-3f + i * 3f, 5.4f, -0.68f), 0.62f, 0.15f, 1.4f, 6,
                    i == 1 ? ProceduralArt.ErrorMagenta : ProceduralArt.ErrorCyan, new Vector3(90f, 0f, 0f));
                seal.AddComponent<AmbientMotion>().Configure(new Vector3(0f, 22f, 0f), 0.04f, 1.7f, i);
            }

            BuildBossArena(region.transform);
            CreateCheckpoint(GroundPoint(0f, 89f), "장군의 문 앞 불씨");
            return gate;
        }

        private static void BuildBossArena(Transform parent)
        {
            var center = GroundPoint(0f, 132f);
            ProceduralArt.Block("Doyun arena dais", parent, center + Vector3.down * 0.18f,
                new Vector3(55f, 0.35f, 49f), ProceduralArt.DarkStone);
            for (var i = 0; i < 18; i++)
            {
                var angle = i * Mathf.PI * 2f / 18f;
                var radius = i % 2 == 0 ? 30f : 28f;
                var position = center + new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                var height = 5f + i % 5 * 1.4f;
                ProceduralArt.Frustum($"General tomb pillar {i:00}", parent, position, 0.9f, 0.62f, height, 8,
                    i % 4 == 0 ? ProceduralArt.Moss : ProceduralArt.Stone,
                    new Vector3(i % 3 * 3f, -angle * Mathf.Rad2Deg, (i % 4 - 1) * 2f));
            }
            BuildCorruptionCluster(parent, center + new Vector3(19f, 0f, 8f), 22, true);
            ProceduralArt.PointLight("Doyun arena cold light", parent, center + new Vector3(0f, 13f, 4f),
                new Color(0.22f, 0.42f, 0.9f), 2.2f, 38f, true);
        }

        private static void BuildLandmarkKeep(Transform parent)
        {
            var basePoint = new Vector3(0f, 0f, 151f);
            ProceduralArt.Block("Keep distant mass", parent, basePoint + Vector3.up * 19f,
                new Vector3(48f, 38f, 13f), ProceduralArt.DarkStone);
            for (var i = 0; i < 4; i++)
            {
                var x = -27f + i * 18f;
                var height = 42f + (i * 11 % 17);
                ProceduralArt.Frustum($"Keep watchtower {i}", parent, basePoint + new Vector3(x, 0f, 0f),
                    7.5f, 5.2f, height, 8, ProceduralArt.DarkStone,
                    new Vector3(0f, 22.5f, i % 2 == 0 ? -2f : 3f));
                ProceduralArt.Frustum("Pagoda roof shadow", parent, basePoint + new Vector3(x, height - 1f, 0f),
                    11.5f, 0.6f, 5.5f, 4, ProceduralArt.DarkStone, new Vector3(0f, 45f, 0f));
            }
            var beacon = ProceduralArt.Frustum("Distant error beacon", parent, basePoint + new Vector3(0f, 41f, 0f),
                2.2f, 0.12f, 25f, 7, ProceduralArt.ErrorMagenta, new Vector3(0f, 0f, 4f));
            beacon.AddComponent<AmbientMotion>().Configure(new Vector3(0f, 5f, 0f), 0.22f, 0.7f);
            ProceduralArt.PointLight("Beacon wound", parent, basePoint + new Vector3(0f, 60f, 0f),
                new Color(1f, 0.01f, 0.38f), 4f, 55f);
        }

        private static void BuildErrorMoon(Transform parent)
        {
            var moon = ProceduralArt.Primitive(PrimitiveType.Sphere, "Fractured moon", parent,
                new Vector3(-108f, 78f, 105f), Vector3.one * 15f, ProceduralArt.Bone);
            moon.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            for (var i = 0; i < 7; i++)
            {
                var shard = ProceduralArt.Frustum($"Moon fracture {i}", parent,
                    new Vector3(-108f + Mathf.Sin(i * 2.2f) * 14f, 78f + Mathf.Cos(i * 1.8f) * 11f, 104f),
                    1.2f + i % 2, 0.05f, 8f + i % 3 * 3f, 5,
                    i % 2 == 0 ? ProceduralArt.ErrorCyan : ProceduralArt.ErrorMagenta,
                    new Vector3(i * 27f, i * 51f, i * 13f));
                shard.AddComponent<AmbientMotion>().Configure(new Vector3(1f, 3f, 0.5f), 0.35f, 0.35f, i);
            }
        }

        private static void BuildBellTower(Transform parent, Vector3 position)
        {
            ProceduralArt.Block("Bell tower left", parent, position + new Vector3(-3.4f, 5f, 0f),
                new Vector3(2f, 10f, 3f), ProceduralArt.Stone, new Vector3(0f, 0f, -3f));
            ProceduralArt.Block("Bell tower right", parent, position + new Vector3(3.4f, 4.4f, 0f),
                new Vector3(2f, 8.8f, 3f), ProceduralArt.Moss, new Vector3(0f, 0f, 4f));
            ProceduralArt.Block("Bell tower beam", parent, position + Vector3.up * 8.7f,
                new Vector3(8.7f, 0.9f, 2.4f), ProceduralArt.DarkStone, new Vector3(0f, 0f, -2f));
            var bell = ProceduralArt.Frustum("Silent bell", parent, position + new Vector3(0f, 5.3f, 0f),
                1.6f, 0.55f, 2.5f, 10, ProceduralArt.Rust, new Vector3(0f, 0f, 180f));
            bell.AddComponent<AmbientMotion>().Configure(new Vector3(0f, 0f, 0.6f), 0.02f, 0.5f);
        }

        private static void BuildCorruptionCluster(Transform parent, Vector3 center, int count, bool magentaHeavy)
        {
            var root = new GameObject("Reality fracture field");
            root.transform.SetParent(parent, false);
            for (var i = 0; i < count; i++)
            {
                var angle = i * 2.39996f;
                var radius = 1.8f + i * 0.58f;
                var x = center.x + Mathf.Cos(angle) * radius;
                var z = center.z + Mathf.Sin(angle) * radius;
                var y = ProceduralArt.GroundHeight(x, z);
                var height = 0.9f + i % 5 * 0.75f;
                var shard = ProceduralArt.Frustum($"Glitch shard {i:00}", root.transform,
                    new Vector3(x, y, z), 0.24f + i % 3 * 0.08f, 0.025f, height, 4 + i % 3,
                    (i % 3 == 0) == magentaHeavy ? ProceduralArt.ErrorMagenta : ProceduralArt.ErrorCyan,
                    new Vector3(i * 11f % 23f, i * 47f, i % 2 == 0 ? 12f : -9f));
                shard.AddComponent<AmbientMotion>().Configure(new Vector3(0f, i % 2 == 0 ? 11f : -8f, 0f), 0.08f, 1.1f, i);
            }
            ProceduralArt.PointLight("Corruption pulse", root.transform, center + Vector3.up * 2.2f,
                magentaHeavy ? new Color(1f, 0.02f, 0.42f) : new Color(0.02f, 0.65f, 1f), 2.3f, 16f);
        }

        private static List<IResettableEnemy> SpawnWorldEnemies()
        {
            var enemies = new List<IResettableEnemy>();
            var spawns = new[]
            {
                new EnemySpawn("soldier_sword_01", -9f, -76f), new EnemySpawn("soldier_spear_01", 10f, -62f),
                new EnemySpawn("soldier_sword_01", -48f, -38f), new EnemySpawn("soldier_spear_01", -72f, -4f),
                new EnemySpawn("elite_castle_guard", -92f, -17f), new EnemySpawn("soldier_sword_01", -58f, 11f),
                new EnemySpawn("soldier_spear_01", 42f, -11f), new EnemySpawn("soldier_sword_01", 62f, 23f),
                new EnemySpawn("soldier_spear_01", 88f, 16f), new EnemySpawn("elite_castle_guard", 73f, -2f),
                new EnemySpawn("soldier_sword_01", -28f, 48f), new EnemySpawn("soldier_spear_01", 31f, 52f),
                new EnemySpawn("soldier_sword_01", 18f, 74f), new EnemySpawn("elite_castle_guard", -8f, 82f),
                new EnemySpawn("soldier_spear_01", -15f, 111f), new EnemySpawn("soldier_sword_01", 17f, 116f)
            };
            foreach (var spawn in spawns)
                enemies.Add(CreateEnemy(spawn.Id, GroundPoint(spawn.X, spawn.Z)));
            return enemies;
        }

        private static PlayerMotor CreatePlayer(Vector3 position)
        {
            var root = new GameObject("Player_Unnamed");
            root.tag = "Player";
            root.transform.position = position;
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.92f;
            controller.radius = 0.36f;
            controller.center = new Vector3(0f, 0.96f, 0f);
            root.AddComponent<ProceduralGroundClamp>().Configure(0.08f);
            root.AddComponent<ActorStateMachine>();
            var vitals = root.AddComponent<CombatVitals>();
            vitals.Configure(Faction.Player, 400f, 100f, 8f, 45f, 0);
            root.AddComponent<PlayerProgression>();
            root.AddComponent<LockOnSystem>();
            var motor = root.AddComponent<PlayerMotor>();
            root.AddComponent<PlayerCombat>();
            root.AddComponent<PlayerInteractor>();
            BuildWandererVisual(root.transform);
            return motor;
        }

        private static void BuildWandererVisual(Transform root)
        {
            ProceduralHumanoid.Build(root, HumanoidLook.Wanderer);
        }

        private static EnemyBrain CreateEnemy(string id, Vector3 position)
        {
            var data = GameDataRepository.Enemy(id);
            var root = new GameObject(data?.name ?? id);
            root.transform.position = position;
            var elite = id == "elite_castle_guard";
            var controller = root.AddComponent<CharacterController>();
            controller.height = elite ? 2.2f : 1.8f;
            controller.radius = elite ? 0.5f : 0.38f;
            controller.center = Vector3.up * controller.height * 0.5f;
            root.AddComponent<ProceduralGroundClamp>().Configure(0.08f);
            root.AddComponent<ActorStateMachine>();
            root.AddComponent<CombatVitals>();
            var brain = root.AddComponent<EnemyBrain>();
            brain.Initialize(id, position);
            BuildSoldierVisual(root.transform, id, elite);
            return brain;
        }

        private static void BuildSoldierVisual(Transform root, string id, bool elite)
        {
            ProceduralHumanoid.Build(root, elite ? HumanoidLook.CastleGuard : HumanoidLook.HollowSoldier,
                id.Contains("spear"));
            var error = ProceduralArt.Frustum("Error growing through armor", root, new Vector3(-0.22f, 1.28f, -0.28f),
                0.13f, 0.01f, elite ? 0.8f : 0.48f, 5, elite ? ProceduralArt.ErrorMagenta : ProceduralArt.ErrorCyan,
                new Vector3(-48f, 0f, -22f));
            error.AddComponent<AmbientMotion>().Configure(new Vector3(0f, 8f, 0f), 0.025f, 2f);
        }

        private static BossDoyunBrain CreateBoss(Vector3 position)
        {
            var root = new GameObject("무너진 장군, 도윤");
            root.transform.position = position;
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2.75f;
            controller.radius = 0.68f;
            controller.center = new Vector3(0f, 1.37f, 0f);
            root.AddComponent<ProceduralGroundClamp>().Configure(0.08f);
            root.AddComponent<ActorStateMachine>();
            root.AddComponent<CombatVitals>();
            var brain = root.AddComponent<BossDoyunBrain>();
            BuildDoyunVisual(root.transform);
            brain.Initialize(position);
            return brain;
        }

        private static void BuildDoyunVisual(Transform root)
        {
            ProceduralHumanoid.Build(root, HumanoidLook.Doyun, true);
            ProceduralArt.PointLight("Doyun error leaking through armor", root, new Vector3(0f, 2.1f, -0.35f),
                new Color(0.04f, 0.42f, 1f), 3.1f, 11f, true);
        }

        private static Checkpoint CreateCheckpoint(Vector3 position, string name)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            var trigger = root.AddComponent<SphereCollider>();
            trigger.radius = 2.35f;
            trigger.center = Vector3.up * 0.7f;
            trigger.isTrigger = true;
            var checkpoint = root.AddComponent<Checkpoint>();
            for (var i = 0; i < 9; i++)
            {
                var angle = i * Mathf.PI * 2f / 9f;
                ProceduralArt.Rock($"Ember altar stone {i}", root.transform,
                    position + new Vector3(Mathf.Sin(angle) * 0.7f, 0.2f, Mathf.Cos(angle) * 0.7f),
                    new Vector3(0.45f, 0.32f, 0.55f), ProceduralArt.DarkStone, 510 + i);
            }
            ProceduralArt.Block("Sword in the ash", root.transform, position + new Vector3(0f, 0.92f, 0f),
                new Vector3(0.12f, 1.85f, 0.21f), ProceduralArt.Rust, new Vector3(0f, 0f, -13f), false);
            ProceduralArt.Block("Checkpoint sword guard", root.transform, position + new Vector3(-0.2f, 1.44f, 0f),
                new Vector3(0.72f, 0.1f, 0.12f), ProceduralArt.DarkStone, new Vector3(0f, 0f, -13f), false);
            for (var i = 0; i < 5; i++)
            {
                var ember = ProceduralArt.Frustum($"Living ember {i}", root.transform,
                    new Vector3(Mathf.Sin(i * 2.3f) * 0.32f, 0.45f, Mathf.Cos(i * 2.3f) * 0.32f),
                    0.16f, 0.01f, 0.75f + i % 2 * 0.3f, 5, ProceduralArt.Ember,
                    new Vector3(i * 9f, i * 43f, i % 2 == 0 ? 10f : -8f));
                ember.AddComponent<AmbientMotion>().Configure(new Vector3(0f, 16f, 0f), 0.11f, 2.2f, i);
            }
            ProceduralArt.PointLight("Warm checkpoint light", root.transform, Vector3.up * 1.15f,
                new Color(1f, 0.23f, 0.035f), 4.2f, 15f, true);
            return checkpoint;
        }

        private static StoryRelic CreateLoreRelic(Transform parent, string id, string title, string lore, bool seal, Vector3 position)
        {
            var root = new GameObject(title);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            var trigger = root.AddComponent<SphereCollider>();
            trigger.radius = 2f;
            trigger.center = Vector3.up * 0.8f;
            trigger.isTrigger = true;
            ProceduralArt.Rock("Memory plinth", root.transform, position + Vector3.up * 0.45f,
                new Vector3(1.05f, 0.65f, 0.9f), ProceduralArt.DarkStone, id.GetHashCode());
            var memory = ProceduralArt.Frustum(seal ? "Sealed memory" : "Echoing record", root.transform,
                new Vector3(0f, 0.78f, 0f), seal ? 0.38f : 0.26f, 0.02f, seal ? 1.55f : 1.05f,
                seal ? 6 : 5, seal ? ProceduralArt.ErrorMagenta : ProceduralArt.ErrorCyan,
                new Vector3(0f, seal ? 30f : 0f, seal ? 0f : 18f));
            memory.AddComponent<AmbientMotion>().Configure(new Vector3(0f, seal ? 24f : -17f, 0f), 0.16f, 1.25f);
            ProceduralArt.PointLight("Memory glow", root.transform, new Vector3(0f, 1.35f, 0f),
                seal ? new Color(1f, 0.02f, 0.42f) : new Color(0f, 0.58f, 1f), 2f, 8f);
            var relic = root.AddComponent<StoryRelic>();
            relic.Configure(id, title, lore, seal);
            return relic;
        }

        private static void CreateGuidingEcho(Vector3 position)
        {
            var root = new GameObject("불씨의 안내자");
            root.transform.position = position;
            var trigger = root.AddComponent<SphereCollider>();
            trigger.radius = 2.2f;
            trigger.center = Vector3.up;
            trigger.isTrigger = true;
            root.AddComponent<GuidingEcho>();
            ProceduralArt.Frustum("Guide tattered robe", root.transform, new Vector3(0f, 0.1f, 0f),
                0.5f, 0.25f, 1.45f, 9, ProceduralArt.Cloth);
            ProceduralArt.Primitive(PrimitiveType.Sphere, "Guide hidden face", root.transform, new Vector3(0f, 1.66f, 0f),
                Vector3.one * 0.4f, ProceduralArt.DarkStone);
            var halo = ProceduralArt.Frustum("Guide error halo", root.transform, new Vector3(0f, 1.78f, 0.05f),
                0.52f, 0.41f, 0.08f, 16, ProceduralArt.ErrorCyan, new Vector3(90f, 0f, 0f));
            halo.AddComponent<AmbientMotion>().Configure(new Vector3(0f, 26f, 0f), 0.07f, 1.1f);
            ProceduralArt.PointLight("Guide pale light", root.transform, Vector3.up * 1.35f,
                new Color(0.18f, 0.72f, 1f), 1.65f, 7f);
        }

        public static DroppedSouls CreateDroppedSouls(Vector3 position, int amount)
        {
            var root = new GameObject("잃어버린 혼");
            root.transform.position = position + Vector3.up * 0.35f;
            var trigger = root.AddComponent<SphereCollider>();
            trigger.radius = 1.25f;
            trigger.isTrigger = true;
            var souls = root.AddComponent<DroppedSouls>();
            souls.Initialize(amount);
            for (var i = 0; i < 5; i++)
            {
                var shard = ProceduralArt.Frustum($"Lost soul splinter {i}", root.transform,
                    new Vector3(Mathf.Sin(i * 2.4f) * 0.38f, i * 0.17f, Mathf.Cos(i * 2.4f) * 0.38f),
                    0.12f, 0.01f, 0.72f, 5, ProceduralArt.ErrorCyan,
                    new Vector3(i * 22f, i * 47f, i * 9f));
                shard.AddComponent<AmbientMotion>().Configure(new Vector3(8f, 24f, 4f), 0.14f, 1.7f, i);
            }
            ProceduralArt.PointLight("Lost soul glow", root.transform, Vector3.up * 0.4f,
                new Color(0.02f, 0.45f, 1f), 2.3f, 8f);
            return souls;
        }

        private static void AddRegionVolume(Transform parent, string region, string epigraph, Vector3 position, Vector3 size)
        {
            var root = new GameObject($"Region trigger - {region}");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            var box = root.AddComponent<BoxCollider>();
            box.size = size;
            var volume = root.AddComponent<RegionVolume>();
            volume.Configure(region, epigraph);
        }

        private static GameObject CreateDetailChunk(Transform parent, string name, Vector3 center, float distance)
        {
            var root = new GameObject($"Streamed chunk - {name}");
            root.transform.SetParent(parent, false);
            root.transform.position = center;
            var content = new GameObject(name);
            content.transform.SetParent(root.transform, false);
            content.transform.position = Vector3.zero;
            root.AddComponent<WorldDetailChunk>().Configure(content, distance);
            return content;
        }

        private static void CreateCamera(Transform player)
        {
            if (Application.isBatchMode) return;
            var existing = Camera.main;
            var cameraObject = existing != null ? existing.gameObject : new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = existing != null ? existing : cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.12f;
            camera.farClipPlane = 520f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.allowHDR = true;
            cameraObject.transform.position = player.position + new Vector3(0f, 3f, -5f);
            var controller = cameraObject.GetComponent<SoulsCamera>() ?? cameraObject.AddComponent<SoulsCamera>();
            controller.SetTarget(player);
        }

#if UNITY_EDITOR
        public static void VerifyPlayerRuntimeBatch()
        {
            ProceduralArt.Initialize();
            var testWorld = new GameObject("Runtime ground verification world");
            ProceduralArt.CreateTerrain("Runtime terrain collision probe", testWorld.transform, WorldSize, 48);
            Physics.SyncTransforms();

            const float x = 0f;
            const float z = -109f;
            var expectedFloor = ProceduralArt.GroundHeight(x, z) + 0.08f;
            var player = CreatePlayer(new Vector3(x, expectedFloor + 4f, z));
            var controller = player.GetComponent<CharacterController>();
            for (var i = 0; i < 160; i++) controller.Move(Vector3.down * 0.08f);
            if (player.transform.position.y < expectedFloor - 0.03f)
                throw new System.InvalidOperationException(
                    $"Procedural terrain collision failed: y={player.transform.position.y:0.000}, floor={expectedFloor:0.000}");

            player.Warp(new Vector3(x, -25f, z));
            if (player.transform.position.y < expectedFloor - 0.001f)
                throw new System.InvalidOperationException(
                    $"Player floor recovery failed: y={player.transform.position.y:0.000}, floor={expectedFloor:0.000}");

            var renderers = player.GetComponentsInChildren<Renderer>();
            var animator = player.GetComponentInChildren<ProceduralHumanoidAnimator>();
            var hasForgedBlade = System.Array.Exists(renderers,
                renderer => renderer != null && renderer.gameObject.name == "Forged oath sword blade");
            var hasHumanHead = System.Array.Exists(renderers,
                renderer => renderer != null && renderer.gameObject.name == "Visible weathered face");
            var hasShield = System.Array.Exists(renderers,
                renderer => renderer != null && renderer.gameObject.name == "Shield weathered face");
            if (animator == null || renderers.Length < 55 || !hasForgedBlade || !hasHumanHead || !hasShield)
                throw new System.InvalidOperationException(
                    $"Humanoid assembly incomplete: animator={animator != null}, renderers={renderers.Length}, " +
                    $"blade={hasForgedBlade}, face={hasHumanHead}, shield={hasShield}");

            Debug.Log($"PLAYER_RUNTIME_OK floor={expectedFloor:0.000} y={player.transform.position.y:0.000} " +
                      $"humanRenderers={renderers.Length}");
            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(testWorld);
        }

        public static void VerifyCombatReactionBatch()
        {
            ProceduralArt.Initialize();
            var player = CreatePlayer(Vector3.zero);
            var enemy = CreateEnemy("soldier_sword_01", Vector3.forward * 2f);
            var boss = CreateBoss(Vector3.forward * 5f);
            Physics.SyncTransforms();

            var playerVitals = player.GetComponent<CombatVitals>();
            var playerState = player.GetComponent<ActorStateMachine>();
            playerState.Force(ActorState.Attack, 2f);
            var playerResult = playerVitals.ReceiveHit(new DamageInfo(enemy.gameObject, 30f, 8f, 1f, true,
                player.transform.position + Vector3.up, -Vector3.forward, 1001));
            var playerReaction = playerState.LockedUntil - Time.time;
            if (playerResult != HitOutcome.Damaged || playerState.Current != ActorState.HitReact ||
                playerReaction <= 0f || playerReaction > 0.36f)
                throw new System.InvalidOperationException(
                    $"Player hit reaction failed: result={playerResult}, state={playerState.Current}, lock={playerReaction:0.000}");

            var enemyVitals = enemy.GetComponent<CombatVitals>();
            var enemyState = enemy.GetComponent<ActorStateMachine>();
            enemyState.Force(ActorState.Attack, 2f);
            var enemyResult = enemyVitals.ReceiveHit(new DamageInfo(player.gameObject, 42f, 8f, 1f, false,
                enemy.transform.position + Vector3.up, Vector3.forward, 1002));
            var enemyReaction = enemyState.LockedUntil - Time.time;
            if (enemyResult != HitOutcome.Damaged || enemyState.Current != ActorState.HitReact ||
                enemyReaction <= 0f || enemyReaction > 0.36f)
                throw new System.InvalidOperationException(
                    $"Enemy hit reaction failed: result={enemyResult}, state={enemyState.Current}, lock={enemyReaction:0.000}");

            var bossVitals = boss.GetComponent<CombatVitals>();
            var bossState = boss.GetComponent<ActorStateMachine>();
            bossState.Force(ActorState.Attack, 2f);
            var bossResult = bossVitals.ReceiveHit(new DamageInfo(player.gameObject, 58f, 8f, 1f, false,
                boss.transform.position + Vector3.up, Vector3.forward, 1003));
            var bossReaction = bossState.LockedUntil - Time.time;
            if (bossResult != HitOutcome.Damaged || bossState.Current != ActorState.HitReact ||
                bossReaction <= 0f || bossReaction > 0.22f)
                throw new System.InvalidOperationException(
                    $"Boss hit reaction failed: result={bossResult}, state={bossState.Current}, lock={bossReaction:0.000}");

            if (player.GetComponentInChildren<ProceduralHumanoidAnimator>() == null ||
                enemy.GetComponentInChildren<ProceduralHumanoidAnimator>() == null ||
                boss.GetComponentInChildren<ProceduralHumanoidAnimator>() == null)
                throw new System.InvalidOperationException("Combat reaction animator missing from one or more actors.");

            Debug.Log($"COMBAT_REACTION_OK player={playerReaction:0.000} enemy={enemyReaction:0.000} " +
                      $"boss={bossReaction:0.000}");
            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(enemy.gameObject);
            Object.DestroyImmediate(boss.gameObject);
        }
#endif

        private static Vector3 GroundPoint(float x, float z) => new Vector3(x, ProceduralArt.GroundHeight(x, z) + 0.08f, z);

        private readonly struct EnemySpawn
        {
            public readonly string Id;
            public readonly float X;
            public readonly float Z;
            public EnemySpawn(string id, float x, float z) { Id = id; X = x; Z = z; }
        }
    }
}
