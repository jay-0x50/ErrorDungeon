using System.Collections.Generic;
using UnityEngine;

namespace ErrorDungeon
{
    internal enum HumanoidLook
    {
        Wanderer,
        HollowSoldier,
        CastleGuard,
        Doyun
    }

    internal static class ProceduralHumanoid
    {
        public static ProceduralHumanoidAnimator Build(Transform actor, HumanoidLook look, bool spear = false)
        {
            var scale = look == HumanoidLook.Doyun ? 1.43f : look == HumanoidLook.CastleGuard ? 1.12f : 1f;
            var visualName = look == HumanoidLook.Wanderer
                ? "The nameless wanderer - layered visual"
                : look == HumanoidLook.Doyun
                    ? "Doyun - fractured general armor"
                    : look == HumanoidLook.CastleGuard
                        ? "Castle executioner visual"
                        : "Hollow soldier visual";
            var visual = new GameObject(visualName);
            visual.transform.SetParent(actor, false);
            visual.transform.localScale = Vector3.one * scale;

            var skeleton = new GameObject("Animated human skeleton");
            skeleton.transform.SetParent(visual.transform, false);
            var pelvis = Joint("Pelvis", skeleton.transform, new Vector3(0f, 0.91f, 0f));
            var chest = Joint("Chest", pelvis, new Vector3(0f, 0.39f, 0f));

            var cloth = look == HumanoidLook.Wanderer ? ProceduralArt.Cloth : ProceduralArt.EnemyMetal;
            var armor = look == HumanoidLook.Wanderer ? ProceduralArt.PlayerMetal : ProceduralArt.EnemyMetal;
            var accent = look == HumanoidLook.Wanderer ? ProceduralArt.Leather : ProceduralArt.Rust;
            var skin = look == HumanoidLook.Wanderer ? ProceduralArt.Skin : ProceduralArt.DarkStone;

            BuildTorso(pelvis, chest, look, cloth, armor, accent);
            BuildHead(chest, look, skin, cloth, armor, accent);

            var leftArm = BuildArm(chest, "Left", -1f, armor, cloth, accent, look);
            var rightArm = BuildArm(chest, "Right", 1f, armor, cloth, accent, look);
            var leftLeg = BuildLeg(pelvis, "Left", -1f, cloth, armor, accent);
            var rightLeg = BuildLeg(pelvis, "Right", 1f, cloth, armor, accent);

            if (look == HumanoidLook.Wanderer)
            {
                BuildWandererLayers(skeleton.transform, chest, leftArm.Hand, rightArm.Hand);
                BuildSword(rightArm.Hand, 1f, false);
            }
            else if (look == HumanoidLook.Doyun)
            {
                BuildDoyunLayers(skeleton.transform, chest);
                BuildSpear(rightArm.Hand, 1.22f, true);
            }
            else
            {
                BuildSoldierLayers(skeleton.transform, chest, look == HumanoidLook.CastleGuard);
                if (spear) BuildSpear(rightArm.Hand, look == HumanoidLook.CastleGuard ? 1.08f : 0.92f, false);
                else BuildSword(rightArm.Hand, look == HumanoidLook.CastleGuard ? 1.15f : 0.86f, true);
            }

            ConfigureLod(visual);
            var animator = visual.AddComponent<ProceduralHumanoidAnimator>();
            animator.Configure(actor, skeleton.transform, pelvis, chest,
                leftArm.Upper, leftArm.Lower, rightArm.Upper, rightArm.Lower,
                leftLeg.Upper, leftLeg.Lower, rightLeg.Upper, rightLeg.Lower,
                look == HumanoidLook.Doyun ? 0.72f : look == HumanoidLook.CastleGuard ? 0.9f : 1f);
            return animator;
        }

        private static void ConfigureLod(GameObject visual)
        {
            var highDetail = visual.GetComponentsInChildren<Renderer>();
            var mediumDetail = new List<Renderer>();
            foreach (var renderer in highDetail)
            {
                var part = renderer.gameObject.name;
                if (part.Contains("Ribcage") || part.Contains("abdomen") || part.Contains("thigh") ||
                    part.Contains("shin") || part.Contains("upper arm") || part.Contains("forearm") ||
                    part.Contains("Human head") || part.Contains("helmet") || part.Contains("cloak panel") ||
                    part.Contains("skirt plate") || part.Contains("coat plate") || part.Contains("sword blade") ||
                    part.Contains("Spear shaft") || part.Contains("spearhead") || part.Contains("Shield") ||
                    part.Contains("plate") || part.Contains("Blade"))
                    mediumDetail.Add(renderer);
            }
            var lodGroup = visual.AddComponent<LODGroup>();
            lodGroup.fadeMode = LODFadeMode.CrossFade;
            lodGroup.animateCrossFading = true;
            lodGroup.SetLODs(new[]
            {
                new LOD(0.16f, highDetail),
                new LOD(0.035f, mediumDetail.ToArray())
            });
            lodGroup.RecalculateBounds();
        }

        private static void BuildTorso(Transform pelvis, Transform chest, HumanoidLook look, Material cloth, Material armor,
            Material accent)
        {
            ProceduralArt.Primitive(PrimitiveType.Capsule, "Anatomical abdomen", pelvis, new Vector3(0f, 0.13f, 0f),
                new Vector3(0.47f, 0.27f, 0.34f), cloth);
            ProceduralArt.Primitive(PrimitiveType.Capsule, "Ribcage", chest, new Vector3(0f, 0.13f, 0f),
                new Vector3(0.62f, 0.34f, 0.42f), armor);
            ProceduralArt.Frustum("Tapered waist armor", pelvis, new Vector3(0f, -0.14f, 0f),
                0.39f, 0.31f, 0.42f, 12, cloth);
            ProceduralArt.LocalBlock("Worn waist belt", pelvis, new Vector3(0f, -0.03f, 0.01f),
                new Vector3(0.79f, 0.105f, 0.39f),
                look == HumanoidLook.Wanderer ? ProceduralArt.AmberLeather : accent);
            ProceduralArt.LocalBlock("Belt buckle", pelvis, new Vector3(0f, -0.03f, 0.225f),
                new Vector3(0.14f, 0.13f, 0.035f), ProceduralArt.PolishedSteel);

            if (look == HumanoidLook.Wanderer)
            {
                ProceduralArt.PolygonPlate("Layered chest plate left", chest, new Vector3(-0.155f, 0.15f, 0.235f),
                    0.33f, 0.47f, 0.065f, ProceduralArt.PlayerMetal, new Vector3(-4f, -4f, 5f), -0.2f);
                ProceduralArt.PolygonPlate("Layered chest plate right", chest, new Vector3(0.155f, 0.15f, 0.235f),
                    0.33f, 0.47f, 0.065f, ProceduralArt.PlayerMetal, new Vector3(-4f, 4f, -5f), 0.2f);
                ProceduralArt.PolygonPlate("Lower oath plate", pelvis, new Vector3(0f, -0.08f, 0.235f),
                    0.34f, 0.28f, 0.055f, ProceduralArt.PlayerMetal, new Vector3(-7f, 0f, 0f));
                ProceduralArt.LocalBlock("Amber belt pouch", pelvis, new Vector3(-0.31f, -0.11f, 0.16f),
                    new Vector3(0.2f, 0.24f, 0.12f), ProceduralArt.AmberLeather, new Vector3(5f, 12f, 4f));
            }
            else
            {
                ProceduralArt.PolygonPlate("Corroded breastplate", chest, new Vector3(0f, 0.14f, 0.235f),
                    0.58f, 0.51f, 0.085f, armor, new Vector3(-5f, 0f, 0f), 0.12f);
                ProceduralArt.LocalBlock("Breastplate ridge", chest, new Vector3(0f, 0.22f, 0.285f),
                    new Vector3(0.075f, 0.38f, 0.04f), accent);
            }
        }

        private static void BuildHead(Transform chest, HumanoidLook look, Material skin, Material cloth, Material armor,
            Material accent)
        {
            var neck = Joint("Neck", chest, new Vector3(0f, 0.52f, 0f));
            ProceduralArt.Primitive(PrimitiveType.Capsule, "Neck", neck, new Vector3(0f, 0.04f, 0f),
                new Vector3(0.19f, 0.15f, 0.18f), look == HumanoidLook.Wanderer ? ProceduralArt.Skin : cloth);
            var head = Joint("Head", neck, new Vector3(0f, 0.22f, 0f));
            ProceduralArt.Primitive(PrimitiveType.Sphere, "Human head", head, Vector3.zero,
                new Vector3(0.34f, 0.39f, 0.32f), skin);

            if (look == HumanoidLook.Wanderer)
            {
                ProceduralArt.Primitive(PrimitiveType.Sphere, "Shoulder length hair", head, new Vector3(0f, 0.055f, -0.055f),
                    new Vector3(0.37f, 0.42f, 0.34f), ProceduralArt.Hair);
                ProceduralArt.Primitive(PrimitiveType.Sphere, "Visible weathered face", head, new Vector3(0f, -0.015f, 0.105f),
                    new Vector3(0.285f, 0.325f, 0.205f), ProceduralArt.Skin);
                ProceduralArt.Frustum("Nose", head, new Vector3(0f, -0.015f, 0.205f),
                    0.052f, 0.018f, 0.13f, 8, ProceduralArt.Skin, new Vector3(90f, 0f, 0f));
                ProceduralArt.LocalBlock("Scar across face", head, new Vector3(0.055f, 0.035f, 0.224f),
                    new Vector3(0.012f, 0.13f, 0.01f), ProceduralArt.Rust, new Vector3(0f, 0f, 24f));
                for (var sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    var side = sideIndex == 0 ? -1f : 1f;
                    ProceduralArt.Primitive(PrimitiveType.Sphere, side < 0 ? "Left eye" : "Right eye", head,
                        new Vector3(side * 0.075f, 0.055f, 0.211f), new Vector3(0.042f, 0.027f, 0.018f),
                        ProceduralArt.Bone);
                    ProceduralArt.Primitive(PrimitiveType.Sphere, side < 0 ? "Left pupil" : "Right pupil", head,
                        new Vector3(side * 0.075f, 0.055f, 0.224f), Vector3.one * 0.013f, ProceduralArt.ErrorCyan);
                }
                ProceduralArt.Frustum("Broken hood", head, new Vector3(0f, -0.21f, -0.035f),
                    0.31f, 0.25f, 0.62f, 12, cloth, new Vector3(0f, 13f, 0f));
                ProceduralArt.Frustum("Hood shadow rim", head, new Vector3(0f, -0.13f, 0.13f),
                    0.245f, 0.205f, 0.08f, 14, ProceduralArt.Hair, new Vector3(78f, 0f, 0f));
            }
            else
            {
                ProceduralArt.Primitive(PrimitiveType.Sphere, "Fitted iron helmet", head, new Vector3(0f, 0.025f, 0f),
                    new Vector3(0.385f, 0.43f, 0.36f), armor);
                ProceduralArt.LocalBlock("Helmet face plate", head, new Vector3(0f, -0.025f, 0.21f),
                    new Vector3(0.31f, 0.28f, 0.055f), ProceduralArt.DarkStone, new Vector3(-6f, 0f, 0f));
                for (var slot = -1; slot <= 1; slot++)
                    ProceduralArt.LocalBlock($"Breathing slot {slot + 2}", head, new Vector3(slot * 0.075f, -0.045f, 0.242f),
                        new Vector3(0.026f, 0.12f, 0.015f), accent, new Vector3(0f, 0f, slot * 4f));
                ProceduralArt.Frustum("Helmet crest", head, new Vector3(0f, 0.18f, -0.015f),
                    0.18f, 0.015f, look == HumanoidLook.Doyun ? 0.82f : 0.5f, 7, accent,
                    new Vector3(-7f, 0f, 0f));
            }
        }

        private static Limb BuildArm(Transform chest, string name, float side, Material armor, Material cloth,
            Material accent, HumanoidLook look)
        {
            var upper = Joint($"{name} shoulder", chest, new Vector3(side * 0.37f, 0.34f, 0f));
            ProceduralArt.Primitive(PrimitiveType.Sphere, $"{name} rounded shoulder", upper, new Vector3(0f, -0.03f, 0f),
                Vector3.one * (look == HumanoidLook.Doyun ? 0.28f : 0.24f), accent);
            ProceduralArt.PolygonPlate($"{name} shoulder plate", upper,
                new Vector3(side * 0.055f, -0.035f, 0.105f), look == HumanoidLook.Doyun ? 0.52f : 0.4f,
                look == HumanoidLook.Doyun ? 0.36f : 0.29f, 0.075f, armor,
                new Vector3(8f, side * 12f, side * -9f), side * 0.3f);
            ProceduralArt.Primitive(PrimitiveType.Capsule, $"{name} upper arm", upper, new Vector3(0f, -0.235f, 0f),
                new Vector3(0.18f, 0.235f, 0.18f), armor);
            var lower = Joint($"{name} elbow", upper, new Vector3(0f, -0.46f, 0f));
            ProceduralArt.Primitive(PrimitiveType.Sphere, $"{name} elbow guard", lower, Vector3.zero,
                Vector3.one * 0.17f, accent);
            ProceduralArt.Primitive(PrimitiveType.Capsule, $"{name} forearm", lower, new Vector3(0f, -0.22f, 0f),
                new Vector3(0.15f, 0.22f, 0.15f), cloth);
            ProceduralArt.PolygonPlate($"{name} vambrace plate", lower, new Vector3(0f, -0.21f, 0.13f),
                0.24f, 0.34f, 0.055f, look == HumanoidLook.Wanderer ? ProceduralArt.PlayerMetal : accent,
                new Vector3(-4f, 0f, side * 3f), side * 0.16f);
            var hand = Joint($"{name} hand joint", lower, new Vector3(0f, -0.44f, 0f));
            ProceduralArt.Primitive(PrimitiveType.Sphere, $"{name} hand", hand, Vector3.zero,
                new Vector3(0.16f, 0.19f, 0.15f), look == HumanoidLook.Wanderer ? ProceduralArt.Skin : accent);
            return new Limb(upper, lower, hand);
        }

        private static Limb BuildLeg(Transform pelvis, string name, float side, Material cloth, Material armor, Material accent)
        {
            var upper = Joint($"{name} hip", pelvis, new Vector3(side * 0.205f, -0.05f, 0f));
            ProceduralArt.Primitive(PrimitiveType.Capsule, $"{name} thigh", upper, new Vector3(0f, -0.245f, 0f),
                new Vector3(0.22f, 0.25f, 0.22f), cloth);
            var lower = Joint($"{name} knee", upper, new Vector3(0f, -0.49f, 0f));
            ProceduralArt.Primitive(PrimitiveType.Sphere, $"{name} knee guard", lower, new Vector3(0f, 0f, 0.035f),
                new Vector3(0.22f, 0.18f, 0.2f), armor);
            ProceduralArt.PolygonPlate($"{name} articulated knee plate", lower, new Vector3(0f, -0.015f, 0.155f),
                0.29f, 0.25f, 0.07f, armor, new Vector3(-5f, 0f, 0f), side * 0.1f);
            ProceduralArt.Primitive(PrimitiveType.Capsule, $"{name} shin", lower, new Vector3(0f, -0.225f, 0f),
                new Vector3(0.18f, 0.23f, 0.18f), armor);
            var foot = Joint($"{name} ankle", lower, new Vector3(0f, -0.45f, 0f));
            ProceduralArt.Primitive(PrimitiveType.Sphere, $"{name} leather boot", foot, new Vector3(0f, 0.035f, 0.095f),
                new Vector3(0.24f, 0.17f, 0.42f), ProceduralArt.Leather);
            ProceduralArt.LocalBlock($"{name} boot sole", foot, new Vector3(0f, -0.06f, 0.12f),
                new Vector3(0.25f, 0.055f, 0.44f), accent);
            return new Limb(upper, lower, foot);
        }

        private static void BuildWandererLayers(Transform skeleton, Transform chest, Transform leftHand, Transform rightHand)
        {
            for (var panel = -2; panel <= 2; panel++)
                ProceduralArt.ClothPanel($"Wind split cloak panel {panel + 3}", skeleton,
                    new Vector3(panel * 0.145f, 0.72f, -0.22f - Mathf.Abs(panel) * 0.018f),
                    0.2f, 0.92f - Mathf.Abs(panel) * 0.07f, ProceduralArt.Cloth,
                    new Vector3(7f + Mathf.Abs(panel) * 3f, panel * 3f, panel * -4f), panel * 0.17f);
            ProceduralArt.Frustum("Fur shoulder mantle", chest, new Vector3(0f, 0.31f, -0.01f),
                0.53f, 0.39f, 0.26f, 14, ProceduralArt.Linen);
            ProceduralArt.ClothPanel("Amber pilgrim sash", chest, new Vector3(-0.03f, 0.02f, 0.285f),
                0.72f, 0.13f, ProceduralArt.AmberLeather, new Vector3(0f, 0f, -19f), -0.2f);
            ProceduralArt.Frustum("Left wrist wrap", leftHand, new Vector3(0f, 0.06f, 0f),
                0.12f, 0.1f, 0.2f, 10, ProceduralArt.Linen);
            ProceduralArt.Frustum("Right wrist wrap", rightHand, new Vector3(0f, 0.06f, 0f),
                0.12f, 0.1f, 0.2f, 10, ProceduralArt.Linen);
            ProceduralArt.Primitive(PrimitiveType.Sphere, "Left cloak clasp", chest, new Vector3(-0.29f, 0.32f, 0.25f),
                Vector3.one * 0.09f, ProceduralArt.AmberLeather);
            ProceduralArt.Primitive(PrimitiveType.Sphere, "Right cloak clasp", chest, new Vector3(0.29f, 0.32f, 0.25f),
                Vector3.one * 0.09f, ProceduralArt.AmberLeather);
            BuildScabbard(skeleton);
            BuildShield(leftHand);
            ProceduralArt.PointLight("Wanderer cold rim", chest, new Vector3(0f, 0.38f, -0.34f),
                new Color(0.24f, 0.42f, 0.72f), 0.55f, 3.8f);
        }

        private static void BuildSoldierLayers(Transform skeleton, Transform chest, bool elite)
        {
            var panelCount = elite ? 7 : 5;
            for (var i = 0; i < panelCount; i++)
            {
                var t = panelCount == 1 ? 0f : i / (float)(panelCount - 1);
                var x = Mathf.Lerp(-0.38f, 0.38f, t);
                ProceduralArt.PolygonPlate($"Lamellar skirt plate {i + 1}", skeleton,
                    new Vector3(x, 0.67f, -0.02f), 0.16f, elite ? 0.59f : 0.5f, 0.075f,
                    ProceduralArt.EnemyMetal, new Vector3(i % 2 == 0 ? 4f : -3f, 0f, x * -13f),
                    i % 2 == 0 ? -0.25f : 0.25f);
            }
            ProceduralArt.Frustum("Ragged mail collar", chest, new Vector3(0f, 0.33f, 0f),
                elite ? 0.58f : 0.49f, 0.37f, 0.24f, 12, ProceduralArt.Rust);
        }

        private static void BuildDoyunLayers(Transform skeleton, Transform chest)
        {
            for (var i = 0; i < 9; i++)
            {
                var angle = i * Mathf.PI * 2f / 9f;
                ProceduralArt.PolygonPlate($"General coat plate {i + 1}", skeleton,
                    new Vector3(Mathf.Sin(angle) * 0.39f, 0.71f, Mathf.Cos(angle) * 0.22f),
                    0.18f, 0.7f, 0.085f, i % 3 == 0 ? ProceduralArt.Rust : ProceduralArt.DarkStone,
                    new Vector3(4f, angle * Mathf.Rad2Deg, -Mathf.Sin(angle) * 7f), Mathf.Sin(angle) * 0.3f);
            }
            ProceduralArt.Frustum("General command mantle", chest, new Vector3(0f, 0.34f, 0f),
                0.66f, 0.43f, 0.31f, 14, ProceduralArt.Rust);
            for (var i = 0; i < 5; i++)
            {
                var crack = ProceduralArt.Frustum($"Living armor seam {i + 1}", chest,
                    new Vector3(-0.24f + i * 0.12f, -0.05f + i * 0.09f, 0.28f),
                    0.035f, 0.006f, 0.32f, 5, i % 2 == 0 ? ProceduralArt.ErrorCyan : ProceduralArt.ErrorMagenta,
                    new Vector3(68f, i * 17f, i % 2 == 0 ? -13f : 16f));
                crack.AddComponent<AmbientMotion>().Configure(new Vector3(0f, 7f, 0f), 0.012f, 2.5f, i);
            }
        }

        private static void BuildSword(Transform hand, float scale, bool rusted)
        {
            var weapon = Joint("Hand-held sword", hand, new Vector3(0f, -0.02f, 0.06f));
            weapon.localRotation = Quaternion.Euler(8f, 0f, 178f);
            ProceduralArt.Frustum("Leather sword grip", weapon, new Vector3(0f, 0.02f, 0f),
                0.055f * scale, 0.05f * scale, 0.42f * scale, 9, ProceduralArt.Leather);
            ProceduralArt.Frustum("Left swept quillon", weapon, new Vector3(-0.25f * scale, 0.43f * scale, 0f),
                0.055f * scale, 0.025f * scale, 0.36f * scale, 7, ProceduralArt.Rust,
                new Vector3(0f, 0f, 78f));
            ProceduralArt.Frustum("Right swept quillon", weapon, new Vector3(0.25f * scale, 0.43f * scale, 0f),
                0.055f * scale, 0.025f * scale, 0.36f * scale, 7, ProceduralArt.Rust,
                new Vector3(0f, 0f, -78f));
            ProceduralArt.Frustum("Rain guard collar", weapon, new Vector3(0f, 0.39f * scale, 0f),
                0.11f * scale, 0.075f * scale, 0.16f * scale, 8, ProceduralArt.PlayerMetal);
            ProceduralArt.Blade(rusted ? "Notched rust sword blade" : "Forged oath sword blade", weapon,
                new Vector3(0f, 0.48f * scale, 0f), 1.5f * scale, 0.14f * scale, 0.045f * scale,
                rusted ? ProceduralArt.Rust : ProceduralArt.PolishedSteel);
            var trailTip = Joint("Sword motion trail tip", weapon, new Vector3(0f, 1.93f * scale, 0f));
            var trail = trailTip.gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.11f;
            trail.minVertexDistance = 0.035f;
            trail.startWidth = 0.13f * scale;
            trail.endWidth = 0.015f * scale;
            trail.sharedMaterial = rusted ? ProceduralArt.ErrorMagenta : ProceduralArt.ErrorCyan;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            ProceduralArt.Frustum("Faceted sword pommel", weapon, new Vector3(0f, -0.18f * scale, 0f),
                0.09f * scale, 0.055f * scale, 0.18f * scale, 8, ProceduralArt.Rust,
                new Vector3(0f, 22.5f, 180f));
        }

        private static void BuildSpear(Transform hand, float scale, bool lightning)
        {
            var weapon = Joint(lightning ? "Doyun lightning spear" : "Hand-held spear", hand, new Vector3(0f, -0.35f, 0.06f));
            weapon.localRotation = Quaternion.Euler(4f, 0f, 176f);
            ProceduralArt.Frustum("Spear shaft", weapon, Vector3.zero, 0.045f * scale, 0.04f * scale,
                2.8f * scale, 9, ProceduralArt.Leather);
            ProceduralArt.Frustum("Spearhead socket", weapon, new Vector3(0f, 2.7f * scale, 0f),
                0.075f * scale, 0.055f * scale, 0.2f * scale, 8,
                lightning ? ProceduralArt.ErrorCyan : ProceduralArt.Rust);
            var head = ProceduralArt.Blade("Leaf shaped spearhead", weapon, new Vector3(0f, 2.82f * scale, 0f),
                0.68f * scale, 0.18f * scale, 0.055f * scale,
                lightning ? ProceduralArt.ErrorCyan : ProceduralArt.PolishedSteel);
            if (lightning) head.AddComponent<AmbientMotion>().Configure(new Vector3(0f, 14f, 0f), 0.026f, 3f);
        }

        private static void BuildShield(Transform hand)
        {
            var shield = Joint("Left hand shield socket", hand, new Vector3(0f, -0.02f, 0.17f));
            ProceduralArt.PolygonPlate("Shield iron rim", shield, Vector3.zero, 0.68f, 0.86f, 0.09f,
                ProceduralArt.PlayerMetal, new Vector3(0f, 0f, -3f), -0.18f);
            ProceduralArt.PolygonPlate("Shield weathered face", shield, new Vector3(0f, 0f, 0.052f),
                0.54f, 0.7f, 0.035f, ProceduralArt.Cloth, new Vector3(0f, 0f, -3f), -0.18f);
            ProceduralArt.Primitive(PrimitiveType.Sphere, "Shield oath boss", shield, new Vector3(0f, 0.04f, 0.105f),
                new Vector3(0.16f, 0.16f, 0.09f), ProceduralArt.AmberLeather);
            ProceduralArt.LocalBlock("Shield diagonal brace", shield, new Vector3(0f, -0.02f, 0.086f),
                new Vector3(0.055f, 0.58f, 0.028f), ProceduralArt.Rust, new Vector3(0f, 0f, -24f));
        }

        private static void BuildScabbard(Transform skeleton)
        {
            var scabbard = Joint("Left hip scabbard socket", skeleton, new Vector3(-0.34f, 0.84f, -0.08f));
            scabbard.localRotation = Quaternion.Euler(4f, -9f, 164f);
            ProceduralArt.Frustum("Oath sword scabbard", scabbard, Vector3.zero, 0.09f, 0.065f, 1.45f, 8,
                ProceduralArt.Leather);
            ProceduralArt.Frustum("Scabbard steel throat", scabbard, new Vector3(0f, -0.015f, 0f),
                0.11f, 0.1f, 0.13f, 8, ProceduralArt.PlayerMetal);
            ProceduralArt.Frustum("Scabbard steel chape", scabbard, new Vector3(0f, 1.36f, 0f),
                0.075f, 0.02f, 0.17f, 8, ProceduralArt.PlayerMetal);
        }

        private static Transform Joint(string name, Transform parent, Vector3 localPosition)
        {
            var joint = new GameObject(name).transform;
            joint.SetParent(parent, false);
            joint.localPosition = localPosition;
            return joint;
        }

        private readonly struct Limb
        {
            public readonly Transform Upper;
            public readonly Transform Lower;
            public readonly Transform Hand;

            public Limb(Transform upper, Transform lower, Transform hand)
            {
                Upper = upper;
                Lower = lower;
                Hand = hand;
            }
        }
    }

    public sealed class ProceduralHumanoidAnimator : MonoBehaviour
    {
        private Transform actor;
        private Transform skeleton;
        private Transform pelvis;
        private Transform chest;
        private Transform leftArm;
        private Transform leftForearm;
        private Transform rightArm;
        private Transform rightForearm;
        private Transform leftLeg;
        private Transform leftShin;
        private Transform rightLeg;
        private Transform rightShin;
        private ActorStateMachine state;
        private CombatVitals vitals;
        private TrailRenderer weaponTrail;
        private Vector3 previousPosition;
        private float strideStrength;
        private float stridePhase;
        private float animationScale = 1f;
        private float stateEnteredAt;
        private float attackStartedAt;
        private float attackWindup = 0.22f;
        private float attackRecovery = 0.35f;
        private int attackVariant;
        private bool attackPrimed;
        private float hitStartedAt;
        private float hitSide;
        private float hitForward = -1f;
        private int hitVariant;

        internal void Configure(Transform actorRoot, Transform skeletonRoot, Transform pelvisBone, Transform chestBone,
            Transform leftUpperArm, Transform leftLowerArm, Transform rightUpperArm, Transform rightLowerArm,
            Transform leftUpperLeg, Transform leftLowerLeg, Transform rightUpperLeg, Transform rightLowerLeg,
            float speedScale)
        {
            actor = actorRoot;
            skeleton = skeletonRoot;
            pelvis = pelvisBone;
            chest = chestBone;
            leftArm = leftUpperArm;
            leftForearm = leftLowerArm;
            rightArm = rightUpperArm;
            rightForearm = rightLowerArm;
            leftLeg = leftUpperLeg;
            leftShin = leftLowerLeg;
            rightLeg = rightUpperLeg;
            rightShin = rightLowerLeg;
            animationScale = speedScale;

            if (state != null) state.Changed -= OnStateChanged;
            if (vitals != null) vitals.Damaged -= OnDamaged;
            state = actor != null ? actor.GetComponent<ActorStateMachine>() : null;
            vitals = actor != null ? actor.GetComponent<CombatVitals>() : null;
            weaponTrail = actor != null ? actor.GetComponentInChildren<TrailRenderer>() : null;
            if (state != null) state.Changed += OnStateChanged;
            if (vitals != null) vitals.Damaged += OnDamaged;
            previousPosition = actor != null ? actor.position : Vector3.zero;
            stateEnteredAt = Time.time;
            attackStartedAt = Time.time;
        }

        public void BeginAttack(float windup, float recovery, int variant)
        {
            attackWindup = Mathf.Max(0.06f, windup);
            attackRecovery = Mathf.Max(0.08f, recovery);
            attackVariant = variant;
            attackStartedAt = Time.time;
            attackPrimed = true;
        }

        private void OnDestroy()
        {
            if (state != null) state.Changed -= OnStateChanged;
            if (vitals != null) vitals.Damaged -= OnDamaged;
        }

        private void OnStateChanged(ActorState previous, ActorState next)
        {
            stateEnteredAt = Time.time;
            if (next != ActorState.Attack) return;
            if (!attackPrimed)
            {
                attackWindup = 0.22f;
                attackRecovery = 0.35f;
                attackVariant++;
                attackStartedAt = Time.time;
            }
            attackPrimed = false;
        }

        private void OnDamaged(CombatVitals damaged, DamageInfo hit, float damage)
        {
            hitStartedAt = Time.time;
            hitVariant++;
            var direction = Vector3.ProjectOnPlane(hit.HitDirection, Vector3.up);
            if (direction.sqrMagnitude < 0.001f && hit.Attacker != null)
                direction = actor.position - hit.Attacker.transform.position;
            if (direction.sqrMagnitude < 0.001f) direction = -actor.forward;
            var localDirection = actor.InverseTransformDirection(direction.normalized);
            hitSide = Mathf.Clamp(localDirection.x, -1f, 1f);
            hitForward = Mathf.Clamp(localDirection.z, -1f, 1f);
        }

        private void LateUpdate()
        {
            if (actor == null || skeleton == null || Time.deltaTime <= 0f) return;
            var planarDelta = Vector3.ProjectOnPlane(actor.position - previousPosition, Vector3.up);
            previousPosition = actor.position;
            var speed = Mathf.Clamp(planarDelta.magnitude / Time.deltaTime, 0f, 8f);
            var locomoting = state == null || state.Current == ActorState.Locomotion;
            strideStrength = Mathf.MoveTowards(strideStrength, locomoting ? Mathf.Clamp01(speed / 3.2f) : 0f,
                Time.deltaTime * 5.5f);
            stridePhase += Time.deltaTime * (2.4f + speed * 1.25f) * animationScale;

            var swing = Mathf.Sin(stridePhase) * 31f * strideStrength;
            var lift = Mathf.Max(0f, Mathf.Sin(stridePhase)) * 7f * strideStrength;
            var breath = Mathf.Sin(Time.time * 1.65f) * 0.008f;
            var targetBodyRotation = Quaternion.Euler(0f, -swing * 0.08f, 0f);
            var chestRotation = Quaternion.Euler(breath * 35f, swing * -0.11f, swing * 0.04f);
            var skeletonOffset = Vector3.zero;
            var emitWeaponTrail = false;
            var leftArmRotation = Quaternion.Euler(swing * 0.72f, 0f, -5f);
            var rightArmRotation = Quaternion.Euler(-swing * 0.72f, 0f, 5f);
            var leftForearmRotation = Quaternion.Euler(-12f - Mathf.Max(0f, -swing) * 0.28f, 0f, 0f);
            var rightForearmRotation = Quaternion.Euler(-12f - Mathf.Max(0f, swing) * 0.28f, 0f, 0f);
            var leftLegRotation = Quaternion.Euler(-swing, 0f, -1.5f);
            var rightLegRotation = Quaternion.Euler(swing, 0f, 1.5f);
            var leftShinRotation = Quaternion.Euler(Mathf.Max(0f, swing) * 0.65f, 0f, 0f);
            var rightShinRotation = Quaternion.Euler(Mathf.Max(0f, -swing) * 0.65f, 0f, 0f);

            if (state != null)
            {
                switch (state.Current)
                {
                    case ActorState.Attack:
                        var attackAge = Mathf.Max(0f, Time.time - attackStartedAt);
                        var strikeDuration = Mathf.Clamp(attackRecovery * 0.38f, 0.1f, 0.2f);
                        emitWeaponTrail = attackAge >= attackWindup * 0.62f &&
                                          attackAge <= attackWindup + strikeDuration + 0.04f;
                        var attackIndex = ((attackVariant % 3) + 3) % 3;
                        Quaternion windBody;
                        Quaternion strikeBody;
                        Quaternion recoverBody;
                        Quaternion windRightArm;
                        Quaternion strikeRightArm;
                        Quaternion recoverRightArm;
                        Quaternion windLeftArm;
                        Quaternion strikeLeftArm;
                        Quaternion recoverLeftArm;
                        Quaternion windRightForearm;
                        Quaternion strikeRightForearm;
                        Quaternion recoverRightForearm;
                        var strikeOffset = Vector3.zero;

                        if (attackIndex == 0)
                        {
                            windBody = Quaternion.Euler(-5f, -36f, 7f);
                            strikeBody = Quaternion.Euler(-9f, 42f, -8f);
                            recoverBody = Quaternion.Euler(-4f, 10f, 0f);
                            windRightArm = Quaternion.Euler(-72f, -38f, 68f);
                            strikeRightArm = Quaternion.Euler(-84f, 42f, -64f);
                            recoverRightArm = Quaternion.Euler(-34f, 8f, 12f);
                            windLeftArm = Quaternion.Euler(-24f, 12f, -36f);
                            strikeLeftArm = Quaternion.Euler(-49f, -18f, -19f);
                            recoverLeftArm = Quaternion.Euler(-13f, 0f, -8f);
                            windRightForearm = Quaternion.Euler(-72f, 0f, 9f);
                            strikeRightForearm = Quaternion.Euler(-18f, 0f, -6f);
                            recoverRightForearm = Quaternion.Euler(-26f, 0f, 0f);
                            strikeOffset = new Vector3(-0.025f, 0f, 0.08f);
                        }
                        else if (attackIndex == 1)
                        {
                            windBody = Quaternion.Euler(-7f, 39f, -8f);
                            strikeBody = Quaternion.Euler(-10f, -44f, 9f);
                            recoverBody = Quaternion.Euler(-5f, -9f, 0f);
                            windRightArm = Quaternion.Euler(-82f, 46f, -59f);
                            strikeRightArm = Quaternion.Euler(-70f, -44f, 72f);
                            recoverRightArm = Quaternion.Euler(-37f, -7f, 13f);
                            windLeftArm = Quaternion.Euler(-45f, -17f, -20f);
                            strikeLeftArm = Quaternion.Euler(-27f, 15f, -38f);
                            recoverLeftArm = Quaternion.Euler(-14f, 0f, -8f);
                            windRightForearm = Quaternion.Euler(-22f, 0f, -7f);
                            strikeRightForearm = Quaternion.Euler(-54f, 0f, 10f);
                            recoverRightForearm = Quaternion.Euler(-28f, 0f, 0f);
                            strikeOffset = new Vector3(0.03f, 0f, 0.1f);
                        }
                        else
                        {
                            windBody = Quaternion.Euler(7f, -12f, 0f);
                            strikeBody = Quaternion.Euler(-19f, 4f, 0f);
                            recoverBody = Quaternion.Euler(-7f, 0f, 0f);
                            windRightArm = Quaternion.Euler(-51f, 16f, 24f);
                            strikeRightArm = Quaternion.Euler(-91f, 1f, 4f);
                            recoverRightArm = Quaternion.Euler(-45f, 5f, 11f);
                            windLeftArm = Quaternion.Euler(-61f, -18f, -42f);
                            strikeLeftArm = Quaternion.Euler(-77f, -9f, -29f);
                            recoverLeftArm = Quaternion.Euler(-18f, 0f, -9f);
                            windRightForearm = Quaternion.Euler(-108f, 0f, 0f);
                            strikeRightForearm = Quaternion.Euler(-7f, 0f, 0f);
                            recoverRightForearm = Quaternion.Euler(-31f, 0f, 0f);
                            strikeOffset = new Vector3(0f, -0.025f, 0.19f);
                        }

                        if (attackAge < attackWindup)
                        {
                            var phase = Mathf.SmoothStep(0f, 1f, attackAge / attackWindup);
                            targetBodyRotation = Quaternion.Slerp(Quaternion.identity, windBody, phase);
                            rightArmRotation = Quaternion.Slerp(Quaternion.Euler(-18f, 0f, 5f), windRightArm, phase);
                            leftArmRotation = Quaternion.Slerp(Quaternion.Euler(-8f, 0f, -5f), windLeftArm, phase);
                            rightForearmRotation = Quaternion.Slerp(Quaternion.Euler(-12f, 0f, 0f),
                                windRightForearm, phase);
                        }
                        else if (attackAge < attackWindup + strikeDuration)
                        {
                            var phase = Mathf.SmoothStep(0f, 1f,
                                (attackAge - attackWindup) / strikeDuration);
                            targetBodyRotation = Quaternion.Slerp(windBody, strikeBody, phase);
                            rightArmRotation = Quaternion.Slerp(windRightArm, strikeRightArm, phase);
                            leftArmRotation = Quaternion.Slerp(windLeftArm, strikeLeftArm, phase);
                            rightForearmRotation = Quaternion.Slerp(windRightForearm, strikeRightForearm, phase);
                            skeletonOffset = Vector3.Lerp(Vector3.zero, strikeOffset, phase);
                        }
                        else
                        {
                            var remainingRecovery = Mathf.Max(0.08f, attackRecovery - strikeDuration);
                            var phase = Mathf.SmoothStep(0f, 1f,
                                Mathf.Clamp01((attackAge - attackWindup - strikeDuration) / remainingRecovery));
                            targetBodyRotation = Quaternion.Slerp(strikeBody, recoverBody, phase);
                            rightArmRotation = Quaternion.Slerp(strikeRightArm, recoverRightArm, phase);
                            leftArmRotation = Quaternion.Slerp(strikeLeftArm, recoverLeftArm, phase);
                            rightForearmRotation = Quaternion.Slerp(strikeRightForearm, recoverRightForearm, phase);
                            skeletonOffset = Vector3.Lerp(strikeOffset, Vector3.zero, phase);
                        }
                        chestRotation = Quaternion.Slerp(Quaternion.identity, targetBodyRotation, 0.55f);
                        leftLegRotation = Quaternion.Euler(attackIndex == 1 ? -17f : 15f, 0f, -5f);
                        rightLegRotation = Quaternion.Euler(attackIndex == 1 ? 16f : -21f, 0f, 4f);
                        break;
                    case ActorState.HitReact:
                        var hitAge = Mathf.Max(0f, Time.time - hitStartedAt);
                        var recoil = Mathf.Clamp01(1f - hitAge / 0.42f);
                        var readableSide = Mathf.Abs(hitSide) < 0.12f
                            ? (((hitVariant & 1) == 0) ? -0.42f : 0.42f)
                            : hitSide;
                        targetBodyRotation = Quaternion.Euler(10f - hitForward * 22f * recoil,
                            readableSide * 27f * recoil, -readableSide * 18f * recoil);
                        chestRotation = Quaternion.Euler(-hitForward * 30f * recoil,
                            readableSide * 34f * recoil, -readableSide * 22f * recoil);
                        leftArmRotation = Quaternion.Euler(35f * recoil, -24f * readableSide, -52f);
                        rightArmRotation = Quaternion.Euler(28f * recoil, 28f * readableSide, 55f);
                        leftForearmRotation = Quaternion.Euler(-34f, 0f, -18f * readableSide);
                        rightForearmRotation = Quaternion.Euler(-42f, 0f, 20f * readableSide);
                        leftLegRotation = Quaternion.Euler(-12f * recoil, 0f, -7f);
                        rightLegRotation = Quaternion.Euler(17f * recoil, 0f, 7f);
                        skeletonOffset = new Vector3(readableSide * 0.055f, 0.012f, hitForward * 0.085f) * recoil;
                        break;
                    case ActorState.Guard:
                    case ActorState.Parry:
                        targetBodyRotation = Quaternion.Euler(-5f, -12f, 0f);
                        leftArmRotation = Quaternion.Euler(-77f, -28f, -38f);
                        rightArmRotation = Quaternion.Euler(-63f, 25f, 28f);
                        leftForearmRotation = Quaternion.Euler(-72f, 0f, 0f);
                        rightForearmRotation = Quaternion.Euler(-64f, 0f, 0f);
                        break;
                    case ActorState.Dodge:
                        targetBodyRotation = Quaternion.Euler(37f, 0f, 0f);
                        leftArmRotation = Quaternion.Euler(31f, 0f, -18f);
                        rightArmRotation = Quaternion.Euler(39f, 0f, 18f);
                        leftLegRotation = Quaternion.Euler(-38f, 0f, 0f);
                        rightLegRotation = Quaternion.Euler(44f, 0f, 0f);
                        break;
                    case ActorState.Knockdown:
                    case ActorState.Dead:
                        targetBodyRotation = Quaternion.Euler(76f, 0f, 18f);
                        leftArmRotation = Quaternion.Euler(52f, 0f, -48f);
                        rightArmRotation = Quaternion.Euler(-28f, 0f, 46f);
                        break;
                    case ActorState.UseItem:
                        rightArmRotation = Quaternion.Euler(-105f, 12f, 20f);
                        rightForearmRotation = Quaternion.Euler(-92f, 0f, 0f);
                        break;
                }
            }

            var blendSpeed = state != null && state.Current == ActorState.HitReact ? 27f : 12f;
            var blend = 1f - Mathf.Exp(-Time.deltaTime * blendSpeed);
            skeleton.localPosition = skeletonOffset + Vector3.up * (breath + lift * 0.0035f);
            pelvis.localRotation = Quaternion.Slerp(pelvis.localRotation, targetBodyRotation, blend);
            chest.localRotation = Quaternion.Slerp(chest.localRotation, chestRotation, blend);
            Pose(leftArm, leftArmRotation, blend);
            Pose(leftForearm, leftForearmRotation, blend);
            Pose(rightArm, rightArmRotation, blend);
            Pose(rightForearm, rightForearmRotation, blend);
            Pose(leftLeg, leftLegRotation, blend);
            Pose(leftShin, leftShinRotation, blend);
            Pose(rightLeg, rightLegRotation, blend);
            Pose(rightShin, rightShinRotation, blend);
            if (weaponTrail != null) weaponTrail.emitting = emitWeaponTrail;
        }

        private static void Pose(Transform bone, Quaternion target, float blend)
        {
            if (bone != null) bone.localRotation = Quaternion.Slerp(bone.localRotation, target, blend);
        }
    }
}
