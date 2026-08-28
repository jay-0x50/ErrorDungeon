using System;
using UnityEngine;

namespace ErrorDungeon
{
    [RequireComponent(typeof(CombatVitals))]
    public sealed class PlayerProgression : MonoBehaviour
    {
        [SerializeField] private int level = 1;
        [SerializeField] private int vitality = 8;
        [SerializeField] private int endurance = 8;
        [SerializeField] private int strength = 10;
        [SerializeField] private int dexterity = 8;
        [SerializeField] private float equippedWeight = 18f;
        [SerializeField] private float baseMaxWeight = 40f;
        [SerializeField] private string weaponId = "wp_rusted_greatsword";
        [SerializeField] private int weaponLevel;

        public int Level => level;
        public int Souls { get; private set; }
        public int Vitality => vitality;
        public int Endurance => endurance;
        public int Strength => strength;
        public int Dexterity => dexterity;
        public string WeaponId => weaponId;
        public int WeaponLevel => weaponLevel;
        public int Flasks { get; private set; } = 3;
        public int IronShards { get; private set; }
        public float MaxWeight => baseMaxWeight + endurance;
        public float LoadRatio => MaxWeight <= 0f ? 1f : equippedWeight / MaxWeight;
        public EquipmentLoad LoadClass => LoadRatio <= 0.3f ? EquipmentLoad.Light : LoadRatio <= 0.7f ? EquipmentLoad.Medium : EquipmentLoad.Heavy;
        public int LevelUpCost => Mathf.RoundToInt(120f * Mathf.Pow(level + 1, 1.35f));
        public event Action Changed;

        private void Start() => ApplyStats();

        public float WeaponAttack
        {
            get
            {
                var weapon = GameDataRepository.Weapon(weaponId);
                if (weapon == null) return 50f;
                var scaling = strength * weapon.scaling.str + dexterity * weapon.scaling.dex;
                var upgrade = GameDataRepository.Upgrade(weaponId);
                var levelData = upgrade?.levels == null ? null : Array.Find(upgrade.levels, value => value.level == weaponLevel);
                return (weapon.baseAtk + scaling) * (levelData?.atkMul ?? 1f);
            }
        }

        public void AddSouls(int amount)
        {
            Souls = Mathf.Max(0, Souls + Mathf.Max(0, amount));
            Changed?.Invoke();
        }

        public int RemoveAllSouls()
        {
            var dropped = Souls;
            Souls = 0;
            Changed?.Invoke();
            return dropped;
        }

        public bool TryLevelUp()
        {
            if (Souls < LevelUpCost || GameSession.Instance == null || !GameSession.Instance.IsPlayerAtCheckpoint) return false;
            Souls -= LevelUpCost;
            level++;
            if (level % 2 == 0) vitality++; else endurance++;
            if (level % 3 == 0) strength++;
            ApplyStats();
            Changed?.Invoke();
            GameSession.Instance.Save();
            return true;
        }

        public void AddIronShards(int amount)
        {
            IronShards = Mathf.Max(0, IronShards + Mathf.Max(0, amount));
            Changed?.Invoke();
        }

        public bool TryUpgradeWeapon()
        {
            if (GameSession.Instance == null || !GameSession.Instance.IsPlayerAtCheckpoint) return false;
            var upgrade = GameDataRepository.Upgrade(weaponId);
            if (upgrade == null || weaponLevel >= upgrade.maxLevel) return false;
            var next = Array.Find(upgrade.levels, value => value.level == weaponLevel + 1);
            if (next == null) return false;
            var shardCost = 0;
            if (next.materials != null)
                foreach (var material in next.materials)
                    if (material.itemId == "mat_iron_shard") shardCost += material.count;
            if (Souls < next.costSouls || IronShards < shardCost) return false;
            Souls -= next.costSouls;
            IronShards -= shardCost;
            weaponLevel = next.level;
            Changed?.Invoke();
            GameSession.Instance.Save();
            return true;
        }

        public bool ConsumeFlask()
        {
            if (Flasks <= 0) return false;
            Flasks--;
            Changed?.Invoke();
            return true;
        }

        public void RefillFlasks()
        {
            Flasks = GameDataRepository.Item("it_heal_flask")?.refill.refillTo ?? 3;
            Changed?.Invoke();
        }

        public void EquipBossSpear()
        {
            weaponId = "wp_spear_doyun";
            equippedWeight = 13.5f;
            Changed?.Invoke();
        }

        public PlayerSaveData Capture(Vector3 checkpoint)
        {
            return new PlayerSaveData
            {
                level = level, vitality = vitality, endurance = endurance, strength = strength,
                dexterity = dexterity, souls = Souls, weaponId = weaponId, weaponLevel = weaponLevel,
                flaskCount = Flasks, ironShards = IronShards, checkpoint = new SerializableVector3(checkpoint)
            };
        }

        public void Restore(PlayerSaveData data)
        {
            if (data == null) return;
            level = Mathf.Max(1, data.level);
            vitality = Mathf.Max(0, data.vitality);
            endurance = Mathf.Max(0, data.endurance);
            strength = Mathf.Max(0, data.strength);
            dexterity = Mathf.Max(0, data.dexterity);
            Souls = Mathf.Max(0, data.souls);
            weaponId = string.IsNullOrEmpty(data.weaponId) ? "wp_rusted_greatsword" : data.weaponId;
            weaponLevel = Mathf.Clamp(data.weaponLevel, 0, 10);
            Flasks = Mathf.Clamp(data.flaskCount, 0, 10);
            IronShards = Mathf.Max(0, data.ironShards);
            ApplyStats();
            Changed?.Invoke();
        }

        private void ApplyStats() => GetComponent<CombatVitals>().SetAttributes(vitality, endurance);
    }
}
