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

    [Header("Génération")]
    public int seed = 0;
    [Tooltip("Echelle du bruit de Perlin (plus petit = zones plus larges)")]
    [Range(0.05f, 0.3f)]
    public float noiseScale = 0.12f;
    [Tooltip("Passes de lissage cellulaire (2-3 recommandé)")]
    [Range(0, 5)]
    public int smoothingPasses = 2;
    [Tooltip("Tuiles forcées walkable dans chaque coin pour les camps de départ")]
    [Range(1, 4)]
    public int cornerMargin = 2;

    [Header("Map Type")]
    public MapType mapType = MapType.Classic;

    [Header("Camps")]
    public GameObject normalCampPrefab;
    public GameObject neutralCampPrefab;
    [Tooltip("Camps placés dans chaque coin (un coin = un joueur)")]
    public int campsPerCorner = 2;
    public int neutralCampCount = 8;
    [Tooltip("Distance min entre deux camps (en unités world)")]
    public float minCampDistance = 3f;
    [Tooltip("Hauteur au-dessus de la surface de la tuile")]
    public float campYOffset = 0.2f;

    private readonly List<HexTile> walkableTiles = new List<HexTile>();
    private readonly Dictionary<Vector2Int, HexTile> tileMap = new Dictionary<Vector2Int, HexTile>();

    public static List<Camp>[] CornerCamps { get; private set; }
    public static Bounds MapBounds { get; private set; }

    private void Start()
    {
        if (walkablePrefabs == null || walkablePrefabs.Length == 0)
        {
            Debug.LogError("[HexGrid] Aucun prefab walkable assigné !");
            return;
        }

        if (seed != 0) Random.InitState(seed);

        float hexW, hexD;
        MeasureHex(out hexW, out hexD);
        GenerateGrid(hexW, hexD);
        BuildNavMesh();
        PlaceCamps();

        if (RegionManager.Instance != null)
        {
            RegionManager.Instance.GenerateRegions(MapBounds);
            RegionManager.Instance.AssignCampsToRegions();
        }
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

        // Pré-calculer le terrain avec bruit + lissage
        HexTerrain[,] terrainGrid = BuildTerrainGrid();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                HexTerrain terrain = terrainGrid[x, z];
                GameObject prefab   = GetPrefabForTerrain(terrain);

                float xPos = x * colSpacing;
                float zPos = z * rowSpacing + (x % 2 == 1 ? rowSpacing * 0.5f : 0f);

                GameObject tile = Instantiate(prefab, new Vector3(xPos, 0f, zPos), Quaternion.identity, transform);
                tile.transform.localScale = Vector3.one * hexScale;

                HexTile ht = tile.AddComponent<HexTile>();
                ht.terrain = terrain;

                foreach (MeshFilter mf in tile.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null) continue;
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }

                NavMeshModifier mod = tile.AddComponent<NavMeshModifier>();
                mod.overrideArea = true;
                mod.area = terrain == HexTerrain.Walkable
                    ? UnityEngine.AI.NavMesh.GetAreaFromName("Walkable")
                    : UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");

                tileMap[new Vector2Int(x, z)] = ht;
                if (terrain == HexTerrain.Walkable) walkableTiles.Add(ht);
            }
        }

        FixIsolatedTiles();
        ComputeMapBounds(hexW, hexD);
    }

    // ── 3. Génération du terrain ─────────────────────────────────────

    private HexTerrain[,] BuildTerrainGrid()
    {
        float ox = Random.Range(0f, 999f);
        float oz = Random.Range(0f, 999f);

        HexTerrain[,] grid = new HexTerrain[width, height];

        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                grid[x, z] = SampleTerrain(x, z, ox, oz);

        for (int p = 0; p < smoothingPasses; p++)
            grid = SmoothPass(grid);

        ForceCornerWalkable(grid);
        ForceConnectivity(grid);   // couloirs coins → centre

        return grid;
    }

    private HexTerrain SampleTerrain(int x, int z, float ox, float oz)
    {
        float v = Mathf.PerlinNoise((x + ox) * noiseScale, (z + oz) * noiseScale);

        switch (mapType)
        {
            case MapType.FrozenPeaks:
                // ~80% walkable, 20% montagne
                return v < 0.22f ? HexTerrain.Mountain : HexTerrain.Walkable;

            case MapType.Island:
                // ~80% walkable, eau aux extrêmes
                if (v < 0.10f || v > 0.90f) return HexTerrain.Water;
                return HexTerrain.Walkable;

            default: // Classic
                // ~78% walkable, obstacles rares
                if (v < 0.10f) return HexTerrain.Water;
                if (v < 0.22f) return HexTerrain.Mountain;
                return HexTerrain.Walkable;
        }
    }

    // Lissage cellulaire : agrandit les amas d'obstacles (pas de walkable)
    private HexTerrain[,] SmoothPass(HexTerrain[,] grid)
    {
        HexTerrain[,] next = new HexTerrain[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                int nonWalkable = 0, total = 0;

                foreach (Vector2Int nb in GetHexNeighbors(new Vector2Int(x, z)))
                {
                    if (nb.x < 0 || nb.x >= width || nb.y < 0 || nb.y >= height) continue;
                    total++;
                    if (grid[nb.x, nb.y] != HexTerrain.Walkable) nonWalkable++;
                }

                if (total == 0) { next[x, z] = grid[x, z]; continue; }

                // Un obstacle ne grossit que si la majorité des voisins sont obstacles
                if (grid[x, z] != HexTerrain.Walkable && nonWalkable >= (total * 2 / 3))
                    next[x, z] = grid[x, z];
                else if (nonWalkable == 0)
                    next[x, z] = HexTerrain.Walkable; // entouré de walkable → walkable
                else
                    next[x, z] = grid[x, z]; // sinon garder
            }
        }

        return next;
    }

    private void ForceCornerWalkable(HexTerrain[,] grid)
    {
        int m = Mathf.Clamp(cornerMargin, 1, Mathf.Min(width, height) / 3);
        for (int dx = 0; dx < m; dx++)
        for (int dz = 0; dz < m; dz++)
        {
            grid[dx,         dz]          = HexTerrain.Walkable;
            grid[width-1-dx, dz]          = HexTerrain.Walkable;
            grid[dx,         height-1-dz] = HexTerrain.Walkable;
            grid[width-1-dx, height-1-dz] = HexTerrain.Walkable;
        }
    }

    // Trace un couloir walkable de chaque coin vers le centre
    private void ForceConnectivity(HexTerrain[,] grid)
    {
        Vector2Int center = new Vector2Int(width / 2, height / 2);
        Vector2Int[] corners =
        {
            new Vector2Int(0,         0),
            new Vector2Int(width - 1, 0),
            new Vector2Int(0,         height - 1),
            new Vector2Int(width - 1, height - 1),
        };
        foreach (Vector2Int corner in corners)
            CarvePath(grid, corner, center);
    }

    private void CarvePath(HexTerrain[,] grid, Vector2Int from, Vector2Int to)
    {
        int x = from.x, z = from.y;
        while (x != to.x || z != to.y)
        {
            grid[x, z] = HexTerrain.Walkable;
            if (x < to.x) x++;
            else if (x > to.x) x--;
            if (z < to.y) z++;
            else if (z > to.y) z--;
        }
        grid[to.x, to.y] = HexTerrain.Walkable;
    }

    private GameObject GetPrefabForTerrain(HexTerrain terrain)
    {
        switch (terrain)
        {
            case HexTerrain.Mountain:
                if (mountainPrefabs != null && mountainPrefabs.Length > 0)
                    return mountainPrefabs[Random.Range(0, mountainPrefabs.Length)];
                break;
            case HexTerrain.Water:
                if (waterPrefabs != null && waterPrefabs.Length > 0)
                    return waterPrefabs[Random.Range(0, waterPrefabs.Length)];
                break;
        }
        return walkablePrefabs[Random.Range(0, walkablePrefabs.Length)];
    }

    // ── 4. Connexité BFS ─────────────────────────────────────────────

    private void FixIsolatedTiles()
    {
        if (walkableTiles.Count == 0) return;

        Dictionary<HexTile, Vector2Int> tileCoords = new Dictionary<HexTile, Vector2Int>();
        foreach (var kv in tileMap)
            if (kv.Value.terrain == HexTerrain.Walkable)
                tileCoords[kv.Value] = kv.Key;

        // BFS depuis la tuile walkable la plus centrale (meilleure graine)
        Vector2Int gridCenter = new Vector2Int(width / 2, height / 2);
        HexTile seedTile = ClosestWalkableTo(gridCenter);
        if (seedTile == null) return;

        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Vector2Int startCoord = tileCoords[seedTile];
        queue.Enqueue(startCoord);
        visited.Add(startCoord);

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            foreach (Vector2Int nb in GetHexNeighbors(cur))
            {
                if (visited.Contains(nb)) continue;
                if (!tileMap.TryGetValue(nb, out HexTile nbTile)) continue;
                if (nbTile.terrain != HexTerrain.Walkable) continue;
                visited.Add(nb);
                queue.Enqueue(nb);
            }
        }

        // Convertir les tuiles non atteignables
        List<HexTile> isolated = new List<HexTile>();
        foreach (HexTile tile in walkableTiles)
            if (!visited.Contains(tileCoords[tile])) isolated.Add(tile);

        foreach (HexTile tile in isolated)
        {
            tile.terrain = HexTerrain.Mountain;
            NavMeshModifier mod = tile.GetComponent<NavMeshModifier>();
            if (mod != null) mod.area = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
            walkableTiles.Remove(tile);
        }

        if (isolated.Count > 0)
            Debug.Log($"[HexGrid] {isolated.Count} tuile(s) isolée(s) converties.");
    }

    private HexTile ClosestWalkableTo(Vector2Int target)
    {
        HexTile best = null;
        float bestDist = float.MaxValue;
        foreach (HexTile t in walkableTiles)
        {
            if (!tileMap.ContainsValue(t)) continue;
            foreach (var kv in tileMap)
            {
                if (kv.Value != t) continue;
                float d = Vector2Int.Distance(kv.Key, target);
                if (d < bestDist) { bestDist = d; best = t; }
                break;
            }
        }
        return best ?? (walkableTiles.Count > 0 ? walkableTiles[0] : null);
    }

    private IEnumerable<Vector2Int> GetHexNeighbors(Vector2Int c)
    {
        int x = c.x, z = c.y;
        if (x % 2 == 0)
        {
            yield return new Vector2Int(x-1, z-1); yield return new Vector2Int(x-1, z);
            yield return new Vector2Int(x,   z-1); yield return new Vector2Int(x,   z+1);
            yield return new Vector2Int(x+1, z-1); yield return new Vector2Int(x+1, z);
        }
        else
        {
            yield return new Vector2Int(x-1, z);   yield return new Vector2Int(x-1, z+1);
            yield return new Vector2Int(x,   z-1); yield return new Vector2Int(x,   z+1);
            yield return new Vector2Int(x+1, z);   yield return new Vector2Int(x+1, z+1);
        }
    }

    // ── 5. NavMesh ───────────────────────────────────────────────────

    private void BuildNavMesh()
    {
        NavMeshSurface surface = gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.BuildNavMesh();
        Debug.Log("[HexGrid] NavMesh baked.");
    }

    // ── 6. Placement des camps ───────────────────────────────────────

    private void PlaceCamps()
    {
        CornerCamps = new List<Camp>[4];
        for (int i = 0; i < 4; i++) CornerCamps[i] = new List<Camp>();

        if (walkableTiles.Count == 0) { Debug.LogError("[HexGrid] Aucune tuile walkable !"); return; }

        HashSet<HexTile> used = new HashSet<HexTile>();

        Vector3[] corners =
        {
            new Vector3(MapBounds.min.x, 0f, MapBounds.min.z),
            new Vector3(MapBounds.max.x, 0f, MapBounds.min.z),
            new Vector3(MapBounds.min.x, 0f, MapBounds.max.z),
            new Vector3(MapBounds.max.x, 0f, MapBounds.max.z),
        };

        if (normalCampPrefab != null)
        {
            for (int c = 0; c < 4; c++)
            {
                List<HexTile> pool = new List<HexTile>(walkableTiles);
                pool.RemoveAll(t => used.Contains(t));
                Vector3 corner = corners[c];
                pool.Sort((a, b) =>
                    Vector3.Distance(a.transform.position, corner)
                    .CompareTo(Vector3.Distance(b.transform.position, corner)));

                List<Vector3> placed = new List<Vector3>();
                foreach (HexTile tile in pool)
                {
                    if (placed.Count >= campsPerCorner) break;
                    bool tooClose = false;
                    foreach (Vector3 p in placed)
                        if (Vector3.Distance(tile.transform.position, p) < minCampDistance * 0.5f)
                        { tooClose = true; break; }
                    if (tooClose) continue;

                    Camp camp = SpawnCamp(normalCampPrefab, tile, CampType.Normal);
                    if (camp != null) { CornerCamps[c].Add(camp); placed.Add(tile.transform.position); used.Add(tile); }
                }
                Debug.Log($"[HexGrid] Coin {c} : {placed.Count} camp(s).");
            }
        }

        if (neutralCampPrefab != null)
        {
            List<HexTile> neutralPool = new List<HexTile>(walkableTiles);
            neutralPool.RemoveAll(t => used.Contains(t));
            List<Vector3> neutralUsed = new List<Vector3>();
            int neutralPlaced = 0;
            Vector3 mapCenter = new Vector3(MapBounds.center.x, 0f, MapBounds.center.z);

            for (int i = 0; i < neutralCampCount && neutralPool.Count > 0; i++)
            {
                HexTile tile = PickCenterTile(neutralPool, neutralUsed, mapCenter);
                if (tile == null) break;
                Camp camp = SpawnCamp(neutralCampPrefab, tile, CampType.NeutralSpecial);
                if (camp != null)
                {
                    neutralUsed.Add(tile.transform.position);
                    RemoveTooClose(neutralPool, tile.transform.position, minCampDistance * 0.5f);
                    neutralPlaced++;
                }
            }
            Debug.Log($"[HexGrid] {neutralPlaced} camps neutres.");
        }
    }

    private Camp SpawnCamp(GameObject prefab, HexTile tile, CampType type)
    {
        Renderer r = tile.GetComponentInChildren<Renderer>();
        Vector3 spawnPos = r != null
            ? new Vector3(r.bounds.center.x, r.bounds.max.y + campYOffset, r.bounds.center.z)
            : tile.transform.position + Vector3.up * campYOffset;

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        Camp camp = obj.GetComponent<Camp>();
        if (camp == null) { Debug.LogError($"[HexGrid] Pas de Camp sur '{prefab.name}'"); Destroy(obj); return null; }
        camp.campType = type; camp.isNeutral = true; camp.owner = null;
        return camp;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private HexTile PickCenterTile(List<HexTile> pool, List<Vector3> used, Vector3 center)
    {
        if (pool.Count == 0) return null;
        List<HexTile> sorted = new List<HexTile>(pool);
        sorted.Sort((a, b) => Vector3.Distance(a.transform.position, center).CompareTo(Vector3.Distance(b.transform.position, center)));
        foreach (HexTile t in sorted)
        {
            bool tooClose = false;
            foreach (Vector3 u in used)
                if (Vector3.Distance(t.transform.position, u) < minCampDistance) { tooClose = true; break; }
            if (!tooClose) return t;
        }
        return sorted[0];
    }

    private void RemoveTooClose(List<HexTile> pool, Vector3 center, float radius)
        => pool.RemoveAll(t => Vector3.Distance(t.transform.position, center) < radius);

    private void ComputeMapBounds(float hexW, float hexD)
    {
        float colSpacing = hexW * colSpacingFactor;
        float rowSpacing = hexD * rowSpacingFactor;
        float xMax = (width  - 1) * colSpacing + hexW;
        float zMax = (height - 1) * rowSpacing + rowSpacing * 0.5f + hexD;
        MapBounds = new Bounds(new Vector3(xMax * 0.5f, 0f, zMax * 0.5f), new Vector3(xMax, 0f, zMax));
    }
}
