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

    [Header("Grid")]
    public int width  = 12;
    public int height = 12;
    public float hexScale = 0.5f;
    public float colSpacingFactor = 0.75f;
    public float rowSpacingFactor = 1f;

    [Header("Randomness")]
    public int seed = 0;

    [Header("Map Type")]
    public MapType mapType = MapType.Classic;

    [Header("Camps")]
    public GameObject normalCampPrefab;
    public GameObject neutralCampPrefab;
    public int normalCampCount  = 8;
    public int neutralCampCount = 3;
    [Tooltip("Distance min entre deux camps (en unités world)")]
    public float minCampDistance = 5f;
    [Tooltip("Hauteur au-dessus de la surface de la tuile")]
    public float campYOffset = 0.2f;

    // Tuiles walkables disponibles pour placer les camps
    private readonly List<HexTile> walkableTiles = new List<HexTile>();

    public static Bounds MapBounds { get; private set; }

    private void Start()
    {
        if (walkablePrefabs == null || walkablePrefabs.Length == 0)
        {
            Debug.LogError("[HexGrid] Aucun prefab walkable assigné !");
            return;
        }

        if (seed != 0) Random.InitState(seed);

        // 1. Mesurer un hex pour calculer l'espacement
        float hexW, hexD;
        MeasureHex(out hexW, out hexD);

        // 2. Générer la grille
        GenerateGrid(hexW, hexD);

        // 3. Bake NavMesh
        BuildNavMesh();

        // 4. Placer les camps sur les tuiles walkables
        PlaceCamps();

        // 5. Assigner les camps aux régions
        if (RegionManager.Instance != null)
            RegionManager.Instance.AssignCampsToRegions();
    }

    // ── 1. Mesure ────────────────────────────────────────────────────

    private void MeasureHex(out float hexW, out float hexD)
    {
        GameObject tmp = Instantiate(walkablePrefabs[0]);
        tmp.transform.localScale = Vector3.one * hexScale;
        Renderer r = tmp.GetComponentInChildren<Renderer>();
        hexW = r != null ? r.bounds.size.x : hexScale;
        hexD = r != null ? r.bounds.size.z : hexScale;
        DestroyImmediate(tmp);
    }

    // ── 2. Grille ────────────────────────────────────────────────────

    private void GenerateGrid(float hexW, float hexD)
    {
        float colSpacing = hexW * colSpacingFactor;
        float rowSpacing = hexD * rowSpacingFactor;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float xPos = x * colSpacing;
                float zPos = z * rowSpacing + (x % 2 == 1 ? rowSpacing * 0.5f : 0f);

                (GameObject prefab, HexTerrain terrain) = PickTerrain();

                GameObject tile = Instantiate(prefab, new Vector3(xPos, 0f, zPos), Quaternion.identity, transform);
                tile.transform.localScale = Vector3.one * hexScale;

                HexTile ht = tile.AddComponent<HexTile>();
                ht.terrain = terrain;

                // Ajouter MeshCollider sur chaque mesh enfant (nécessaire pour NavMesh bake + raycast camps)
                foreach (MeshFilter mf in tile.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null) continue;
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }

                // NavMesh modifier
                NavMeshModifier mod = tile.AddComponent<NavMeshModifier>();
                mod.overrideArea = true;
                mod.area = terrain == HexTerrain.Walkable
                    ? UnityEngine.AI.NavMesh.GetAreaFromName("Walkable")
                    : UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");

                if (terrain == HexTerrain.Walkable)
                    walkableTiles.Add(ht);
            }
        }

        ComputeMapBounds(hexW, hexD);
    }

    private void ComputeMapBounds(float hexW, float hexD)
    {
        float colSpacing = hexW * colSpacingFactor;
        float rowSpacing = hexD * rowSpacingFactor;

        float xMax = (width  - 1) * colSpacing + hexW;
        float zMax = (height - 1) * rowSpacing + rowSpacing * 0.5f + hexD;

        MapBounds = new Bounds(
            new Vector3(xMax * 0.5f, 0f, zMax * 0.5f),
            new Vector3(xMax, 0f, zMax)
        );
    }

    // ── 3. NavMesh ───────────────────────────────────────────────────

    private void BuildNavMesh()
    {
        NavMeshSurface surface = gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.BuildNavMesh();
        Debug.Log("[HexGrid] NavMesh baked.");
    }

    // ── 4. Placement des camps ───────────────────────────────────────

    private void PlaceCamps()
    {
        if (walkableTiles.Count == 0)
        {
            Debug.LogError("[HexGrid] Aucune tuile walkable !");
            return;
        }

        List<HexTile> pool = new List<HexTile>(walkableTiles);
        List<Vector3> usedPositions = new List<Vector3>();

        // Camps normaux — dispersion max
        int normalPlaced = 0;
        if (normalCampPrefab != null)
        {
            for (int i = 0; i < normalCampCount && pool.Count > 0; i++)
            {
                HexTile tile = PickSpreadTile(pool, usedPositions);
                if (tile == null) break;

                SpawnCamp(normalCampPrefab, tile, CampType.Normal);
                usedPositions.Add(tile.transform.position);
                RemoveTooClose(pool, tile.transform.position, minCampDistance);
                normalPlaced++;
            }
        }
        Debug.Log($"[HexGrid] {normalPlaced} camps normaux placés.");

        // Camps neutres spéciaux — éloignés des normaux
        int neutralPlaced = 0;
        if (neutralCampPrefab != null)
        {
            for (int i = 0; i < neutralCampCount && pool.Count > 0; i++)
            {
                HexTile tile = PickSpreadTile(pool, usedPositions);
                if (tile == null) break;

                SpawnCamp(neutralCampPrefab, tile, CampType.NeutralSpecial);
                usedPositions.Add(tile.transform.position);
                RemoveTooClose(pool, tile.transform.position, minCampDistance * 0.5f);
                neutralPlaced++;
            }
        }
        Debug.Log($"[HexGrid] {neutralPlaced} camps neutres placés.");
    }

    private void SpawnCamp(GameObject prefab, HexTile tile, CampType type)
    {
        // Lire le dessus réel du mesh via Renderer.bounds.max.y (pas besoin de collider)
        Vector3 tileCenter = tile.transform.position;
        float   surfaceY   = tileCenter.y;

        Renderer r = tile.GetComponentInChildren<Renderer>();
        if (r != null) surfaceY = r.bounds.max.y;

        Vector3 spawnPos = new Vector3(tileCenter.x, surfaceY + campYOffset, tileCenter.z);

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);

        Camp camp = obj.GetComponent<Camp>();
        if (camp == null)
        {
            Debug.LogError($"[HexGrid] Prefab '{prefab.name}' n'a pas de composant Camp !");
            Destroy(obj);
            return;
        }

        camp.campType = type;
        camp.isNeutral = true;
        camp.owner     = null;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    // Sélectionne la tuile la plus éloignée de toutes les positions déjà utilisées
    private HexTile PickSpreadTile(List<HexTile> pool, List<Vector3> used)
    {
        if (pool.Count == 0) return null;
        if (used.Count == 0) return pool[Random.Range(0, pool.Count)];

        HexTile best     = pool[0];
        float   bestDist = -1f;

        foreach (HexTile t in pool)
        {
            float minDist = float.MaxValue;
            foreach (Vector3 u in used)
                minDist = Mathf.Min(minDist, Vector3.Distance(t.transform.position, u));

            if (minDist > bestDist) { bestDist = minDist; best = t; }
        }
        return best;
    }

    private void RemoveTooClose(List<HexTile> pool, Vector3 center, float radius)
    {
        pool.RemoveAll(t => Vector3.Distance(t.transform.position, center) < radius);
    }

    private (GameObject, HexTerrain) PickTerrain()
    {
        int wW, mW, wWater;
        switch (mapType)
        {
            case MapType.FrozenPeaks: wW = 50; mW = 50; wWater = 0;  break;
            case MapType.Island:      wW = 20; mW = 5;  wWater = 75; break;
            default:                  wW = 65; mW = 20; wWater = 15; break;
        }

        int total = wW + mW + wWater;
        int roll  = Random.Range(0, total);

        if (roll < wW)
            return (walkablePrefabs[Random.Range(0, walkablePrefabs.Length)], HexTerrain.Walkable);

        roll -= wW;
        if (roll < mW && mountainPrefabs != null && mountainPrefabs.Length > 0)
            return (mountainPrefabs[Random.Range(0, mountainPrefabs.Length)], HexTerrain.Mountain);

        if (waterPrefabs != null && waterPrefabs.Length > 0)
            return (waterPrefabs[Random.Range(0, waterPrefabs.Length)], HexTerrain.Water);

        return (walkablePrefabs[Random.Range(0, walkablePrefabs.Length)], HexTerrain.Walkable);
    }
}
