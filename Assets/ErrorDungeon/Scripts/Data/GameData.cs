using System;

namespace ErrorDungeon
{
    [Serializable]
    public sealed class EnemyDataCollection { public EnemyData[] enemies; }

    [Serializable]
    public sealed class EnemyData
    {
        public string id;
        public string name;
        public string tier;
        public EnemyBaseStats @base;
        public EnemyScaling scaling;
        public EnemyResistance resist;
        public DropData[] drops;
        public string aiProfile;
    }

    [Serializable]
    public sealed class EnemyBaseStats
    {
        public float hp;
        public float attack;
        public float defense;
        public float poise;
        public float stamina;
    }

    [Serializable]
    public sealed class EnemyScaling
    {
        public int regionLevel;
        public float hpMulPerLevel;
        public float atkMulPerLevel;
    }

    [Serializable]
    public sealed class EnemyResistance
    {
        public float bleed;
        public float poison;
        public float fire;
        public float lightning;
    }

    [Serializable]
    public sealed class DropData
    {
        public string itemId;
        public float chance;
        public int min;
        public int max;
    }

    [Serializable]
    public sealed class WeaponDataCollection { public WeaponData[] weapons; }

    [Serializable]
    public sealed class WeaponData
    {
        public string id;
        public string name;
        public string type;
        public string rarity;
        public float weight;
        public float baseAtk;
        public GuardData guard;
        public ScalingData scaling;
        public string moveset;
        public SkillData skill;
        public string loreText;
    }

    [Serializable]
    public sealed class GuardData { public float damageCut; public float staminaMul; }

    [Serializable]
    public sealed class ScalingData { public float str; public float dex; }

    [Serializable]
    public sealed class SkillData { public string id; public float cost; }

    [Serializable]
    public sealed class ItemDataCollection { public ItemData[] items; }

    [Serializable]
    public sealed class ItemData
    {
        public string id;
        public string name;
        public string type;
        public int stackMax;
        public ItemUseData use;
        public RefillData refill;
        public string loreText;
    }

    [Serializable]
    public sealed class ItemUseData { public float healHpFlat; public float healHpPct; public float castTimeSec; }

    [Serializable]
    public sealed class RefillData { public bool atCheckpoint; public int refillTo; }

    [Serializable]
    public sealed class UpgradeDataCollection { public UpgradeData[] upgrades; }

    [Serializable]
    public sealed class UpgradeData
    {
        public string weaponId;
        public int maxLevel;
        public UpgradeLevelData[] levels;
    }

    [Serializable]
    public sealed class UpgradeLevelData
    {
        public int level;
        public int costSouls;
        public UpgradeMaterialData[] materials;
        public float atkMul;
    }

    [Serializable]
    public sealed class UpgradeMaterialData { public string itemId; public int count; }

    [Serializable]
    public sealed class CutsceneDataCollection { public CutsceneData[] cutscenes; }

    [Serializable]
    public sealed class CutsceneData
    {
        public string id;
        public string triggerType;
        public string bossId;
        public float hpPercent;
        public bool skippable;
        public float durationSec;
        public string cameraTrack;
        public string dialogueId;
    }
}
