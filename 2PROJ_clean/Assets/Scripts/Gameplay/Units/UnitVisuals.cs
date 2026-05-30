using UnityEngine;
using SupKonQuest;

// instancie le bon modele 3D selon la race de l'unite au moment du spawn
[RequireComponent(typeof(UnitStats))]
public class UnitVisuals : MonoBehaviour
{
    private UnitStats stats;

    private void Awake()
    {
        stats = GetComponent<UnitStats>();
    }

    // appele par CampProduction apres le spawn
    public void ApplyRaceVisuals()
    {
        if (stats == null) return;

        RaceDefinition def = RaceRegistry.Get(stats.race);
        if (def == null) return;

        var skin = def.GetUnitSkin(stats.unitType);
        if (!skin.HasValue)
        {
            Debug.LogWarning($"[UnitVisuals] Pas de skin pour {stats.unitType} race {stats.race}");
            return;
        }
        if (skin.Value.modelPrefab == null)
        {
            Debug.LogWarning($"[UnitVisuals] modelPrefab est null pour {stats.unitType} race {stats.race}");
            return;
        }

        Debug.Log($"[UnitVisuals] Instancie {skin.Value.modelPrefab.name} pour {stats.unitType} race {stats.race}");
        Animator checkAnim = skin.Value.modelPrefab.GetComponentInChildren<Animator>();
        Debug.Log($"[UnitVisuals] Animator dans le prefab : {(checkAnim != null ? checkAnim.name : "NULL - ANIMATOR MANQUANT")}");
        if (checkAnim != null)
            Debug.Log($"[UnitVisuals] Controller : {(checkAnim.runtimeAnimatorController != null ? checkAnim.runtimeAnimatorController.name : "NULL - CONTROLLER MANQUANT")}");


        // on cache le mesh placeholder du prefab (cube/capsule par defaut)
        Renderer placeholder = GetComponent<Renderer>();
        if (placeholder != null) placeholder.enabled = false;

        // on instancie le modele en enfant de l'unite et on recentre sa position
        GameObject model = Instantiate(skin.Value.modelPrefab, transform, false);
        model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        model.transform.localScale = Vector3.one;
    }
}
