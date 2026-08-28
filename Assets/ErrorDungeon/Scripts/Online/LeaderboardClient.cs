using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ErrorDungeon
{
    public sealed class LeaderboardClient : MonoBehaviour
    {
        [Serializable]
        private sealed class LeaderboardSubmission
        {
            public string playerName;
            public int maxRegion;
            public string maxBoss;
            public int level;
            public int clearTimeSec;
            public string timestamp;
        }

        [Serializable]
        private sealed class Heartbeat
        {
            public string instanceId;
            public string buildVersion;
            public string timestamp;
        }

        private string ApiBase
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (!string.IsNullOrWhiteSpace(Application.absoluteURL))
                    return new Uri(Application.absoluteURL).GetLeftPart(UriPartial.Authority) + "/api";
                return "/api";
#else
                var configured = Environment.GetEnvironmentVariable("ERROR_DUNGEON_API_URL");
                return string.IsNullOrWhiteSpace(configured) ? "http://error-dungeon.local/api" : configured.TrimEnd('/');
#endif
            }
        }

        public void SubmitBossClear(int level, int clearTimeSeconds)
        {
            var payload = new LeaderboardSubmission
            {
                playerName = "박재영",
                maxRegion = 1,
                maxBoss = "boss_doyun",
                level = level,
                clearTimeSec = clearTimeSeconds,
                timestamp = DateTime.UtcNow.ToString("O")
            };
            StartCoroutine(PostJson("/leaderboard/submit", JsonUtility.ToJson(payload)));
        }

        public void StartServerHeartbeat()
        {
            if (!Application.isBatchMode) return;
            StartCoroutine(HeartbeatLoop());
        }

        private IEnumerator HeartbeatLoop()
        {
            while (true)
            {
                var payload = new Heartbeat
                {
                    instanceId = SystemInfo.deviceUniqueIdentifier,
                    buildVersion = Application.version,
                    timestamp = DateTime.UtcNow.ToString("O")
                };
                yield return PostJson("/events/heartbeat", JsonUtility.ToJson(payload));
                yield return new WaitForSecondsRealtime(30f);
            }
        }

        private IEnumerator PostJson(string path, string json)
        {
            using (var request = new UnityWebRequest(ApiBase + path, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 8;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                    Debug.LogWarning($"Linux server request failed ({request.responseCode}): {request.error}");
            }
        }
    }
}
