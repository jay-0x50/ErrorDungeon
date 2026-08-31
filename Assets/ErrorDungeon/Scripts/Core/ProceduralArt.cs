using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ErrorDungeon
{
    internal static class ProceduralArt
    {
        public static Material Stone { get; private set; }
        public static Material DarkStone { get; private set; }
        public static Material Moss { get; private set; }
        public static Material Rust { get; private set; }
        public static Material Cloth { get; private set; }
        public static Material Bone { get; private set; }
        public static Material PlayerMetal { get; private set; }
        public static Material EnemyMetal { get; private set; }
        public static Material Skin { get; private set; }
        public static Material Leather { get; private set; }
        public static Material Linen { get; private set; }
        public static Material Hair { get; private set; }
        public static Material AmberLeather { get; private set; }
        public static Material PolishedSteel { get; private set; }
        public static Material ErrorCyan { get; private set; }
        public static Material ErrorMagenta { get; private set; }
        public static Material Ember { get; private set; }
        public static Material Water { get; private set; }
        public static Material Mist { get; private set; }

        public static void Initialize()
        {
            Stone = MakeLit("Ash stone", new Color(0.19f, 0.20f, 0.205f), 0.02f, 0.2f,
                MakeNoise("Stone grain", new Color(0.13f, 0.14f, 0.145f), new Color(0.31f, 0.30f, 0.27f), 3.1f, 17.4f));
            DarkStone = MakeLit("Wet black stone", new Color(0.065f, 0.075f, 0.085f), 0.04f, 0.32f,
                MakeNoise("Dark stone grain", new Color(0.035f, 0.04f, 0.05f), new Color(0.16f, 0.17f, 0.18f), 4.5f, 44.8f));
            Moss = MakeLit("Dead moss", new Color(0.12f, 0.16f, 0.105f), 0f, 0.12f,
                MakeNoise("Moss grain", new Color(0.06f, 0.08f, 0.055f), new Color(0.23f, 0.28f, 0.14f), 7f, 91.3f));
            Rust = MakeLit("Rusted iron", new Color(0.25f, 0.105f, 0.045f), 0.58f, 0.22f,
                MakeNoise("Rust grain", new Color(0.08f, 0.035f, 0.02f), new Color(0.42f, 0.19f, 0.055f), 8f, 121.6f));
            Cloth = MakeLit("Weathered cloth", new Color(0.105f, 0.11f, 0.125f), 0f, 0.08f);
            Bone = MakeLit("Old bone", new Color(0.48f, 0.45f, 0.35f), 0f, 0.12f);
            PlayerMetal = MakeLit("Wanderer steel", new Color(0.31f, 0.335f, 0.37f), 0.68f, 0.28f);
            EnemyMetal = MakeLit("Corrupted iron", new Color(0.13f, 0.09f, 0.095f), 0.54f, 0.24f);
            Skin = MakeLit("Weathered skin", new Color(0.43f, 0.30f, 0.245f), 0f, 0.26f,
                MakeNoise("Skin grain", new Color(0.31f, 0.19f, 0.15f), new Color(0.56f, 0.39f, 0.30f), 2.2f, 213.7f));
            Leather = MakeLit("Oiled leather", new Color(0.105f, 0.055f, 0.035f), 0f, 0.34f,
                MakeNoise("Leather grain", new Color(0.045f, 0.022f, 0.016f), new Color(0.19f, 0.085f, 0.045f), 5.4f, 81.2f));
            Linen = MakeLit("Ash linen", new Color(0.28f, 0.265f, 0.235f), 0f, 0.05f,
                MakeNoise("Linen weave", new Color(0.16f, 0.155f, 0.145f), new Color(0.39f, 0.36f, 0.31f), 10f, 67.3f));
            Hair = MakeLit("Soot black hair", new Color(0.026f, 0.022f, 0.021f), 0f, 0.16f);
            AmberLeather = MakeLit("Oath amber leather", new Color(0.38f, 0.17f, 0.035f), 0.08f, 0.3f,
                MakeNoise("Amber leather grain", new Color(0.16f, 0.055f, 0.012f), new Color(0.54f, 0.27f, 0.055f), 5.8f, 143.2f));
            PolishedSteel = MakeLit("Edge polished steel", new Color(0.48f, 0.52f, 0.56f), 0.86f, 0.55f);
            ErrorCyan = MakeCorruption("Cyan error", new Color(0.01f, 0.17f, 0.22f), new Color(0f, 1.1f, 1.5f));
            ErrorMagenta = MakeCorruption("Magenta error", new Color(0.25f, 0.015f, 0.12f), new Color(1.5f, 0f, 0.72f));
            Ember = MakeLit("Checkpoint ember", new Color(0.66f, 0.14f, 0.015f), 0.08f, 0.22f,
                null, new Color(1.65f, 0.22f, 0.01f));
            Water = MakeTransparent("Black water", new Color(0.025f, 0.07f, 0.09f, 0.76f), 0.78f);
            Mist = MakeTransparent("World mist", new Color(0.32f, 0.39f, 0.48f, 0.12f), 0f);
        }

        public static float GroundHeight(float x, float z)
        {
            var broad = (Mathf.PerlinNoise((x + 431f) * 0.012f, (z + 817f) * 0.012f) - 0.5f) * 5.2f;
            var detail = Mathf.Sin(x * 0.055f) * Mathf.Cos(z * 0.047f) * 0.75f;
            var height = broad + detail;
            var road = Mathf.Clamp01((Mathf.Abs(x) - 7f) / 16f);
            if (z > -125f && z < 142f) height = Mathf.Lerp(0.1f, height, road);
            height = Flatten(height, x, z, -74f, -25f, 30f, 0.35f);
            height = Flatten(height, x, z, 69f, 5f, 31f, -0.1f);
            height = Flatten(height, x, z, 0f, 65f, 27f, 0.4f);
            height = Flatten(height, x, z, 0f, 126f, 36f, 0.15f);
            return height;
        }

        public static GameObject CreateTerrain(string name, Transform parent, float size, int resolution)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var mesh = new Mesh { name = $"{name}_Mesh", indexFormat = IndexFormat.UInt32 };
            var side = resolution + 1;
            var vertices = new Vector3[side * side];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[resolution * resolution * 6];
            for (var z = 0; z <= resolution; z++)
            for (var x = 0; x <= resolution; x++)
            {
                var px = (x / (float)resolution - 0.5f) * size;
                var pz = (z / (float)resolution - 0.5f) * size;
                var index = z * side + x;
                vertices[index] = new Vector3(px, GroundHeight(px, pz), pz);
                uv[index] = new Vector2(px, pz) * 0.075f;
            }
            var triangle = 0;
            for (var z = 0; z < resolution; z++)
            for (var x = 0; x < resolution; x++)
            {
                var index = z * side + x;
                triangles[triangle++] = index;
                triangles[triangle++] = index + side;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = index + 1;
                triangles[triangle++] = index + side;
                triangles[triangle++] = index + side + 1;
            }
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = Moss;
            var terrainCollider = root.AddComponent<MeshCollider>();
            terrainCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation |
                                             MeshColliderCookingOptions.EnableMeshCleaning |
                                             MeshColliderCookingOptions.WeldColocatedVertices;
            terrainCollider.sharedMesh = mesh;
            root.isStatic = true;

            var failSafe = new GameObject("Terrain collision fail-safe");
            failSafe.transform.SetParent(root.transform, false);
            failSafe.transform.localPosition = new Vector3(0f, -8f, 0f);
            var failSafeCollider = failSafe.AddComponent<BoxCollider>();
            failSafeCollider.size = new Vector3(size, 4f, size);
            failSafe.isStatic = true;
            return root;
        }

        public static GameObject Block(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, Vector3 rotation = default, bool collider = true)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.position = position;
            block.transform.localScale = scale;
            block.transform.eulerAngles = rotation;
            block.GetComponent<Renderer>().sharedMaterial = material;
            var box = block.GetComponent<BoxCollider>();
            box.enabled = collider;
            block.isStatic = collider;
            return block;
        }

        public static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material material, Vector3 localRotation = default)
        {
            var visual = GameObject.CreatePrimitive(type);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;
            visual.transform.localEulerAngles = localRotation;
            visual.GetComponent<Renderer>().sharedMaterial = material;
            var collider = visual.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            return visual;
        }

        public static GameObject LocalBlock(string name, Transform parent, Vector3 localPosition, Vector3 localScale,
            Material material, Vector3 localRotation = default)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = localScale;
            block.transform.localEulerAngles = localRotation;
            block.GetComponent<Renderer>().sharedMaterial = material;
            var collider = block.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            return block;
        }

        public static GameObject Frustum(string name, Transform parent, Vector3 localPosition, float bottomRadius,
            float topRadius, float height, int sides, Material material, Vector3 localRotation = default, bool collider = false)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localEulerAngles = localRotation;
            var mesh = BuildFrustumMesh(name, bottomRadius, topRadius, height, Mathf.Max(3, sides));
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collider) root.AddComponent<MeshCollider>().sharedMesh = mesh;
            return root;
        }

        public static GameObject PolygonPlate(string name, Transform parent, Vector3 localPosition, float width,
            float height, float depth, Material material, Vector3 localRotation = default, float asymmetry = 0f)
        {
            var points = new[]
            {
                new Vector2(-0.43f, 0.5f),
                new Vector2(0.43f, 0.5f),
                new Vector2(0.5f, 0.24f),
                new Vector2(0.38f + asymmetry * 0.08f, -0.31f),
                new Vector2(asymmetry * 0.08f, -0.5f),
                new Vector2(-0.42f + asymmetry * 0.04f, -0.34f),
                new Vector2(-0.5f, 0.2f)
            };
            return MeshObject(name, parent, localPosition, localRotation,
                BuildExtrudedPolygonMesh(name, points, width, height, depth), material);
        }

        public static GameObject ClothPanel(string name, Transform parent, Vector3 localPosition, float width,
            float height, Material material, Vector3 localRotation = default, float tear = 0f)
        {
            var points = new[]
            {
                new Vector2(-0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.47f, -0.27f + tear * 0.07f),
                new Vector2(0.18f, -0.5f),
                new Vector2(-0.08f, -0.39f - tear * 0.06f),
                new Vector2(-0.34f, -0.48f + tear * 0.1f),
                new Vector2(-0.5f, -0.22f)
            };
            return MeshObject(name, parent, localPosition, localRotation,
                BuildExtrudedPolygonMesh(name, points, width, height, 0.028f), material);
        }

        public static GameObject Blade(string name, Transform parent, Vector3 localPosition, float length,
            float halfWidth, float ridgeDepth, Material material, Vector3 localRotation = default)
        {
            var widths = new[] { halfWidth * 0.88f, halfWidth, halfWidth * 0.82f, halfWidth * 0.68f, 0.004f };
            var heights = new[] { 0f, length * 0.07f, length * 0.63f, length * 0.84f, length };
            var vertices = new List<Vector3>(widths.Length * 4);
            var uv = new List<Vector2>(widths.Length * 4);
            for (var ring = 0; ring < widths.Length; ring++)
            {
                var width = widths[ring];
                var ridge = ring == widths.Length - 1 ? 0.002f : ridgeDepth;
                vertices.Add(new Vector3(-width, heights[ring], 0f));
                vertices.Add(new Vector3(0f, heights[ring], ridge));
                vertices.Add(new Vector3(width, heights[ring], 0f));
                vertices.Add(new Vector3(0f, heights[ring], -ridge));
                var v = heights[ring] / length;
                uv.Add(new Vector2(0f, v));
                uv.Add(new Vector2(0.5f, v));
                uv.Add(new Vector2(1f, v));
                uv.Add(new Vector2(0.5f, v));
            }

            var triangles = new List<int>();
            for (var ring = 0; ring < widths.Length - 1; ring++)
            for (var side = 0; side < 4; side++)
            {
                var nextSide = (side + 1) % 4;
                var a = ring * 4 + side;
                var b = ring * 4 + nextSide;
                var c = (ring + 1) * 4 + side;
                var d = (ring + 1) * 4 + nextSide;
                triangles.Add(a); triangles.Add(c); triangles.Add(d);
                triangles.Add(a); triangles.Add(d); triangles.Add(b);
            }
            triangles.Add(0); triangles.Add(2); triangles.Add(1);
            triangles.Add(0); triangles.Add(3); triangles.Add(2);

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return MeshObject(name, parent, localPosition, localRotation, mesh, material);
        }

        public static GameObject Rock(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, int seed, bool collider = false)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.localScale = scale;
            root.transform.eulerAngles = new Vector3(seed * 17f % 22f, seed * 67f % 360f, seed * 31f % 18f);
            var mesh = BuildRockMesh(name, 7 + Mathf.Abs(seed) % 3, seed);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collider) root.AddComponent<MeshCollider>().sharedMesh = mesh;
            root.isStatic = collider;
            return root;
        }

        public static void DeadTree(string name, Transform parent, Vector3 position, float scale, int seed)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.eulerAngles = Vector3.up * (seed * 73f % 360f);
            Frustum("Trunk", root.transform, Vector3.zero, 0.48f * scale, 0.2f * scale, 5.6f * scale, 7, DarkStone,
                new Vector3(0f, 0f, (seed % 3 - 1) * 5f));
            Branch(root.transform, new Vector3(0f, 3.2f * scale, 0f), new Vector3(2.3f, 2.2f, 0.6f) * scale, 0.18f * scale);
            Branch(root.transform, new Vector3(0f, 4.05f * scale, 0f), new Vector3(-1.7f, 2.5f, -0.8f) * scale, 0.14f * scale);
            if (seed % 2 == 0)
                Branch(root.transform, new Vector3(0f, 2.55f * scale, 0f), new Vector3(-1.8f, 1.3f, 0.6f) * scale, 0.13f * scale);
        }

        public static void RuinArch(string name, Transform parent, Vector3 position, float width, float height, float yaw,
            Material material)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.eulerAngles = Vector3.up * yaw;
            Block("Left carved pier", root.transform, root.transform.position + root.transform.right * (-width * 0.5f) + Vector3.up * height * 0.5f,
                new Vector3(1.45f, height, 2.1f), material, Vector3.up * yaw);
            Block("Right carved pier", root.transform, root.transform.position + root.transform.right * (width * 0.5f) + Vector3.up * height * 0.5f,
                new Vector3(1.45f, height * 0.92f, 2.1f), material, new Vector3(0f, yaw, 2.5f));
            Block("Broken lintel", root.transform, root.transform.position + Vector3.up * (height - 0.45f),
                new Vector3(width + 1.5f, 1.35f, 2.25f), material, new Vector3(0f, yaw, -1.5f));
            for (var i = 0; i < 5; i++)
                Rock($"Carved fracture {i}", root.transform,
                    root.transform.position + root.transform.right * Mathf.Lerp(-width * 0.42f, width * 0.42f, i / 4f) +
                    Vector3.up * (height - 1.25f), new Vector3(0.9f, 0.45f, 0.5f), DarkStone, i + Mathf.RoundToInt(yaw));
        }

        public static ParticleSystem CreateAtmosphere(Transform parent, Vector3 position, Vector3 boxSize, int maxParticles)
        {
            if (Application.isBatchMode) return null;
            var root = new GameObject("Drifting ash and mist");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            var system = root.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 16f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.5f, 0.55f, 0.13f), new Color(0.85f, 0.34f, 0.15f, 0.2f));
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.rateOverTime = Mathf.Max(8f, maxParticles / 8f);
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.55f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.02f, 0.16f);
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = Mist;
            system.Play();
            return system;
        }

        public static Light PointLight(string name, Transform parent, Vector3 localPosition, Color color, float intensity,
            float range, bool shadows = false)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return light;
        }

        private static float Flatten(float height, float x, float z, float cx, float cz, float radius, float target)
        {
            var distance = Vector2.Distance(new Vector2(x, z), new Vector2(cx, cz));
            var blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((distance - radius * 0.62f) / (radius * 0.38f)));
            return Mathf.Lerp(target, height, blend);
        }

        private static void Branch(Transform parent, Vector3 start, Vector3 direction, float radius)
        {
            var length = direction.magnitude;
            var branch = Frustum("Bare branch", parent, start + direction * 0.5f, radius, radius * 0.28f, length, 6, DarkStone);
            branch.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        }

        private static Mesh BuildFrustumMesh(string name, float bottomRadius, float topRadius, float height, int sides)
        {
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            for (var i = 0; i < sides; i++)
            {
                var angle = i * Mathf.PI * 2f / sides;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices.Add(direction * bottomRadius);
                vertices.Add(direction * topRadius + Vector3.up * height);
                uv.Add(new Vector2(i / (float)sides, 0f));
                uv.Add(new Vector2(i / (float)sides, 1f));
            }
            var bottomCenter = vertices.Count;
            vertices.Add(Vector3.zero);
            uv.Add(Vector2.one * 0.5f);
            var topCenter = vertices.Count;
            vertices.Add(Vector3.up * height);
            uv.Add(Vector2.one * 0.5f);
            for (var i = 0; i < sides; i++)
            {
                var next = (i + 1) % sides;
                triangles.Add(i * 2); triangles.Add(next * 2 + 1); triangles.Add(i * 2 + 1);
                triangles.Add(i * 2); triangles.Add(next * 2); triangles.Add(next * 2 + 1);
                triangles.Add(bottomCenter); triangles.Add(next * 2); triangles.Add(i * 2);
                triangles.Add(topCenter); triangles.Add(i * 2 + 1); triangles.Add(next * 2 + 1);
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildRockMesh(string name, int sides, int seed)
        {
            var random = new System.Random(seed * 7919 + 31);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (var ring = 0; ring < 3; ring++)
            {
                var y = ring == 0 ? -0.5f : ring == 1 ? 0.14f : 0.55f;
                var radius = ring == 0 ? 0.72f : ring == 1 ? 1f : 0.46f;
                for (var i = 0; i < sides; i++)
                {
                    var angle = (i + ring * 0.17f) * Mathf.PI * 2f / sides;
                    var jitter = 0.82f + (float)random.NextDouble() * 0.32f;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius * jitter, y + ((float)random.NextDouble() - 0.5f) * 0.12f,
                        Mathf.Sin(angle) * radius * jitter));
                }
            }
            for (var ring = 0; ring < 2; ring++)
            for (var i = 0; i < sides; i++)
            {
                var next = (i + 1) % sides;
                var a = ring * sides + i;
                var b = ring * sides + next;
                var c = (ring + 1) * sides + i;
                var d = (ring + 1) * sides + next;
                triangles.Add(a); triangles.Add(d); triangles.Add(c);
                triangles.Add(a); triangles.Add(b); triangles.Add(d);
            }
            for (var i = 1; i < sides - 1; i++)
            {
                triangles.Add(i); triangles.Add(i + 1); triangles.Add(0);
                var top = sides * 2;
                triangles.Add(top); triangles.Add(top + i + 1); triangles.Add(top + i);
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject MeshObject(string name, Transform parent, Vector3 localPosition,
            Vector3 localRotation, Mesh mesh, Material material)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localEulerAngles = localRotation;
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = material;
            return root;
        }

        private static Mesh BuildExtrudedPolygonMesh(string name, IReadOnlyList<Vector2> points, float width,
            float height, float depth)
        {
            var count = points.Count;
            var vertices = new List<Vector3>(count * 2);
            var uv = new List<Vector2>(count * 2);
            for (var side = 0; side < 2; side++)
            for (var i = 0; i < count; i++)
            {
                var point = points[i];
                vertices.Add(new Vector3(point.x * width, point.y * height, side == 0 ? depth * 0.5f : -depth * 0.5f));
                uv.Add(new Vector2(point.x + 0.5f, point.y + 0.5f));
            }

            var triangles = new List<int>();
            for (var i = 1; i < count - 1; i++)
            {
                triangles.Add(0); triangles.Add(i + 1); triangles.Add(i);
                triangles.Add(count); triangles.Add(count + i); triangles.Add(count + i + 1);
            }
            for (var i = 0; i < count; i++)
            {
                var next = (i + 1) % count;
                triangles.Add(i); triangles.Add(next); triangles.Add(count + next);
                triangles.Add(i); triangles.Add(count + next); triangles.Add(count + i);
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Texture2D MakeNoise(string name, Color low, Color high, float scale, float seed)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, true) { name = name, wrapMode = TextureWrapMode.Repeat };
            var colors = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var noise = Mathf.PerlinNoise(seed + x / (float)size * scale, seed * 0.37f + y / (float)size * scale);
                var grain = Mathf.PerlinNoise(seed * 1.7f + x * 0.31f, seed + y * 0.31f) * 0.12f;
                colors[y * size + x] = Color.Lerp(low, high, Mathf.Clamp01(noise + grain));
            }
            texture.SetPixels(colors);
            texture.Apply(true, false);
            return texture;
        }

        private static Material MakeLit(string name, Color color, float metallic, float smoothness,
            Texture2D texture = null, Color? emission = null)
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse");
            var material = new Material(shader) { name = name, color = color, mainTexture = texture };
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (texture != null) material.mainTextureScale = Vector2.one * 1.25f;
            if (emission.HasValue && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            return material;
        }

        private static Material MakeCorruption(string name, Color baseColor, Color emission)
        {
            var shader = Resources.Load<Shader>("Shaders/ErrorCorruption") ?? Shader.Find("ErrorDungeon/Corruption");
            if (shader == null) return MakeLit(name, baseColor, 0.22f, 0.28f, null, emission);
            var material = new Material(shader) { name = name };
            material.SetColor("_Color", baseColor);
            material.SetColor("_ErrorColor", emission);
            material.SetFloat("_ErrorIntensity", 1.25f);
            return material;
        }

        private static Material MakeTransparent(string name, Color color, float smoothness)
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3f);
            if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = 3000;
            return material;
        }
    }
}
