using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using SupKonQuest;

[DefaultExecutionOrder(-10)]
public class HexGridGenerator : MonoBehaviour
{
    [Header("Terrain Prefabs")]
    public GameObject[] walkablePrefabs;
    public GameObject[] mountainPrefabs;
    public GameObject[] waterPrefabs;

    [Header("Grid Size")]
    public int width = 12;
    public int height = 12;

    [Header("Scale")]
    public float hexScale = 0.5f;

    [Header("Spacing")]
    public float colSpacingFactor = 0.75f;
    public float rowSpacingFactor = 1f;

    [Header("Randomness")]
    public int seed = 0;

    [Header("Map Type")]
    public MapType mapType = MapType.Classic;

    [Header("Camp Placement")]
    public GameObject normalCampPrefab;
    public GameObject neutralCampPrefab;
    [Tooltip("Nombre de camps normaux à placer (doit être >= nb de joueurs x campsPerPlayer)")]
    public int normalCampCount = 2;
    [Tooltip("Nombre de camps neutres spéciaux à placer")]
    public int neutralCampCount = 3;
    [Tooltip("Distance minimale entre deux camps")]
    public float minCampDistance = 3f;
    [Tooltip("Hauteur des camps au-dessus des tuiles")]
    public float campYOffset = 1f;

    private readonly List<HexTile> walkableTiles = new List<HexTile>();

    void Start()
    {
        if (walkablePrefabs == null || walkablePrefabs.Length == 0)
        {
            Debug.LogError("Aucun prefab marchable assigné !");
            return;
        }

        if (seed != 0) Random.InitState(seed);

        GameObject temp = Instantiate(walkablePrefabs[0]);
        Renderer r = temp.GetComponentInChildren<Renderer>();
        float hexWidth = r.bounds.size.x * hexScale;
        float hexDepth = r.bounds.size.z * hexScale;
        DestroyImmediate(temp);

        float colSpacing = hexWidth * colSpacingFactor;
        float rowSpacing = hexDepth * rowSpacingFactor;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float xPos = x * colSpacing;
                float zPos = z * rowSpacing + (x % 2 == 1 ? rowSpacing * 0.5f : 0f);
                Vector3 pos = new Vector3(xPos, 0f, zPos);

                (GameObject prefab, HexTerrain terrain) = PickTerrain();
                if (terrain == HexTerrain.Water)
                    pos.y = -0.05f;
                GameObject instance = Instantiate(prefab, pos, Quaternion.identity, transform);
                instance.transform.localScale = Vector3.one * hexScale;

                HexTile tile = instance.AddComponent<HexTile>();
                tile.terrain = terrain;

                if (terrain == HexTerrain.Walkable)
                    walkableTiles.Add(tile);

                AssignNavMesh(instance, terrain);
            }
        }

        BuildNavMesh();
        PlaceCamps();

        // Assigner les camps aux régions géographiques après génération
        if (RegionManager.Instance != null)
            RegionManager.Instance.AssignCampsToRegions();
    }

    // ── Camp Placement ───────────────────────────────────────────────

    void PlaceCamps()
    {
        if (walkableTiles.Count == 0) return;

        List<HexTile> available = new List<HexTile>(walkableTiles);
        List<Vector3> placed = new List<Vector3>();

        // Camps normaux : dispersion maximale
        if (normalCampPrefab != null)
        {
            for (int i = 0; i < normalCampCount && available.Count > 0; i++)
            {
                HexTile tile = SelectMaxSpreadTile(available, placed);
                Vector3 worldPos = tile.transform.position;
                SpawnCamp(normalCampPrefab, worldPos, CampType.Normal, isNeutral: false);
                placed.Add(worldPos);
                RemoveNearby(available, worldPos, minCampDistance);
            }
        }

        // Camps neutres spéciaux : placement aléatoire
        if (neutralCampPrefab != null)
        {
            for (int i = 0; i < neutralCampCount && available.Count > 0; i++)
            {
                int idx = Random.Range(0, available.Count);
                HexTile tile = available[idx];
                Vector3 worldPos = tile.transform.position;
                SpawnCamp(neutralCampPrefab, worldPos, CampType.NeutralSpecial, isNeutral: true);
                placed.Add(worldPos);
                RemoveNearby(available, worldPos, minCampDistance * 0.5f);
            }
        }
    }

    // Choisit la tuile qui maximise la distance minimale aux camps déjà placés
    HexTile SelectMaxSpreadTile(List<HexTile> candidates, List<Vector3> placed)
    {
        if (placed.Count == 0)
            return candidates[Random.Range(0, candidates.Count)];

        HexTile best = candidates[0];
        float bestMinDist = -1f;

        foreach (HexTile candidate in candidates)
        {
            Vector3 pos = candidate.transform.position;
            float minDist = float.MaxValue;
            foreach (Vector3 p in placed)
                minDist = Mathf.Min(minDist, Vector3.Distance(pos, p));

            if (minDist > bestMinDist)
            {
                bestMinDist = minDist;
                best = candidate;
            }
        }
        return best;
    }

    void RemoveNearby(List<HexTile> list, Vector3 center, float radius)
    {
        list.RemoveAll(t => Vector3.Distance(t.transform.position, center) < radius);
    }

    void SpawnCamp(GameObject prefab, Vector3 worldPos, CampType type, bool isNeutral)
    {
        // Sample NavMesh to land on the actual tile surface (pivot may be at bottom)
        Vector3 surfacePos = worldPos;
        if (UnityEngine.AI.NavMesh.SamplePosition(worldPos + Vector3.up * 3f, out UnityEngine.AI.NavMeshHit hit, 6f, UnityEngine.AI.NavMesh.AllAreas))
            surfacePos = hit.position;

        GameObject campObj = Instantiate(prefab, surfacePos + Vector3.up * campYOffset, Quaternion.identity);
        Camp camp = campObj.GetComponent<Camp>();
        if (camp == null) return;

        camp.campType = type;
        camp.isNeutral = true; // stays neutral until GameManager assigns ownership
        camp.owner = null;
    }

    // ── Terrain ──────────────────────────────────────────────────────

    (GameObject, HexTerrain) PickTerrain()
    {
        int walkW, mountW, waterW;

        switch (mapType)
        {
            case MapType.FrozenPeaks:
                walkW = 50; mountW = 50; waterW = 0;
                break;
            case MapType.Island:
                walkW = 20; mountW = 5; waterW = 75;
                break;
            default:
                walkW = 65; mountW = 20; waterW = 15;
                break;
        }

        int total = walkW + mountW + waterW;
        int roll = Random.Range(0, total);

        if (roll < walkW)
            return (walkablePrefabs[Random.Range(0, walkablePrefabs.Length)], HexTerrain.Walkable);

        roll -= walkW;
        if (roll < mountW && mountainPrefabs != null && mountainPrefabs.Length > 0)
            return (mountainPrefabs[Random.Range(0, mountainPrefabs.Length)], HexTerrain.Mountain);

        if (waterPrefabs != null && waterPrefabs.Length > 0)
            return (waterPrefabs[Random.Range(0, waterPrefabs.Length)], HexTerrain.Water);

        return (walkablePrefabs[Random.Range(0, walkablePrefabs.Length)], HexTerrain.Walkable);
    }

    void AssignNavMesh(GameObject hex, HexTerrain terrain)
    {
        NavMeshModifier modifier = hex.AddComponent<NavMeshModifier>();
        modifier.overrideArea = true;
        modifier.area = terrain == HexTerrain.Walkable
            ? UnityEngine.AI.NavMesh.GetAreaFromName("Walkable")
            : UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
    }

    void BuildNavMesh()
    {
        NavMeshSurface surface = gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.BuildNavMesh();
    }
}
