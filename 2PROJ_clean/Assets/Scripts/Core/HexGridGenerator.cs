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
    public int campsPerCorner = 1;
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

        if (mapType == MapType.Island)
        {
            BuildIslandGrid(grid, ox, oz);
        }
        else
        {
            for (int x = 0; x < width; x++)
                for (int z = 0; z < height; z++)
                    grid[x, z] = SampleTerrain(x, z, ox, oz);

            for (int p = 0; p < smoothingPasses; p++)
                grid = SmoothPass(grid);

            ForceCornerWalkable(grid);
            BridgeCorners(grid);
        }

        // Centre toujours walkable (5ème joueur possible)
        ForceCenterWalkable(grid);

        return grid;
    }

    // ── Islands : majorité eau, archipels ────────────────────────────

    private void BuildIslandGrid(HexTerrain[,] grid, float ox, float oz)
    {
        // Tout eau par défaut
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                grid[x, z] = HexTerrain.Water;

        // Deux couches de bruit pour des formes d'îles organiques
        float scale1 = noiseScale * 2.2f;
        float scale2 = noiseScale * 4.5f;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float v1 = Mathf.PerlinNoise((x + ox) * scale1, (z + oz) * scale1);
                float v2 = Mathf.PerlinNoise((x + ox + 500f) * scale2, (z + oz + 500f) * scale2) * 0.35f;
                float v  = v1 + v2;
                // Seulement les pics → terres (~30% de la map)
                if (v > 1.05f)      grid[x, z] = HexTerrain.Mountain;
                else if (v > 0.88f) grid[x, z] = HexTerrain.Walkable;
            }
        }

        for (int p = 0; p < smoothingPasses; p++)
            grid = SmoothPass(grid);

        // Coins garantis walkable (camps de départ)
        ForceCornerWalkable(grid);
    }

    // ── Classic / FrozenPeaks ────────────────────────────────────────

    private HexTerrain SampleTerrain(int x, int z, float ox, float oz)
    {
        float v = Mathf.PerlinNoise((x + ox) * noiseScale, (z + oz) * noiseScale);

        switch (mapType)
        {
            case MapType.FrozenPeaks:
                // ~55% walkable, 45% montagne (pas d'eau)
                return v > 0.44f ? HexTerrain.Walkable : HexTerrain.Mountain;

            default: // Classic
                // ~55% walkable, ~20% montagne, ~25% eau
                if (v > 0.45f) return HexTerrain.Walkable;
                if (v > 0.32f) return HexTerrain.Mountain;
                return HexTerrain.Water;
        }
    }

    // Lissage : chaque tuile prend le type majoritaire de ses voisins
    private HexTerrain[,] SmoothPass(HexTerrain[,] grid)
    {
        HexTerrain[,] next = new HexTerrain[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                int wCnt = 0, mCnt = 0, waCnt = 0, total = 0;
                foreach (Vector2Int nb in GetHexNeighbors(new Vector2Int(x, z)))
                {
                    if (nb.x < 0 || nb.x >= width || nb.y < 0 || nb.y >= height) continue;
                    total++;
                    switch (grid[nb.x, nb.y]) {
                        case HexTerrain.Walkable: wCnt++;  break;
                        case HexTerrain.Mountain: mCnt++;  break;
                        case HexTerrain.Water:    waCnt++; break;
                    }
                }
                if (total == 0) { next[x, z] = grid[x, z]; continue; }

                if (wCnt  > total / 2) next[x, z] = HexTerrain.Walkable;
                else if (mCnt  > total / 2) next[x, z] = HexTerrain.Mountain;
                else if (waCnt > total / 2) next[x, z] = HexTerrain.Water;
                else next[x, z] = grid[x, z];
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

    private void ForceCenterWalkable(HexTerrain[,] grid)
    {
        int cx = width  / 2;
        int cz = height / 2;
        int m  = Mathf.Clamp(cornerMargin, 1, Mathf.Min(width, height) / 4);
        for (int dx = -m; dx <= m; dx++)
        for (int dz = -m; dz <= m; dz++)
        {
            int nx = cx + dx, nz = cz + dz;
            if (nx >= 0 && nx < width && nz >= 0 && nz < height)
                grid[nx, nz] = HexTerrain.Walkable;
        }
    }

    // Pont minimal uniquement si un coin est déconnecté du coin 0
    private void BridgeCorners(HexTerrain[,] grid)
    {
        Vector2Int[] corners =
        {
            new Vector2Int(0,         0),
            new Vector2Int(width - 1, 0),
            new Vector2Int(0,         height - 1),
            new Vector2Int(width - 1, height - 1),
        };

        HashSet<Vector2Int> reachable = BFSWalkable(grid, corners[0]);

        for (int i = 1; i < corners.Length; i++)
        {
            if (reachable.Contains(corners[i])) continue;

            // Trouver la tuile walkable la plus proche du coin isolé
            Vector2Int nearest = FindNearestInSet(reachable, corners[i]);
            CarvePath(grid, corners[i], nearest);
            reachable = BFSWalkable(grid, corners[0]);
        }
    }

    private HashSet<Vector2Int> BFSWalkable(HexTerrain[,] grid, Vector2Int start)
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        if (start.x < 0 || start.x >= width || start.y < 0 || start.y >= height) return visited;
        if (grid[start.x, start.y] != HexTerrain.Walkable) return visited;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            foreach (Vector2Int nb in GetHexNeighbors(cur))
            {
                if (visited.Contains(nb)) continue;
                if (nb.x < 0 || nb.x >= width || nb.y < 0 || nb.y >= height) continue;
                if (grid[nb.x, nb.y] != HexTerrain.Walkable) continue;
                visited.Add(nb);
                queue.Enqueue(nb);
            }
        }
        return visited;
    }

    private Vector2Int FindNearestInSet(HashSet<Vector2Int> set, Vector2Int target)
    {
        Vector2Int best = target;
        float bestDist = float.MaxValue;
        foreach (Vector2Int v in set)
        {
            float d = Vector2Int.Distance(v, target);
            if (d < bestDist) { bestDist = d; best = v; }
        }
        return best;
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

        if (mapType == MapType.Island)
        {
            // Islands : garder toutes les îles, supprimer seulement les tuiles isolées (1 seule)
            RemoveSingletonIslands(tileCoords);
            return;
        }

        // Autres maps : garder seulement le composant connexe principal (coin 0)
        Vector2Int corner0 = new Vector2Int(0, 0);
        HexTile seedTile = ClosestWalkableTo(corner0);
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

        List<HexTile> isolated = new List<HexTile>();
        foreach (HexTile tile in walkableTiles)
            if (!visited.Contains(tileCoords[tile])) isolated.Add(tile);

        ConvertToMountain(isolated);
        if (isolated.Count > 0)
            Debug.Log($"[HexGrid] {isolated.Count} tuile(s) isolée(s) converties.");
    }

    private void RemoveSingletonIslands(Dictionary<HexTile, Vector2Int> tileCoords)
    {
        // Trouver toutes les tuiles qui n'ont aucun voisin walkable → supprimer
        List<HexTile> singletons = new List<HexTile>();
        foreach (HexTile tile in walkableTiles)
        {
            Vector2Int coord = tileCoords[tile];
            bool hasNeighbor = false;
            foreach (Vector2Int nb in GetHexNeighbors(coord))
            {
                if (!tileMap.TryGetValue(nb, out HexTile nbTile)) continue;
                if (nbTile.terrain == HexTerrain.Walkable) { hasNeighbor = true; break; }
            }
            if (!hasNeighbor) singletons.Add(tile);
        }
        ConvertToMountain(singletons);
    }

    private void ConvertToMountain(List<HexTile> tiles)
    {
        foreach (HexTile tile in tiles)
        {
            tile.terrain = HexTerrain.Mountain;
            NavMeshModifier mod = tile.GetComponent<NavMeshModifier>();
            if (mod != null) mod.area = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
            walkableTiles.Remove(tile);
        }
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
