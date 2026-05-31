using System.Collections.Generic;
using UnityEngine;

namespace SupKonQuest
{
    [DefaultExecutionOrder(0)]
    public class RegionManager : MonoBehaviour
    {
        public static RegionManager Instance { get; private set; }

        [Header("Pool de données (optionnel)")]
        public RegionData[] regionDataPool;

        [Header("Découpage de la map")]
        public int regionsX = 2;
        public int regionsZ = 2;

        private static readonly string[] NameKeys =
        {
            "region_north", "region_east", "region_south", "region_west",
            "region_center", "region_mountains", "region_coast", "region_islands"
        };
        private static readonly int[] DefaultBonuses = { 10, 10, 10, 10, 15, 12, 8, 20 };

        private readonly List<Region> regionList = new List<Region>();
        private Material lineMat;
        private GUIStyle labelStyle;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader != null)
            {
                lineMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                lineMat.SetInt("_Cull",     (int)UnityEngine.Rendering.CullMode.Off);
                lineMat.SetInt("_ZWrite", 0);
            }
        }

        public void GenerateRegions(Bounds mapBounds)
        {
            foreach (Region r in regionList)
                if (r != null) Destroy(r.gameObject);
            regionList.Clear();

            float zoneW = mapBounds.size.x / regionsX;
            float zoneD = mapBounds.size.z / regionsZ;
            int idx = 0;

            for (int ix = 0; ix < regionsX; ix++)
            {
                for (int iz = 0; iz < regionsZ; iz++)
                {
                    float cx = mapBounds.min.x + (ix + 0.5f) * zoneW;
                    float cz = mapBounds.min.z + (iz + 0.5f) * zoneD;

                    GameObject go = new GameObject($"Region_{ix}_{iz}");
                    go.transform.SetParent(transform);

                    Region r    = go.AddComponent<Region>();
                    r.center    = new Vector3(cx, 0f, cz);
                    r.size      = new Vector3(zoneW, 200f, zoneD);
                    r.nameKey          = idx < NameKeys.Length       ? NameKeys[idx]       : "region_default";
                    r.defaultBonusGold = idx < DefaultBonuses.Length ? DefaultBonuses[idx] : 10;

                    if (regionDataPool != null && regionDataPool.Length > 0)
                        r.data = regionDataPool[idx % regionDataPool.Length];

                    idx++;
                    regionList.Add(r);
                }
            }

            Debug.Log($"[RegionManager] {regionList.Count} régions ({regionsX}×{regionsZ}).");
        }

        public void AssignCampsToRegions()
        {
            foreach (Region r in regionList) r.camps.Clear();

            Camp[] allCamps = FindObjectsByType<Camp>(FindObjectsSortMode.None);
            foreach (Camp camp in allCamps)
            {
                foreach (Region r in regionList)
                {
                    if (!r.ContainsPoint(camp.transform.position)) continue;
                    r.camps.Add(camp);
                    break;
                }
            }
        }

        public Region[] GetAllRegions() => regionList.ToArray();

        public List<Region> GetRegionsOwnedBy(PlayerData player)
        {
            List<Region> owned = new List<Region>();
            foreach (Region r in regionList)
                if (r.IsOwnedBy(player)) owned.Add(r);
            return owned;
        }

        public int GetRegionBonusGold(PlayerData player)
        {
            int total = 0;
            foreach (Region r in regionList)
                if (PlayerHasMajority(r, player)) total += r.GetBonusGold();
            return total;
        }

        private static bool PlayerHasMajority(Region region, PlayerData player)
        {
            int playerCount = 0, maxRival = 0;
            Dictionary<PlayerData, int> counts = new Dictionary<PlayerData, int>();
            foreach (Camp c in region.camps)
            {
                if (c.owner == null) continue;
                counts.TryGetValue(c.owner, out int n);
                counts[c.owner] = n + 1;
            }
            counts.TryGetValue(player, out playerCount);
            foreach (var kv in counts)
                if (kv.Key != player && kv.Value > maxRival) maxRival = kv.Value;
            return playerCount > 0 && playerCount > maxRival;
        }

        public bool IsInOwnedRegion(Vector3 position, int ownerId)
        {
            PlayerData player = GameManager.Instance?.GetPlayerById(ownerId);
            if (player == null) return false;
            foreach (Region r in regionList)
                if (r.IsOwnedBy(player) && r.ContainsPoint(position)) return true;
            return false;
        }

        private void OnRenderObject()
        {
            if (lineMat == null || regionList.Count == 0) return;

            lineMat.SetPass(0);
            GL.Begin(GL.LINES);

            foreach (Region r in regionList)
            {
                PlayerData owner = r.GetOwner();
                Color col = owner != null
                    ? new Color(owner.playerColor.r, owner.playerColor.g, owner.playerColor.b, 0.85f)
                    : new Color(0.55f, 0.55f, 0.55f, 0.5f);
                GL.Color(col);

                Vector3 c    = r.center;
                Vector3 half = r.size * 0.5f;
                float   y    = 0.15f;

                Vector3 a = new Vector3(c.x - half.x, y, c.z - half.z);
                Vector3 b = new Vector3(c.x + half.x, y, c.z - half.z);
                Vector3 d = new Vector3(c.x + half.x, y, c.z + half.z);
                Vector3 e = new Vector3(c.x - half.x, y, c.z + half.z);

                GL.Vertex(a); GL.Vertex(b);
                GL.Vertex(b); GL.Vertex(d);
                GL.Vertex(d); GL.Vertex(e);
                GL.Vertex(e); GL.Vertex(a);
            }

            GL.End();
        }

        private void OnGUI()
        {
            Camera cam = Camera.main;
            if (cam == null || regionList.Count == 0) return;

            InitLabelStyle();

            foreach (Region r in regionList)
            {
                Vector3 sp = cam.WorldToScreenPoint(r.center);
                if (sp.z < 0f) continue;

                float x = sp.x;
                float y = Screen.height - sp.y;

                PlayerData owner = r.GetOwner();
                GUI.color = owner != null
                    ? new Color(owner.playerColor.r, owner.playerColor.g, owner.playerColor.b, 0.9f)
                    : new Color(0.8f, 0.8f, 0.8f, 0.6f);

                string label = r.GetDisplayName();
                if (owner != null) label += $"\n+{r.GetBonusGold()}g";

                GUI.Label(new Rect(x - 50f, y - 18f, 100f, 36f), label, labelStyle);
                GUI.color = Color.white;
            }
        }

        private void InitLabelStyle()
        {
            if (labelStyle != null) return;
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            };
        }
    }
}
