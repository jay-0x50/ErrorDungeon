using UnityEngine;

namespace ErrorDungeon
{
    public sealed class SoulsHUD : MonoBehaviour
    {
        private CombatVitals playerVitals;
        private PlayerProgression progression;
        private PlayerInteractor interactor;
        private PlayerCombat combat;
        private GUIStyle small;
        private GUIStyle body;
        private GUIStyle heading;
        private GUIStyle center;
        private GUIStyle region;
        private Texture2D white;
        private bool showHelp = true;

        private static readonly Color Ink = new Color(0.018f, 0.022f, 0.028f, 0.88f);
        private static readonly Color Iron = new Color(0.31f, 0.34f, 0.38f, 0.92f);
        private static readonly Color Parchment = new Color(0.78f, 0.76f, 0.68f, 1f);
        private static readonly Color Cyan = new Color(0.12f, 0.74f, 0.88f, 1f);
        private static readonly Color Magenta = new Color(0.9f, 0.09f, 0.48f, 1f);

        public void Bind(GameObject player)
        {
            playerVitals = player.GetComponent<CombatVitals>();
            progression = player.GetComponent<PlayerProgression>();
            interactor = player.GetComponent<PlayerInteractor>();
            combat = player.GetComponent<PlayerCombat>();
        }

        private void Update()
        {
            if (!Application.isBatchMode && Input.GetKeyDown(KeyCode.F1)) showHelp = !showHelp;
        }

        private void EnsureStyles()
        {
            if (white != null) return;
            white = new Texture2D(1, 1) { name = "HUD white pixel" };
            white.SetPixel(0, 0, Color.white);
            white.Apply();
            small = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13, normal = { textColor = new Color(0.72f, 0.73f, 0.72f) },
                alignment = TextAnchor.MiddleLeft
            };
            body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16, normal = { textColor = Parchment }, richText = true,
                wordWrap = true
            };
            heading = new GUIStyle(body)
            {
                fontSize = 21, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.9f, 0.88f, 0.8f) }
            };
            center = new GUIStyle(heading) { alignment = TextAnchor.MiddleCenter, fontSize = 23 };
            region = new GUIStyle(center)
            {
                fontSize = 14, fontStyle = FontStyle.Normal, normal = { textColor = new Color(0.64f, 0.67f, 0.72f, 0.94f) }
            };
        }

        private void OnGUI()
        {
            if (playerVitals == null || progression == null) return;
            EnsureStyles();

            var session = GameSession.Instance;
            if (session != null && session.CinematicActive)
            {
                DrawCinematicFrame(session);
                DrawNarrative();
                return;
            }

            DrawVitals();
            DrawWorldObjective();
            DrawInteraction();
            DrawBoss();
            DrawCombo();
            DrawNarrative();
            DrawHelp();
        }

        private void DrawCinematicFrame(GameSession session)
        {
            var barHeight = Mathf.Max(72f, Screen.height * 0.13f);
            Fill(new Rect(0f, 0f, Screen.width, barHeight), Color.black);
            Fill(new Rect(0f, Screen.height - barHeight, Screen.width, barHeight), Color.black);
            if (!string.IsNullOrEmpty(session.CinematicChapter))
                GUI.Label(new Rect(40f, 20f, Screen.width - 80f, 34f), session.CinematicChapter, region);
        }

        private void DrawCombo()
        {
            if (combat == null || combat.ActiveComboStep < 0) return;
            var marks = combat.ActiveComboStep == 0 ? "I" : combat.ActiveComboStep == 1 ? "II" : "III";
            var rect = new Rect(Screen.width * 0.5f - 90f, Screen.height * 0.69f, 180f, 32f);
            GUI.Label(rect, $"연격  {marks}", region);
        }

        private void DrawVitals()
        {
            DrawPanel(new Rect(24, 22, 390, 91), 0.82f);
            DrawBar(new Rect(42, 40, 335, 17), playerVitals.Health01,
                new Color(0.62f, 0.075f, 0.055f), new Color(0.07f, 0.025f, 0.022f), new Color(0.8f, 0.24f, 0.18f));
            DrawBar(new Rect(42, 63, 276, 11), playerVitals.Stamina01,
                new Color(0.25f, 0.49f, 0.19f), new Color(0.025f, 0.065f, 0.035f), new Color(0.48f, 0.72f, 0.3f));
            GUI.Label(new Rect(42, 80, 355, 25),
                $"LV {progression.Level}    혼  {progression.Souls:N0}    철편  {progression.IronShards}    성배  {progression.Flasks}", small);

            var weaponName = GameDataRepository.Weapon(progression.WeaponId)?.name ?? progression.WeaponId;
            var weaponRect = new Rect(Screen.width - 330, 24, 306, 48);
            DrawPanel(weaponRect, 0.72f);
            GUI.Label(new Rect(weaponRect.x + 16, weaponRect.y + 8, weaponRect.width - 32, 31),
                $"{weaponName}  +{progression.WeaponLevel}", heading);
        }

        private void DrawWorldObjective()
        {
            var story = WorldStory.Instance;
            if (story == null) return;
            GUI.Label(new Rect(Screen.width * 0.5f - 210f, 20f, 420f, 24f), $"—  {story.RegionName}  —", region);
            var rect = new Rect(Screen.width - 388, 84, 364, 72);
            DrawPanel(rect, 0.68f);
            GUI.Label(new Rect(rect.x + 15, rect.y + 8, rect.width - 30, 18), "현재의 맹세", small);
            GUI.Label(new Rect(rect.x + 15, rect.y + 27, rect.width - 30, 38), story.Objective, body);
            if (story.SealCount > 0)
            {
                var width = (rect.width - 30f) / story.SealTarget;
                for (var i = 0; i < story.SealTarget; i++)
                {
                    var color = i < story.SealCount ? (i == 1 ? Magenta : Cyan) : new Color(0.18f, 0.19f, 0.21f, 0.8f);
                    Fill(new Rect(rect.x + 15 + i * width, rect.y + rect.height - 4f, width - 3f, 2f), color);
                }
            }
        }

        private void DrawInteraction()
        {
            var prompt = interactor.CurrentPrompt;
            if (!string.IsNullOrEmpty(prompt))
            {
                var rect = new Rect(Screen.width * 0.5f - 260f, Screen.height - 134f, 520f, 45f);
                DrawPanel(rect, 0.76f);
                GUI.Label(rect, prompt, center);
            }
            if (GameSession.Instance != null && GameSession.Instance.IsPlayerAtCheckpoint)
                GUI.Label(new Rect(Screen.width * 0.5f - 380f, Screen.height - 87f, 760f, 27f),
                    $"[L] 능력 각인  혼 {progression.LevelUpCost:N0}     [U] 무기 강화", region);
        }

        private void DrawBoss()
        {
            var boss = GameSession.Instance?.ActiveBoss;
            if (boss == null || boss.Vitals == null) return;
            var width = Mathf.Min(720f, Screen.width - 140f);
            var x = (Screen.width - width) * 0.5f;
            GUI.Label(new Rect(x, Screen.height - 202f, width, 34f), "무너진 장군, 도윤", center);
            DrawBar(new Rect(x, Screen.height - 165f, width, 15f), boss.Vitals.Health01,
                new Color(0.49f, 0.045f, 0.04f), new Color(0.055f, 0.025f, 0.026f),
                boss.IsPhaseTwo ? Cyan : new Color(0.76f, 0.18f, 0.12f));
        }

        private void DrawNarrative()
        {
            var session = GameSession.Instance;
            if (session == null || Time.time >= session.NarrativeUntil || string.IsNullOrEmpty(session.NarrativeText)) return;
            var rect = new Rect(Screen.width * 0.5f - 390f, Screen.height * 0.18f, 780f, 126f);
            Fill(new Rect(0f, rect.y - 20f, Screen.width, rect.height + 40f), new Color(0f, 0f, 0f, 0.34f));
            DrawPanel(rect, 0.88f, true);
            GUI.Label(new Rect(rect.x + 30f, rect.y + 12f, rect.width - 60f, rect.height - 24f), session.NarrativeText, center);
        }

        private void DrawHelp()
        {
            if (!showHelp)
            {
                GUI.Label(new Rect(26, Screen.height - 34, 260, 22), "F1  조작 도움말", small);
                return;
            }
            var rect = new Rect(24, Screen.height - 58, Mathf.Min(970f, Screen.width - 48f), 34f);
            DrawPanel(rect, 0.58f);
            GUI.Label(new Rect(rect.x + 12, rect.y + 4, rect.width - 24, rect.height - 8),
                "WASD 이동   SHIFT 달리기/강공격   LMB 공격   RMB 가드   SPACE 회피   F 패링   Q 주시   R 성배   E 상호작용   F1 숨기기", small);
        }

        private void DrawPanel(Rect rect, float alpha, bool errorEdge = false)
        {
            Fill(rect, new Color(Ink.r, Ink.g, Ink.b, alpha));
            Fill(new Rect(rect.x, rect.y, rect.width, 1f), errorEdge ? Cyan : Iron);
            Fill(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), errorEdge ? Magenta : new Color(Iron.r, Iron.g, Iron.b, 0.45f));
            Fill(new Rect(rect.x, rect.y, 1f, rect.height), new Color(Iron.r, Iron.g, Iron.b, 0.62f));
            Fill(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), new Color(Iron.r, Iron.g, Iron.b, 0.62f));
        }

        private void DrawBar(Rect rect, float fill, Color foreground, Color background, Color edge)
        {
            Fill(rect, new Color(0.01f, 0.012f, 0.015f, 0.95f));
            Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), background);
            var width = (rect.width - 4) * Mathf.Clamp01(fill);
            Fill(new Rect(rect.x + 2, rect.y + 2, width, rect.height - 4), foreground);
            if (width > 1f) Fill(new Rect(rect.x + 2, rect.y + 2, width, 1f), edge);
        }

        private void Fill(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, white);
            GUI.color = old;
        }
    }
}
