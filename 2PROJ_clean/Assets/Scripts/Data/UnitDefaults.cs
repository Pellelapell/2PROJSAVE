using System.Collections.Generic;
using UnityEngine;

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
            [UnitType.Infantry]  = new Def { hp=80,  damage=10, price=50,  attackSpeed=1.00f, range=2.0f, moveSpeed=4.0f, buildTime=4f,  damageType=DamageType.Physical, displayName="Infanterie" },
            [UnitType.Support]   = new Def { hp=60,  damage=6,  price=45,  attackSpeed=0.80f, range=2.0f, moveSpeed=4.0f, buildTime=3f,  damageType=DamageType.Physical, hasActivable=true, spellDuration=5f,  spellCooldown=15f, displayName="Soutien" },
            [UnitType.Heal]      = new Def { hp=55,  damage=0,  price=55,  attackSpeed=0.50f, range=4.5f, moveSpeed=3.5f, buildTime=4f,  damageType=DamageType.Healing,  hasActivable=true, spellDuration=6f,  spellCooldown=12f, displayName="Soigneur" },
            [UnitType.Range]     = new Def { hp=45,  damage=14, price=65,  attackSpeed=0.75f, range=5.5f, moveSpeed=3.5f, buildTime=4f,  damageType=DamageType.Physical, displayName="Archer" },
            [UnitType.Heavy]     = new Def { hp=150, damage=12, price=85,  attackSpeed=0.65f, range=2.0f, moveSpeed=2.0f, buildTime=6f,  damageType=DamageType.Physical, displayName="Lourd" },
            [UnitType.AntiArmor] = new Def { hp=55,  damage=18, price=75,  attackSpeed=0.55f, range=3.5f, moveSpeed=3.0f, buildTime=5f,  damageType=DamageType.Piercing, displayName="Anti-Armure" },
            [UnitType.Mortar]    = new Def { hp=55,  damage=30, price=100, attackSpeed=0.30f, range=8.0f, moveSpeed=2.0f, buildTime=7f,  damageType=DamageType.Siege,    isAOE=true, aoeRadius=2.5f, aoeFalloff=0.5f, displayName="Mortier" },
            [UnitType.Transport] = new Def { hp=100, damage=0,  price=90,  attackSpeed=0f,    range=0f,   moveSpeed=3.5f, buildTime=10f, damageType=DamageType.Physical, displayName="Transport" },
            [UnitType.Frigate]   = new Def { hp=110, damage=18, price=110, attackSpeed=0.80f, range=5.0f, moveSpeed=3.0f, buildTime=9f,  damageType=DamageType.Physical, displayName="Frégate" },
            [UnitType.Destroyer] = new Def { hp=200, damage=28, price=150, attackSpeed=0.65f, range=6.0f, moveSpeed=3.5f, buildTime=14f, damageType=DamageType.Physical, displayName="Destroyer" },
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
            ApplyRaceBonus(stats, type);
        }

        private static void ApplyRaceBonus(UnitStats stats, UnitType type)
        {
            switch (stats.race)
            {
                case Race.Human:
                    stats.maxHealth     = Mathf.RoundToInt(stats.maxHealth * 1.10f);
                    stats.currentHealth = stats.maxHealth;
                    if (type == UnitType.Range || type == UnitType.Heal)
                        stats.attackRange += 0.5f;
                    stats.detectRange = stats.attackRange * 1.5f;
                    break;

                case Race.Elf:
                    stats.moveSpeed   *= 1.20f;
                    stats.attackRange += 0.5f;
                    stats.detectRange  = stats.attackRange * 1.5f;
                    if (type == UnitType.Heal)
                        stats.spellCooldown *= 0.80f;
                    break;

                case Race.Demon:
                    stats.attackDamage  = Mathf.RoundToInt(stats.attackDamage * 1.20f);
                    stats.attackSpeed  *= 1.15f;
                    stats.maxHealth     = Mathf.RoundToInt(stats.maxHealth * 0.90f);
                    stats.currentHealth = stats.maxHealth;
                    if (type == UnitType.Mortar)
                        stats.aoeRadius += 0.5f;
                    break;
            }
        }

        public static int    GetPrice    (UnitType type) => Data.TryGetValue(type, out Def d) ? d.price     : 50;
        public static float  GetBuildTime(UnitType type) => Data.TryGetValue(type, out Def d) ? d.buildTime : 3f;
        public static string GetName(UnitType type)
        {
            string localized = LocalizationManager.Get("unit_" + type.ToString().ToLower());
            if (localized != "unit_" + type.ToString().ToLower()) return localized;
            return Data.TryGetValue(type, out Def d) ? d.displayName : type.ToString();
        }
    }
}
