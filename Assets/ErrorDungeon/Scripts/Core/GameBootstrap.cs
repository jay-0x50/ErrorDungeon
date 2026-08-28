using System.Collections.Generic;
using UnityEngine;

namespace ErrorDungeon
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        private static Material stone;
        private static Material darkStone;
        private static Material mossStone;
        private static Material rustMetal;
        private static Material errorBlue;
        private static Material errorMagenta;
        private static Material ember;
        private static Material playerMaterial;
        private static Material enemyMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<GameBootstrap>() != null) return;
            new GameObject("ErrorDungeon_Bootstrap").AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            BuildMaterials();
            BuildLighting();
            BuildWorld();
        }

        private static void BuildMaterials()
        {
            stone = Material(new Color(0.18f, 0.19f, 0.2f), 0.1f, 0.5f);
            darkStone = Material(new Color(0.07f, 0.075f, 0.085f), 0f, 0.7f);
            mossStone = Material(new Color(0.12f, 0.17f, 0.13f), 0.02f, 0.35f);
            rustMetal = Material(new Color(0.24f, 0.11f, 0.055f), 0.5f, 0.22f);
            errorBlue = Material(new Color(0.03f, 0.32f, 0.42f), 0.25f, 0.2f, new Color(0.0f, 0.85f, 1.4f));
            errorMagenta = Material(new Color(0.42f, 0.02f, 0.24f), 0.2f, 0.25f, new Color(1.35f, 0.0f, 0.68f));
            ember = Material(new Color(0.8f, 0.25f, 0.04f), 0.1f, 0.2f, new Color(1f, 0.18f, 0.01f));
            playerMaterial = Material(new Color(0.32f, 0.34f, 0.38f), 0.55f, 0.3f);
            enemyMaterial = Material(new Color(0.16f, 0.12f, 0.13f), 0.35f, 0.5f);
        }

        private static Material Material(Color color, float metallic, float smoothness, Color? emission = null)
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (emission.HasValue && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            return material;
        }

        private static void BuildLighting()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.035f, 0.045f, 0.065f);
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.ambientLight = new Color(0.15f, 0.17f, 0.22f);
            var sunObject = new GameObject("Moonlight");
            var light = sunObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.58f, 0.66f, 0.82f);
            light.intensity = 0.95f;
            light.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
        }

        private static void BuildWorld()
        {
            var world = new GameObject("World_Pyesong_Outskirts");
            CreateBlock("OpenField", new Vector3(0f, -1f, 0f), new Vector3(115f, 2f, 115f), stone, world.transform);
            CreateBlock("CastleCauseway", new Vector3(0f, -0.35f, 39f), new Vector3(18f, 0.8f, 54f), darkStone, world.transform);
            CreateBlock("BossArena", new Vector3(0f, -0.35f, 77f), new Vector3(31f, 0.8f, 29f), darkStone, world.transform);

            BuildRuinedRoad(world.transform);
            BuildCorruptionZone(world.transform);

            for (var i = 0; i < 13; i++)
            {
                var angle = i * Mathf.PI * 2f / 13f;
                var radius = 43f + (i % 3) * 4f;
                CreateBlock($"Cliff_{i}", new Vector3(Mathf.Sin(angle) * radius, 2.5f, Mathf.Cos(angle) * radius),
                    new Vector3(13f, 7f + i % 4, 9f), darkStone, world.transform, new Vector3(0f, -angle * Mathf.Rad2Deg, 8f - i % 3 * 5f));
            }
            for (var i = 0; i < 7; i++)
            {
                CreateBlock($"RuinL_{i}", new Vector3(-9f, 2.4f, 25f + i * 9f), new Vector3(2.2f, 7f, 7f), stone, world.transform);
                CreateBlock($"RuinR_{i}", new Vector3(9f, 2.4f, 25f + i * 9f), new Vector3(2.2f, 7f, 7f), stone, world.transform);
            }
            CreateRuinArch("BossGate", new Vector3(0f, 0f, 63f), 20f, 11f, world.transform);
            BuildLandmarkKeep(world.transform);

            for (var i = 0; i < 22; i++)
            {
                var x = Mathf.Sin(i * 3.71f) * 34f;
                var z = -29f + (i % 7) * 8f;
                CreateBlock($"BrokenPillar_{i}", new Vector3(x, 1f + i % 3, z),
                    new Vector3(1.4f, 3f + i % 3 * 1.3f, 1.4f), stone, world.transform,
                    new Vector3(i % 2 * 8f, i * 31f, i % 3 * 6f));
            }

            Physics.SyncTransforms();

            var session = new GameObject("GameSession").AddComponent<GameSession>();
            var checkpoint = CreateCheckpoint(new Vector3(0f, 0.25f, -32f));
            var player = CreatePlayer(checkpoint.RespawnPosition);
            var normalEnemies = new List<IResettableEnemy>();
            normalEnemies.Add(CreateEnemy("soldier_sword_01", new Vector3(-8f, 1f, -15f)));
            normalEnemies.Add(CreateEnemy("soldier_spear_01", new Vector3(9f, 1f, -5f)));
            normalEnemies.Add(CreateEnemy("soldier_sword_01", new Vector3(-13f, 1f, 8f)));
            normalEnemies.Add(CreateEnemy("soldier_spear_01", new Vector3(6f, 1f, 23f)));
            normalEnemies.Add(CreateEnemy("soldier_sword_01", new Vector3(-5f, 1f, 36f)));
            normalEnemies.Add(CreateEnemy("elite_castle_guard", new Vector3(0f, 1f, 53f)));
            var boss = CreateBoss(new Vector3(0f, 0.05f, 78f));

            Physics.SyncTransforms();

            var online = new GameObject("LinuxServerBridge").AddComponent<LeaderboardClient>();
            var hud = new GameObject("SoulsHUD").AddComponent<SoulsHUD>();
            hud.Bind(player.gameObject);
            CreateCamera(player.transform);
            session.RegisterWorld(player, checkpoint, boss, normalEnemies);

            if (Application.isBatchMode) online.StartServerHeartbeat();
        }

        private static void BuildRuinedRoad(Transform parent)
        {
            for (var i = 0; i < 12; i++)
            {
                var z = -28f + i * 7.2f;
                var x = Mathf.Sin(i * 1.9f) * 1.4f;
                var slab = CreateBlock($"RoadSlab_{i:00}", new Vector3(x, 0.08f, z),
                    new Vector3(7.8f, 0.16f, 5.5f), i % 4 == 0 ? mossStone : darkStone, parent,
                    new Vector3(0f, Mathf.Sin(i) * 3f, 0f));
                slab.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            CreateRuinArch("FieldArch", new Vector3(-18f, 0f, -4f), 8f, 7.5f, parent, 18f);
            CreateRuinArch("CollapsedArch", new Vector3(20f, 0f, 18f), 7f, 6.5f, parent, -24f);

            for (var i = 0; i < 9; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                CreateBlock($"RustBannerPole_{i}", new Vector3(side * (12f + i % 3 * 4f), 2.4f, -24f + i * 8f),
                    new Vector3(0.18f, 4.8f, 0.18f), rustMetal, parent, new Vector3(0f, 0f, side * 4f));
            }
        }

        private static void BuildCorruptionZone(Transform parent)
        {
            var zone = new GameObject("ErrorCorruption_CyanMagenta");
            zone.transform.SetParent(parent, false);
            for (var i = 0; i < 20; i++)
            {
                var angle = i * 2.39996f;
                var radius = 3f + i * 0.62f;
                var x = 25f + Mathf.Cos(angle) * radius;
                var z = 7f + Mathf.Sin(angle) * radius;
                var height = 0.9f + (i % 5) * 0.65f;
                CreateVisualPrimitive(i % 3 == 0 ? PrimitiveType.Cube : PrimitiveType.Cylinder,
                    $"GlitchShard_{i:00}", zone.transform, new Vector3(x, height * 0.48f, z),
                    new Vector3(0.18f + i % 3 * 0.08f, height, 0.18f),
                    i % 2 == 0 ? errorMagenta : errorBlue,
                    new Vector3(i * 13f % 28f, i * 47f, i % 2 == 0 ? 16f : -12f));
            }
            CreatePointLight("CorruptionPulse", zone.transform, new Vector3(25f, 2.2f, 7f),
                new Color(0.25f, 0.7f, 1f), 2.1f, 13f);
        }

        private static void BuildLandmarkKeep(Transform parent)
        {
            CreateBlock("KeepSilhouette", new Vector3(0f, 13f, 100f), new Vector3(26f, 28f, 8f), darkStone, parent);
            CreateBlock("KeepTowerLeft", new Vector3(-15f, 19f, 99f), new Vector3(8f, 40f, 10f), darkStone, parent);
            CreateBlock("KeepTowerRight", new Vector3(15f, 17f, 99f), new Vector3(8f, 36f, 10f), darkStone, parent);
            CreateBlock("KeepSpire", new Vector3(0f, 32f, 100f), new Vector3(5f, 18f, 5f), darkStone, parent,
                new Vector3(0f, 0f, 7f));
            CreateVisualPrimitive(PrimitiveType.Sphere, "DistantErrorBeacon", parent,
                new Vector3(0f, 42f, 97f), new Vector3(2.4f, 3.5f, 2.4f), errorMagenta);
            CreatePointLight("DistantBeaconLight", parent, new Vector3(0f, 42f, 97f),
                new Color(1f, 0.02f, 0.5f), 3f, 24f);
        }

        private static void CreateRuinArch(string name, Vector3 position, float width, float height, Transform parent,
            float yRotation = 0f)
        {
            var arch = new GameObject(name);
            arch.transform.SetParent(parent, false);
            arch.transform.position = position;
            arch.transform.eulerAngles = new Vector3(0f, yRotation, 0f);
            CreateBlock("LeftPier", position + arch.transform.right * (-width * 0.5f),
                new Vector3(1.6f, height, 2.1f), stone, arch.transform);
            CreateBlock("RightPier", position + arch.transform.right * (width * 0.5f),
                new Vector3(1.6f, height, 2.1f), mossStone, arch.transform);
            CreateBlock("Lintel", position + Vector3.up * (height - 0.7f),
                new Vector3(width + 1.6f, 1.5f, 2.2f), stone, arch.transform,
                new Vector3(0f, yRotation, yRotation == 0f ? 0f : 2f));
        }

        private static PlayerMotor CreatePlayer(Vector3 position)
        {
            var root = new GameObject("Player_Unnamed");
            root.tag = "Player";
            root.transform.position = position;
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.38f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            root.AddComponent<ActorStateMachine>();
            var vitals = root.AddComponent<CombatVitals>();
            vitals.Configure(Faction.Player, 400f, 100f, 8f, 45f, 0);
            root.AddComponent<PlayerProgression>();
            root.AddComponent<LockOnSystem>();
            var motor = root.AddComponent<PlayerMotor>();
            root.AddComponent<PlayerCombat>();
            root.AddComponent<PlayerInteractor>();

            var body = CreateVisualPrimitive(PrimitiveType.Capsule, "NamelessBody", root.transform,
                new Vector3(0f, 0.9f, 0f), new Vector3(0.78f, 0.9f, 0.78f), playerMaterial);
            CreateVisualPrimitive(PrimitiveType.Cube, "Greatsword", body.transform,
                new Vector3(0.58f, 0.15f, 0.22f), new Vector3(0.12f, 1.5f, 0.22f), darkStone, new Vector3(18f, 0f, -16f));
            return motor;
        }

        private static EnemyBrain CreateEnemy(string id, Vector3 position)
        {
            var data = GameDataRepository.Enemy(id);
            var root = new GameObject(data?.name ?? id);
            root.transform.position = position;
            var controller = root.AddComponent<CharacterController>();
            controller.height = id == "elite_castle_guard" ? 2.2f : 1.8f;
            controller.radius = id == "elite_castle_guard" ? 0.5f : 0.38f;
            controller.center = Vector3.up * controller.height * 0.5f;
            root.AddComponent<ActorStateMachine>();
            root.AddComponent<CombatVitals>();
            var brain = root.AddComponent<EnemyBrain>();
            brain.Initialize(id, position);
            var scale = id == "elite_castle_guard" ? new Vector3(1.15f, 1.1f, 1.15f) : new Vector3(0.8f, 0.9f, 0.8f);
            var body = CreateVisualPrimitive(PrimitiveType.Capsule, "CorruptedBody", root.transform, Vector3.up, scale, enemyMaterial);
            CreateVisualPrimitive(id.Contains("spear") ? PrimitiveType.Cylinder : PrimitiveType.Cube, "Weapon", body.transform,
                new Vector3(0.55f, 0.1f, 0.25f), id.Contains("spear") ? new Vector3(0.07f, 1.2f, 0.07f) : new Vector3(0.12f, 1f, 0.18f),
                stone, new Vector3(22f, 0f, -12f));
            return brain;
        }

        private static BossDoyunBrain CreateBoss(Vector3 position)
        {
            var root = new GameObject("무너진 장군, 도윤");
            root.transform.position = position;
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2.7f;
            controller.radius = 0.62f;
            controller.center = new Vector3(0f, 1.35f, 0f);
            root.AddComponent<ActorStateMachine>();
            root.AddComponent<CombatVitals>();
            var brain = root.AddComponent<BossDoyunBrain>();
            brain.Initialize(position);
            var body = CreateVisualPrimitive(PrimitiveType.Capsule, "DoyunArmor", root.transform,
                new Vector3(0f, 1.35f, 0f), new Vector3(1.25f, 1.35f, 1.25f), darkStone);
            CreateVisualPrimitive(PrimitiveType.Cylinder, "DoyunSpear", body.transform,
                new Vector3(0.9f, 0.1f, 0.35f), new Vector3(0.09f, 1.8f, 0.09f), errorBlue, new Vector3(18f, 0f, -15f));
            CreatePointLight("DoyunError", root.transform, new Vector3(0f, 2.5f, 0f), new Color(0.08f, 0.28f, 1f), 2.5f, 8f);
            return brain;
        }

        private static Checkpoint CreateCheckpoint(Vector3 position)
        {
            var root = new GameObject("폐성 화톳불");
            root.transform.position = position;
            var trigger = root.AddComponent<SphereCollider>();
            trigger.radius = 2.2f;
            trigger.isTrigger = true;
            var checkpoint = root.AddComponent<Checkpoint>();
            CreateVisualPrimitive(PrimitiveType.Cylinder, "EmberStand", root.transform, new Vector3(0f, 0.35f, 0f),
                new Vector3(0.7f, 0.35f, 0.7f), stone);
            CreateVisualPrimitive(PrimitiveType.Sphere, "Ember", root.transform, new Vector3(0f, 0.95f, 0f),
                new Vector3(0.5f, 0.8f, 0.5f), ember);
            CreatePointLight("EmberLight", root.transform, new Vector3(0f, 1.2f, 0f), new Color(1f, 0.22f, 0.04f), 3.5f, 11f);
            return checkpoint;
        }

        public static DroppedSouls CreateDroppedSouls(Vector3 position, int amount)
        {
            var root = new GameObject("잃어버린 혼");
            root.transform.position = position + Vector3.up * 0.65f;
            var trigger = root.AddComponent<SphereCollider>();
            trigger.radius = 1.25f;
            trigger.isTrigger = true;
            var souls = root.AddComponent<DroppedSouls>();
            souls.Initialize(amount);
            CreateVisualPrimitive(PrimitiveType.Sphere, "SoulLight", root.transform, Vector3.zero, Vector3.one * 0.55f, errorBlue);
            CreatePointLight("SoulGlow", root.transform, Vector3.zero, new Color(0.05f, 0.35f, 1f), 2f, 7f);
            return souls;
        }

        private static void CreateCamera(Transform player)
        {
            if (Application.isBatchMode) return;
            var existing = Camera.main;
            var cameraObject = existing != null ? existing.gameObject : new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = existing != null ? existing : cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.15f;
            camera.farClipPlane = 220f;
            cameraObject.transform.position = player.position + new Vector3(0f, 3f, -5f);
            var controller = cameraObject.GetComponent<SoulsCamera>() ?? cameraObject.AddComponent<SoulsCamera>();
            controller.SetTarget(player);
        }

        private static GameObject CreateBlock(string name, Vector3 position, Vector3 scale, Material material,
            Transform parent, Vector3 rotation = default)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            block.transform.eulerAngles = rotation;
            block.GetComponent<Renderer>().material = material;
            var collider = block.GetComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = Vector3.one;
            collider.isTrigger = false;
            collider.enabled = true;
            block.isStatic = true;
            return block;
        }

        private static GameObject CreateVisualPrimitive(PrimitiveType type, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material, Vector3 localRotation = default)
        {
            var visual = GameObject.CreatePrimitive(type);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;
            visual.transform.localEulerAngles = localRotation;
            visual.GetComponent<Renderer>().material = material;
            var collider = visual.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            return visual;
        }

        private static void CreatePointLight(string name, Transform parent, Vector3 localPosition, Color color, float intensity, float range)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = localPosition;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
        }
    }
}
