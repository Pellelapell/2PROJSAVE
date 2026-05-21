using UnityEngine;
using SupKonQuest;

public class UnitVisuals : MonoBehaviour
{
    public Color aiColor = new Color(1f, 0.45f, 0f); // orange

    private Renderer[] unitRenderers;
    private UnitStats stats;

    private void Awake()
    {
        stats = GetComponent<UnitStats>();
        unitRenderers = GetComponentsInChildren<Renderer>();
    }

    public void ApplyRaceVisuals()
    {
        if (stats == null || unitRenderers == null) return;

        PlayerData owner = GameManager.Instance?.GetPlayerById(stats.ownerId);

        Color color;
        if (owner != null && !owner.isAI)
        {
            color = stats.race switch
            {
                Race.Human => Color.blue,
                Race.Elf   => Color.green,
                Race.Demon => Color.red,
                _          => Color.white
            };
        }
        else
        {
            color = aiColor;
        }

        foreach (Renderer rend in unitRenderers)
            rend.material.color = color;
    }
}