using System.Collections.Generic;

namespace SupKonQuest
{
    public static class UnitDefaults
    {
        public struct Def
        {
            public int hp, damage, price;
            public float attackSpeed, range, moveSpeed, buildTime;
            public DamageType damageType;
            public bool isAOE;
            public float aoeRadius, aoeFalloff;
            public bool hasActivable;
            public float spellDuration, spellCooldown;
            public string displayName;
        }

        private static readonly Dictionary<UnitType, Def> Data = new Dictionary<UnitType, Def>
        {
            [UnitType.Infantry]  = new Def { hp=60,  damage=8,  price=50,  attackSpeed=1f,    range=1.5f, moveSpeed=4f,   buildTime=3f,  damageType=DamageType.Physical, displayName="Infanterie" },
            [UnitType.Support]   = new Def { hp=50,  damage=5,  price=40,  attackSpeed=1f,    range=1.5f, moveSpeed=4f,   buildTime=2f,  damageType=DamageType.Physical, hasActivable=true, spellDuration=5f,  spellCooldown=10f, displayName="Soutien" },
            [UnitType.Heal]      = new Def { hp=50,  damage=0,  price=60,  attackSpeed=0.5f,  range=4f,   moveSpeed=4f,   buildTime=3f,  damageType=DamageType.Healing,  hasActivable=true, spellDuration=5f,  spellCooldown=12f, displayName="Soigneur" },
            [UnitType.Range]     = new Def { hp=40,  damage=12, price=60,  attackSpeed=0.8f,  range=5f,   moveSpeed=3.5f, buildTime=3f,  damageType=DamageType.Physical, displayName="Archer" },
            [UnitType.Heavy]     = new Def { hp=120, damage=10, price=80,  attackSpeed=0.7f,  range=1.5f, moveSpeed=2.5f, buildTime=5f,  damageType=DamageType.Physical, displayName="Lourd" },
            [UnitType.AntiArmor] = new Def { hp=50,  damage=15, price=70,  attackSpeed=0.6f,  range=3f,   moveSpeed=3.5f, buildTime=4f,  damageType=DamageType.Piercing, displayName="Anti-Armure" },
            [UnitType.Mortar]    = new Def { hp=60,  damage=35, price=90,  attackSpeed=0.35f, range=7f,   moveSpeed=2.5f, buildTime=6f,  damageType=DamageType.Siege,    isAOE=true, aoeRadius=2.5f, aoeFalloff=0.6f, displayName="Mortier" },
            [UnitType.Transport] = new Def { hp=80,  damage=0,  price=80,  attackSpeed=0f,    range=0f,   moveSpeed=4f,   buildTime=8f,  damageType=DamageType.Physical, displayName="Transport" },
            [UnitType.Frigate]   = new Def { hp=100, damage=15, price=100, attackSpeed=0.9f,  range=4f,   moveSpeed=3f,   buildTime=8f,  damageType=DamageType.Physical, displayName="Frégate" },
            [UnitType.Destroyer] = new Def { hp=150, damage=25, price=130, attackSpeed=0.7f,  range=5f,   moveSpeed=3.5f, buildTime=12f, damageType=DamageType.Physical, displayName="Destroyer" },
        };

        public static void Apply(UnitStats stats, UnitType type)
        {
            if (!Data.TryGetValue(type, out Def d)) return;
            stats.unitType              = type;
            stats.maxHealth             = d.hp;
            stats.currentHealth         = d.hp;
            stats.attackDamage          = d.damage;
            stats.attackSpeed           = d.attackSpeed;
            stats.attackRange           = d.range;
            stats.detectRange           = d.range * 1.5f;
            stats.moveSpeed             = d.moveSpeed;
            stats.damageType            = d.damageType;
            stats.isAOE                 = d.isAOE;
            stats.aoeRadius             = d.aoeRadius;
            stats.aoeFalloff            = d.aoeFalloff;
            stats.hasActivable          = d.hasActivable;
            stats.spellDuration         = d.spellDuration;
            stats.spellCooldown         = d.spellCooldown;
            stats.price                 = d.price;
            stats.attackSpeedMultiplier = 1f;
        }

        public static int    GetPrice    (UnitType type) => Data.TryGetValue(type, out Def d) ? d.price     : 50;
        public static float  GetBuildTime(UnitType type) => Data.TryGetValue(type, out Def d) ? d.buildTime : 3f;
        public static string GetName     (UnitType type) => Data.TryGetValue(type, out Def d) ? d.displayName : type.ToString();
    }
}
