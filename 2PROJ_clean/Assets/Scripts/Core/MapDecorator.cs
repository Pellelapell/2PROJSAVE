using UnityEngine;
using SupKonQuest;

[DefaultExecutionOrder(10)]
public class MapDecorator : MonoBehaviour
{
    [System.Serializable]
    public class DecorationSet
    {
        public string label;
        public GameObject[] prefabs;

        [Range(0f, 1f)]
        [Tooltip("Probabilité qu'au moins une décoration de ce set apparaisse sur un hex éligible")]
        public float spawnChance = 0.3f;

        [Range(1, 5)]
        [Tooltip("Nombre max de décorations placées par hex quand le set se déclenche")]
        public int maxPerHex = 2;

        [Header("Terrains autorisés")]
        public bool onWalkable = true;
        public bool onMountain = false;
        public bool onWater = false;

        [Header("Variation")]
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    }

    [Header("Source")]
    [Tooltip("Laisser vide pour trouver automatiquement le HexGridGenerator dans la scène")]
    public HexGridGenerator hexGrid;

    [Header("Sets de décoration")]
    public DecorationSet[] decorationSets;

    [Header("Placement")]
    [Tooltip("Distance max du centre de l'hex pour placer une décoration")]
    public float placementRadius = 0.15f;
    [Tooltip("Décalage vertical des décorations (ajuster selon la hauteur du modèle hex)")]
    public float yOffset = 0f;

    void Start()
    {
        if (hexGrid == null)
            hexGrid = FindObjectOfType<HexGridGenerator>();

        if (hexGrid == null)
        {
            Debug.LogWarning("MapDecorator : aucun HexGridGenerator trouvé dans la scène.");
            return;
        }

        if (decorationSets == null || decorationSets.Length == 0)
            return;

        HexTile[] tiles = hexGrid.GetComponentsInChildren<HexTile>();
        foreach (HexTile tile in tiles)
            DecorateHex(tile);
    }

    void DecorateHex(HexTile tile)
    {
        foreach (DecorationSet set in decorationSets)
        {
            if (set.prefabs == null || set.prefabs.Length == 0) continue;
            if (!IsTerrainAllowed(set, tile.terrain)) continue;
            if (Random.value > set.spawnChance) continue;

            int count = Random.Range(1, set.maxPerHex + 1);
            for (int i = 0; i < count; i++)
                PlaceDecoration(set, tile);
        }
    }

    bool IsTerrainAllowed(DecorationSet set, HexTerrain terrain)
    {
        switch (terrain)
        {
            case HexTerrain.Walkable:  return set.onWalkable;
            case HexTerrain.Mountain:  return set.onMountain;
            case HexTerrain.Water:     return set.onWater;
            default:                   return false;
        }
    }

    void PlaceDecoration(DecorationSet set, HexTile tile)
    {
        Renderer rend = tile.GetComponentInChildren<Renderer>();
        if (rend == null) return;

        Bounds b = rend.bounds;
        Vector2 offset2D = Random.insideUnitCircle * placementRadius;
        Vector3 pos = new Vector3(b.center.x + offset2D.x, b.max.y + yOffset, b.center.z + offset2D.y);

        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        float scale = Random.Range(set.scaleRange.x, set.scaleRange.y);

        GameObject prefab = set.prefabs[Random.Range(0, set.prefabs.Length)];
        GameObject deco = Instantiate(prefab, pos, rot, tile.transform);
        deco.transform.localScale = Vector3.one * scale;
    }
}
