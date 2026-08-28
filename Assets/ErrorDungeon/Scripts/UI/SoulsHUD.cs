using UnityEngine;

namespace ErrorDungeon
{
    public sealed class SoulsHUD : MonoBehaviour
    {
        private CombatVitals playerVitals;
        private PlayerProgression progression;
        private PlayerInteractor interactor;
        private GUIStyle titleStyle;
        private GUIStyle textStyle;
        private GUIStyle centerStyle;
        private Texture2D white;

        public void Bind(GameObject player)
        {
            playerVitals = player.GetComponent<CombatVitals>();
            progression = player.GetComponent<PlayerProgression>();
            interactor = player.GetComponent<PlayerInteractor>();
        }

        private void EnsureStyles()
        {
            if (white != null) return;
            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } };
            centerStyle = new GUIStyle(titleStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 28 };
        }

        private void OnGUI()
        {
            if (playerVitals == null || progression == null) return;
            EnsureStyles();
            DrawBar(new Rect(38, 34, 330, 18), playerVitals.Health01, new Color(0.62f, 0.08f, 0.06f), Color.black);
            DrawBar(new Rect(38, 57, 270, 13), playerVitals.Stamina01, new Color(0.14f, 0.55f, 0.2f), Color.black);
            GUI.Label(new Rect(38, 76, 520, 28), $"Lv.{progression.Level}   혼 {progression.Souls:N0}   철편 {progression.IronShards}   물약 {progression.Flasks}", textStyle);
            GUI.Label(new Rect(Screen.width - 420, 32, 390, 30), $"{GameDataRepository.Weapon(progression.WeaponId)?.name ?? progression.WeaponId} +{progression.WeaponLevel}", titleStyle);

            var prompt = interactor.CurrentPrompt;
            if (!string.IsNullOrEmpty(prompt)) GUI.Label(new Rect(Screen.width / 2f - 250, Screen.height - 115, 500, 38), prompt, centerStyle);
            if (GameSession.Instance != null && GameSession.Instance.IsPlayerAtCheckpoint)
                GUI.Label(new Rect(Screen.width / 2f - 360, Screen.height - 76, 720, 28), $"[L] 레벨 올리기 (혼 {progression.LevelUpCost:N0})   [U] 무기 강화", centerStyle);

            var boss = GameSession.Instance?.ActiveBoss;
            if (boss != null && boss.Vitals != null)
            {
                GUI.Label(new Rect(Screen.width / 2f - 300, Screen.height - 165, 600, 32), "무너진 장군, 도윤", centerStyle);
                DrawBar(new Rect(Screen.width / 2f - 310, Screen.height - 132, 620, 16), boss.Vitals.Health01,
                    new Color(0.55f, 0.08f, 0.05f), new Color(0.08f, 0.06f, 0.05f));
            }

            if (GameSession.Instance != null && Time.time < GameSession.Instance.NarrativeUntil)
            {
                GUI.Box(new Rect(Screen.width / 2f - 360, Screen.height * 0.22f, 720, 90), GUIContent.none);
                GUI.Label(new Rect(Screen.width / 2f - 350, Screen.height * 0.22f + 8, 700, 74), GameSession.Instance.NarrativeText, centerStyle);
            }

            GUI.Label(new Rect(28, Screen.height - 42, 900, 30), "WASD 이동  Shift 달리기/강공격  LMB 공격  RMB 가드  Space 회피  F 패링  Q 락온  R 물약", textStyle);
        }

        private void DrawBar(Rect rect, float fill, Color foreground, Color background)
        {
            var old = GUI.color;
            GUI.color = background;
            GUI.DrawTexture(rect, white);
            GUI.color = foreground;
            GUI.DrawTexture(new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * Mathf.Clamp01(fill), rect.height - 4), white);
            GUI.color = old;
        }
    }
}
