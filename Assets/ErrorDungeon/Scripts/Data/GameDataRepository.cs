using System;
using System.Collections.Generic;
using UnityEngine;

namespace ErrorDungeon
{
    public static class GameDataRepository
    {
        private static Dictionary<string, EnemyData> enemies;
        private static Dictionary<string, WeaponData> weapons;
        private static Dictionary<string, ItemData> items;
        private static Dictionary<string, UpgradeData> upgrades;

        public static EnemyData Enemy(string id)
        {
            EnsureLoaded();
            return enemies.TryGetValue(id, out var value) ? value : null;
        }

        public static WeaponData Weapon(string id)
        {
            EnsureLoaded();
            return weapons.TryGetValue(id, out var value) ? value : null;
        }

        public static ItemData Item(string id)
        {
            EnsureLoaded();
            return items.TryGetValue(id, out var value) ? value : null;
        }

        public static UpgradeData Upgrade(string weaponId)
        {
            EnsureLoaded();
            return upgrades.TryGetValue(weaponId, out var value) ? value : null;
        }

        private static void EnsureLoaded()
        {
            if (enemies != null) return;
            enemies = LoadMap<EnemyDataCollection, EnemyData>("Data/enemy_types", x => x.enemies, x => x.id);
            weapons = LoadMap<WeaponDataCollection, WeaponData>("Data/weapons", x => x.weapons, x => x.id);
            items = LoadMap<ItemDataCollection, ItemData>("Data/items", x => x.items, x => x.id);
            upgrades = LoadMap<UpgradeDataCollection, UpgradeData>("Data/upgrades", x => x.upgrades, x => x.weaponId);
        }

        private static Dictionary<string, TValue> LoadMap<TCollection, TValue>(string resourcePath,
            Func<TCollection, TValue[]> selector, Func<TValue, string> keySelector)
        {
            var result = new Dictionary<string, TValue>(StringComparer.Ordinal);
            var text = Resources.Load<TextAsset>(resourcePath);
            if (text == null)
            {
                Debug.LogError($"Missing game data: Resources/{resourcePath}.json");
                return result;
            }

            var collection = JsonUtility.FromJson<TCollection>(text.text);
            var values = selector(collection);
            if (values == null) return result;
            foreach (var value in values) result[keySelector(value)] = value;
            return result;
        }
    }
}
