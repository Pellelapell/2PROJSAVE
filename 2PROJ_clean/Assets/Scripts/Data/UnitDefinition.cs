using UnityEngine;
using SupKonQuest;

[CreateAssetMenu(menuName = "SupKonQuest/Data/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    [Header("Identity")]
    public UnitType unitType;
    public string displayName;

    [Header("Economy")]
    public int price = 50;
    public float buildTime = 2f;

    [Header("Stats")]
    public int maxHP = 100;
    public int damage = 10;

    [Tooltip("Attacks per second")]
    public float attackSpeed = 1f;

    public float range = 1.5f;
    public float moveSpeed = 3.5f;

    [Header("Damage Type")]
    public DamageType damageType = DamageType.Physical;

    [Header("AOE (Mortar)")]
    public bool isAOE = false;
    public float aoeRadius = 2.5f;
    [Range(0f, 1f)] public float aoeFalloff = 0.6f;

    [Header("Activable (Support/Heal)")]
    public bool hasActivable = false;
    public float spellDuration = 5f;
    public float spellCooldown = 10f;
}
