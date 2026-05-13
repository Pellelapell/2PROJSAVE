using UnityEngine;
using SupKonQuest;

public class UnitVisuals : MonoBehaviour
{
    [Header("Visual Components")]
    private Renderer unitRenderer;
    private UnitStats stats;


    private void Awake()
    {
        stats = GetComponent<UnitStats>();
        unitRenderer = GetComponent<Renderer>();
    }


    public void ApplyRaceVisuals()
    {
        if (stats == null || unitRenderer == null) return;
        switch (stats.race)
        {
            case Race.Human:
                unitRenderer.material.color = Color.blue;
                break;
            case Race.Elf:
                unitRenderer.material.color = Color.green;
                break;
            case Race.Demon:
                unitRenderer.material.color = Color.red;
                break;
        }
    }
}