using UnityEngine;
using SupKonQuest;

public class UnitVisuals : MonoBehaviour
{
    private Renderer   unitRenderer;
    private MeshFilter meshFilter;
    private UnitStats  stats;

    private void Awake()
    {
        stats        = GetComponent<UnitStats>();
        unitRenderer = GetComponentInChildren<Renderer>();
        meshFilter   = GetComponentInChildren<MeshFilter>();

        if (unitRenderer == null) unitRenderer = GetComponent<Renderer>();
        if (meshFilter   == null) meshFilter   = GetComponent<MeshFilter>();
    }

    public void ApplyRaceVisuals()
    {
        if (stats == null) return;

        RaceDefinition def = RaceRegistry.Get(stats.race);
        if (def != null)
        {
            var skin = def.GetUnitSkin(stats.unitType);
            if (skin.HasValue)
            {
                if (meshFilter != null && skin.Value.mesh != null)
                    meshFilter.sharedMesh = skin.Value.mesh;

                if (unitRenderer != null && skin.Value.material != null)
                {
                    unitRenderer.material = skin.Value.material;
                    return;
                }
            }
        }

        // Fallback couleur si aucun skin configuré
        if (unitRenderer == null) return;
        unitRenderer.material.color = stats.race switch
        {
            Race.Human => Color.blue,
            Race.Elf   => Color.green,
            Race.Demon => Color.red,
            _          => Color.white
        };
    }
}
