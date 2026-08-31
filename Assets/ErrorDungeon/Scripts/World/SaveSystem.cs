using System;
using System.IO;
using UnityEngine;

namespace ErrorDungeon
{
    [Serializable]
    public struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;
        public SerializableVector3(Vector3 value) { x = value.x; y = value.y; z = value.z; }
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [Serializable]
    public sealed class PlayerSaveData
    {
        public int level;
        public int vitality;
        public int endurance;
        public int strength;
        public int dexterity;
        public int souls;
        public string weaponId;
        public int weaponLevel;
        public int flaskCount;
        public int ironShards;
        public SerializableVector3 checkpoint;
    }

    [Serializable]
    public sealed class GameSaveData
    {
        public int version = 2;
        public PlayerSaveData player;
        public bool doyunDefeated;
        public string[] discoveredRelics;
        public bool memoryGateOpen;
        public float totalPlayTime;
        public string savedAtUtc;
    }

    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "error-dungeon-save.json");

        public static void Save(GameSaveData data)
        {
            data.savedAtUtc = DateTime.UtcNow.ToString("O");
            var temp = SavePath + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data, true));
            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(temp, SavePath);
        }

        public static GameSaveData Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                return JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SavePath));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Save load failed: {exception.Message}");
                return null;
            }
        }
    }
}
