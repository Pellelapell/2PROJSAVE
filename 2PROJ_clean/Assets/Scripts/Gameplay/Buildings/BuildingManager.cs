using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SupKonQuest
{
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        [Header("Prefabs")]
        public GameObject campPrefab;
        public GameObject sawmillPrefab;
        public GameObject portPrefab;
        public GameObject castlePrefab;

        [Header("Coûts (Or / Bois)")]
        public int campGold    = 250; public int campWood    = 80;
        public int sawmillGold = 80;  public int sawmillWood = 30;
        public int portGold    = 200; public int portWood    = 80;
        public int castleGold  = 400; public int castleWood  = 200;

        [Header("Temps de construction (s)")]
        public float campBuildTime    = 15f;
        public float sawmillBuildTime = 8f;
        public float portBuildTime    = 20f;
        public float castleBuildTime  = 40f;

        [Header("Production scierie")]
        public int sawmillWoodPerTick = 5;

        [Header("Rayon de construction autour d'un camp allié")]
        public float buildRadius = 12f;

        private class Site
        {
            public HexTile tile;
            public PlayerData owner;
            public BuildingType type;
            public float timeLeft;
        }

        private readonly List<Site> queue = new List<Site>();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            for (int i = queue.Count - 1; i >= 0; i--)
            {
                queue[i].timeLeft -= Time.deltaTime;
                if (queue[i].timeLeft <= 0f)
                {
                    Complete(queue[i]);
                    queue.RemoveAt(i);
                }
            }
        }

        public bool CanBuild(HexTile tile, BuildingType type, PlayerData owner)
        {
            if (tile == null || tile.isOccupied) return false;
            if (tile.terrain == HexTerrain.Water)    return false;
            if (tile.terrain == HexTerrain.Mountain) return false;
            if (type == BuildingType.Port && !HasWaterNeighbor(tile)) return false;
            if (!IsTileReachable(tile, owner)) return false;
            return owner.CanAfford(GoldCost(type), WoodCost(type));
        }

        private static bool IsTileReachable(HexTile tile, PlayerData owner)
        {
            int walkableMask = 1 << NavMesh.GetAreaFromName("Walkable");
            if (walkableMask <= 0) return true;

            if (!NavMesh.SamplePosition(tile.transform.position, out NavMeshHit targetHit, 5f, walkableMask))
                return false;

            NavMeshPath path = new NavMeshPath();
            foreach (Camp camp in owner.ownedCamps)
            {
                if (!NavMesh.SamplePosition(camp.transform.position, out NavMeshHit startHit, 5f, walkableMask))
                    continue;
                NavMesh.CalculatePath(startHit.position, targetHit.position, walkableMask, path);
                if (path.status == NavMeshPathStatus.PathComplete) return true;
            }
            return false;
        }

        public bool TryBuild(HexTile tile, BuildingType type, PlayerData owner)
        {
            if (!CanBuild(tile, type, owner)) return false;
            if (!owner.SpendResources(GoldCost(type), WoodCost(type))) return false;

            tile.isOccupied = true;
            queue.Add(new Site { tile = tile, owner = owner, type = type, timeLeft = BuildTime(type) });
            Debug.Log($"[Build] {type} démarrée par {owner.playerName} ({BuildTime(type)}s)");
            return true;
        }

        public (int gold, int wood) GetCost(BuildingType type) => (GoldCost(type), WoodCost(type));

        public float GetProgress01(HexTile tile)
        {
            foreach (Site s in queue)
                if (s.tile == tile)
                    return 1f - s.timeLeft / BuildTime(s.type);
            return -1f;
        }

        public bool IsUnderConstruction(HexTile tile)
        {
            foreach (Site s in queue) if (s.tile == tile) return true;
            return false;
        }

        public bool CancelBuild(HexTile tile)
        {
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i].tile != tile) continue;
                queue[i].tile.isOccupied = false;
                queue.RemoveAt(i);
                return true;
            }
            return false;
        }

        private void Complete(Site site)
        {
            GameObject prefab = Prefab(site.type);
            if (prefab == null) return;

            Renderer r = site.tile.GetComponentInChildren<Renderer>();
            Vector3 pos = r != null
                ? new Vector3(r.bounds.center.x, 1.8f, r.bounds.center.z)
                : new Vector3(site.tile.transform.position.x, 1.8f, site.tile.transform.position.z);

            GameObject obj = Instantiate(prefab, pos, Quaternion.Euler(270f, 0f, 0f));
            obj.transform.localScale = prefab.transform.localScale * 190f;
            ScaleDownColliders(obj, 190f);

            if (obj.GetComponent<NavMeshObstacle>() == null)
            {
                NavMeshObstacle obstacle = obj.AddComponent<NavMeshObstacle>();
                obstacle.carving = true;
                obstacle.shape   = NavMeshObstacleShape.Box;

                obstacle.size = Vector3.one * 0.01f;
            }

            ApplyBuildingSkin(obj, site.owner, site.type);

            switch (site.type)
            {
                case BuildingType.Camp:
                case BuildingType.Port:
                case BuildingType.Castle:
                    Camp camp = obj.GetComponent<Camp>();
                    if (camp != null)
                    {
                        camp.campType  = site.type == BuildingType.Port   ? CampType.Port
                                       : site.type == BuildingType.Castle ? CampType.Castle
                                       :                                    CampType.Normal;
                        camp.isNeutral = false;
                        camp.SetOwner(site.owner);
                        if (camp.spawnPoint != null)
                            camp.spawnPoint.localPosition = Vector3.zero;
                    }
                    break;

                case BuildingType.Sawmill:
                    Sawmill saw = obj.GetComponent<Sawmill>();
                    if (saw != null) { saw.owner = site.owner; saw.woodPerTick = sawmillWoodPerTick; }
                    BuildingHealth bh = obj.GetComponent<BuildingHealth>() ?? obj.AddComponent<BuildingHealth>();
                    bh.ownerId   = site.owner.playerId;
                    bh.maxHP     = 150;
                    bh.currentHP = 150;
                    break;
            }
        }

        private static void ApplyBuildingSkin(GameObject obj, PlayerData owner, BuildingType type)
        {
            RaceDefinition def = RaceRegistry.Get(owner.race);
            if (def != null)
            {
                var skin = def.GetBuildingSkin(type);
                if (skin.HasValue)
                {
                    MeshFilter mf = obj.GetComponentInChildren<MeshFilter>();
                    if (mf != null && skin.Value.mesh != null)
                        mf.sharedMesh = skin.Value.mesh;

                    Renderer rend = obj.GetComponentInChildren<Renderer>();
                    if (rend != null && skin.Value.material != null)
                    {
                        rend.material = skin.Value.material;
                        return;
                    }
                }
            }

            foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
                rend.material.color = owner.playerColor;
        }

        private static void ScaleDownColliders(GameObject obj, float factor)
        {
            foreach (Collider col in obj.GetComponentsInChildren<Collider>())
            {
                if (col is BoxCollider bc)
                {
                    bc.size   /= factor;
                    bc.center /= factor;
                }
                else if (col is SphereCollider sc)
                    sc.radius /= factor;
                else if (col is CapsuleCollider cc)
                {
                    cc.radius /= factor;
                    cc.height /= factor;
                }
            }
        }

        private bool InOwnedTerritory(HexTile tile, PlayerData owner)
        {
            Vector3 pos = tile.transform.position;
            foreach (Camp camp in owner.ownedCamps)
                if (Vector3.Distance(camp.transform.position, pos) <= buildRadius) return true;
            return false;
        }

        private bool HasWaterNeighbor(HexTile tile)
        {
            Collider[] hits = Physics.OverlapSphere(tile.transform.position, 3f);
            foreach (Collider c in hits)
            {
                HexTile nb = c.GetComponentInParent<HexTile>();
                if (nb != null && nb != tile && nb.terrain == HexTerrain.Water) return true;
            }
            return false;
        }

        public static int CountIslandSize(Vector3 origin)
        {
            var visited = new HashSet<HexTile>();
            var queue   = new Queue<HexTile>();

            Collider[] seed = Physics.OverlapSphere(origin, 8f);
            foreach (Collider c in seed)
            {
                HexTile t = c.GetComponentInParent<HexTile>();
                if (t != null && t.terrain == HexTerrain.Walkable)
                {
                    visited.Add(t);
                    queue.Enqueue(t);
                    break;
                }
            }

            while (queue.Count > 0)
            {
                HexTile cur = queue.Dequeue();
                Collider[] neighbors = Physics.OverlapSphere(cur.transform.position, 10f);
                foreach (Collider c in neighbors)
                {
                    HexTile nb = c.GetComponentInParent<HexTile>();
                    if (nb != null && nb.terrain == HexTerrain.Walkable && visited.Add(nb))
                        queue.Enqueue(nb);
                }
            }

            return visited.Count;
        }

        private int GoldCost(BuildingType t)
        {
            switch (t)
            {
                case BuildingType.Sawmill: return sawmillGold;
                case BuildingType.Port:    return portGold;
                case BuildingType.Castle:  return castleGold;
                default:                   return campGold;
            }
        }

        private int WoodCost(BuildingType t)
        {
            switch (t)
            {
                case BuildingType.Sawmill: return sawmillWood;
                case BuildingType.Port:    return portWood;
                case BuildingType.Castle:  return castleWood;
                default:                   return campWood;
            }
        }

        private float BuildTime(BuildingType t)
        {
            switch (t)
            {
                case BuildingType.Sawmill: return sawmillBuildTime;
                case BuildingType.Port:    return portBuildTime;
                case BuildingType.Castle:  return castleBuildTime;
                default:                   return campBuildTime;
            }
        }

        private GameObject Prefab(BuildingType t)
        {
            switch (t)
            {
                case BuildingType.Sawmill: return sawmillPrefab;
                case BuildingType.Port:    return portPrefab;
                case BuildingType.Castle:  return castlePrefab;
                default:                   return campPrefab;
            }
        }
    }
}
