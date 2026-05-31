using System.Collections.Generic;
using UnityEngine;

namespace SupKonQuest
{
    public class AIController : MonoBehaviour
    {
        [Header("Player")]
        public PlayerData player;

        [Header("Difficulty  0=Easy  1=Medium  2=Hard")]
        [Range(0, 2)] public int difficultyLevel = 1;

        private enum Phase { Economy, Military, Attack }
        private Phase phase = Phase.Economy;

        private static readonly int[]   NeutralRushSize  = {  4,   6,   8 };
        private static readonly int[]   ArmyThreshold    = {  8,  12,  16 };
        private static readonly int[]   MaxPop           = { 12,  20,  30 };
        private static readonly float[] ThinkInterval    = {  2f, 1.5f, 1f };
        private static readonly float[] ProdInterval     = { 10f,  8f,  4f };
        private static readonly float[] WaveCooldown     = { 50f, 30f, 18f };

        private float thinkTimer;
        private float waveTimer;
        private float productionTimer;
        private float patrolTimer;

        private GameManager gm;
        private bool isIslandMap;

        private bool sawmillBuilt;
        private bool portBuilt;
        private bool castleBuilt;

        private Vector3 forwardPos;
        private bool    forwardPosSet;

        private void Start()
        {
            gm              = GameManager.Instance;
            difficultyLevel = Mathf.Clamp(PlayerPrefs.GetInt("AIDifficulty", difficultyLevel), 0, 2);
            isIslandMap     = PlayerPrefs.GetInt("MapType", 0) == (int)MapType.Island;
            thinkTimer      = Random.Range(0f, ThinkInterval[difficultyLevel]);
            patrolTimer     = Random.Range(5f, 12f);
        }

        private void Update()
        {
            if (gm == null || gm.gameOver || player == null || player.eliminated) return;

            thinkTimer      -= Time.deltaTime;
            waveTimer       -= Time.deltaTime;
            productionTimer -= Time.deltaTime;
            patrolTimer     -= Time.deltaTime;

            if (thinkTimer > 0f) return;
            thinkTimer = ThinkInterval[difficultyLevel] + Random.Range(-0.2f, 0.4f);

            TickPhase();
        }

        private void TickPhase()
        {
            RespondToThreat();
            AdvancePhase();

            switch (phase)
            {
                case Phase.Economy:  DoEconomy();  break;
                case Phase.Military: DoMilitary(); break;
                case Phase.Attack:   DoAttack();   break;
            }
        }

        private void AdvancePhase()
        {
            int units = CountUnits();
            switch (phase)
            {
                case Phase.Economy:
                    if (sawmillBuilt && units >= NeutralRushSize[difficultyLevel])
                        phase = Phase.Military;
                    break;

                case Phase.Military:
                    if (units >= ArmyThreshold[difficultyLevel] && waveTimer <= 0f)
                    {
                        phase         = Phase.Attack;
                        forwardPosSet = false;
                    }
                    break;

                case Phase.Attack:
                    if (units < ArmyThreshold[difficultyLevel] / 2)
                        phase = Phase.Military;
                    break;
            }
        }

        private void DoEconomy()
        {
            if (!sawmillBuilt)
            {
                sawmillBuilt = TryBuildNow(BuildingType.Sawmill);
                TrainUnits(cap: NeutralRushSize[difficultyLevel]);
                return;
            }

            TrainUnits(cap: NeutralRushSize[difficultyLevel] + 2);

            if (CountIdleUnits() >= NeutralRushSize[difficultyLevel])
                RushNearestNeutral(NeutralRushSize[difficultyLevel]);

            if (HasCapturedNeutral() && !portBuilt && difficultyLevel >= 1)
                portBuilt = TryBuildNow(BuildingType.Port);

            if (difficultyLevel >= 2 && !castleBuilt && player.ownedCamps.Count >= 3)
                castleBuilt = TryBuildNow(BuildingType.Castle);
        }

        private void DoMilitary()
        {
            TrainUnits(cap: MaxPop[difficultyLevel]);

            if (!portBuilt && difficultyLevel >= 1 && HasCapturedNeutral())
                portBuilt = TryBuildNow(BuildingType.Port);
            if (difficultyLevel >= 2 && !castleBuilt && player.ownedCamps.Count >= 3)
                castleBuilt = TryBuildNow(BuildingType.Castle);

            if (NearestNeutral(80f) != null && CountIdleUnits() >= NeutralRushSize[difficultyLevel])
                RushNearestNeutral(NeutralRushSize[difficultyLevel]);

            if (!forwardPosSet)
                ComputeForwardPosition();

            if (patrolTimer <= 0f)
            {
                patrolTimer = 8f + Random.Range(-2f, 3f);
                AssignPatrol();
            }

            if (isIslandMap) ManageNavy();
        }

        private void DoAttack()
        {
            Camp target = BestEnemyCamp();
            if (target == null) { phase = Phase.Military; return; }

            var all = GetMovable();
            if (all.Count < 3) { phase = Phase.Military; return; }

            var melee   = new List<UnitMovement>();
            var ranged  = new List<UnitMovement>();
            var support = new List<UnitMovement>();

            foreach (var mov in all)
            {
                UnitStats us = mov.GetComponent<UnitStats>();
                if (us == null) { melee.Add(mov); continue; }
                if (IsSupportUnit(us))   support.Add(mov);
                else if (IsRangedUnit(us)) ranged.Add(mov);
                else                     melee.Add(mov);
            }

            Vector3 dest      = target.transform.position;
            Vector3 attackDir = Vector3.zero;
            if (player.ownedCamps.Count > 0)
                attackDir = (dest - player.ownedCamps[0].transform.position).normalized;

            Vector3 rangeBase   = dest - attackDir * 7f;
            Vector3 supportBase = dest - attackDir * 15f;

            for (int i = 0; i < melee.Count; i++)
            {
                melee[i].MoveTo(dest + FormOffset(i, melee.Count));
                melee[i].GetComponent<UnitAttack>()?.SetCampTarget(target);
            }

            for (int i = 0; i < ranged.Count; i++)
            {
                ranged[i].MoveTo(rangeBase + FormOffset(i, ranged.Count));
                ranged[i].GetComponent<UnitAttack>()?.SetCampTarget(target);
            }

            for (int i = 0; i < support.Count; i++)
            {
                support[i].MoveTo(supportBase + FormOffset(i, support.Count));
                support[i].GetComponent<UnitAttack>()?.SetCampTarget(target);
            }

            waveTimer = WaveCooldown[difficultyLevel];
            phase     = Phase.Military;

            if (isIslandMap) ManageNavy();
        }

        private void TrainUnits(int cap)
        {
            if (productionTimer > 0f) return;

            int pop    = CountUnits();
            int popCap = Mathf.Min(cap, player.ownedCamps.Count * 10);
            if (pop >= popCap) return;

            EnemyCompo comp   = difficultyLevel >= 2 ? ScanEnemy() : default;
            bool       trained = false;

            foreach (Camp camp in player.ownedCamps)
            {
                CampProduction prod = camp.GetComponent<CampProduction>();
                if (prod == null || prod.GetQueueCount() >= 1) continue;
                if (prod.Produce(PickUnit(camp, comp))) trained = true;
            }

            if (trained)
                productionTimer = ProdInterval[difficultyLevel] + Random.Range(-1f, 2f);
        }

        private UnitType PickUnit(Camp camp, EnemyCompo comp)
        {
            if (camp.campType == CampType.Port)
            {
                if (difficultyLevel < 2) return UnitType.Frigate;
                return Random.value < 0.4f ? UnitType.Destroyer : UnitType.Frigate;
            }

            if (camp.campType == CampType.Castle)
            {
                bool vsHeavy = comp.total > 0 && comp.heavy > comp.total * 0.3f;
                float r = Random.value;
                if (vsHeavy && r < 0.4f) return UnitType.AntiArmor;
                if (r < 0.30f) return UnitType.Heavy;
                if (r < 0.55f) return UnitType.Mortar;
                if (r < 0.75f) return UnitType.Heal;
                return UnitType.AntiArmor;
            }

            if (difficultyLevel == 0)
                return Random.value < 0.7f ? UnitType.Infantry : UnitType.Range;

            if (difficultyLevel == 1)
            {
                float r = Random.value;
                if (r < 0.40f) return UnitType.Infantry;
                if (r < 0.60f) return UnitType.Range;
                if (r < 0.75f) return UnitType.Heavy;
                return UnitType.Support;
            }

            bool needAA = comp.total > 0 && comp.heavy > comp.total * 0.25f;
            float r2 = Random.value;
            if (needAA && r2 < 0.30f) return UnitType.AntiArmor;
            if (r2 < 0.25f) return UnitType.Infantry;
            if (r2 < 0.45f) return UnitType.Range;
            if (r2 < 0.60f) return UnitType.Heavy;
            if (r2 < 0.75f) return UnitType.Heal;
            return UnitType.Support;
        }

        private void RushNearestNeutral(int unitCount)
        {
            Camp neutral = NearestNeutral(80f);
            if (neutral == null) return;

            var units = GetMovable();
            units.RemoveAll(IsBusy);
            units.Sort((a, b) =>
                Vector3.Distance(a.transform.position, neutral.transform.position)
                    .CompareTo(Vector3.Distance(b.transform.position, neutral.transform.position)));

            int count = Mathf.Min(unitCount, units.Count);
            for (int i = 0; i < count; i++)
            {
                units[i].MoveTo(neutral.transform.position + FormOffset(i, count));
                units[i].GetComponent<UnitAttack>()?.SetCampTarget(neutral);
            }
        }

        private void ComputeForwardPosition()
        {
            Camp enemy = BestEnemyCamp();
            if (enemy == null || player.ownedCamps.Count == 0) return;

            Vector3 basePos = player.ownedCamps[0].transform.position;
            Vector3 dir     = (enemy.transform.position - basePos).normalized;
            float   dist    = Vector3.Distance(basePos, enemy.transform.position);
            forwardPos    = basePos + dir * Mathf.Min(dist * 0.35f, 14f);
            forwardPosSet = true;
        }

        private void AssignPatrol()
        {
            if (!forwardPosSet) return;

            var movable   = GetMovable();
            int defenders = Mathf.Max(2, movable.Count / 3);
            int assigned  = 0;

            foreach (var mov in movable)
            {
                if (IsBusy(mov)) continue;
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

                if (assigned < defenders)
                {
                    float r = 3f + difficultyLevel;
                    mov.MoveTo(forwardPos + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r));
                    assigned++;
                }
                else
                {
                    Camp nearest = NearestOwnCamp(mov.transform.position);
                    if (nearest == null) continue;
                    mov.MoveTo(nearest.transform.position + new Vector3(Mathf.Cos(angle) * 3f, 0f, Mathf.Sin(angle) * 3f));
                }
            }
        }

        private bool RespondToThreat()
        {
            foreach (Camp camp in player.ownedCamps)
            {
                if (!CampUnderAttack(camp)) continue;
                var nearby = UnitsNear(camp.transform.position, 30f);
                for (int i = 0; i < nearby.Count; i++)
                {
                    nearby[i].MoveTo(camp.transform.position + FormOffset(i, nearby.Count));
                    nearby[i].GetComponent<UnitAttack>()?.ClearTargets();
                }
                return nearby.Count > 0;
            }
            return false;
        }

        private void ManageNavy()
        {
            foreach (UnitStats us in FindObjectsByType<UnitStats>(FindObjectsSortMode.None))
            {
                if (us.ownerId != player.playerId || us.unitType != UnitType.Transport) continue;
                TransportShip ship = us.GetComponent<TransportShip>();
                UnitMovement  mov  = us.GetComponent<UnitMovement>();
                if (ship == null || mov == null || mov.IsMoving) continue;

                if (!ship.IsEmpty)
                {
                    Camp target = BestEnemyCamp();
                    if (target == null) continue;
                    Vector3 land = NearWalkable(target.transform.position, 12f);
                    if (land == Vector3.zero) continue;
                    if (Vector3.Distance(us.transform.position, land) <= 6f)
                        ship.DisembarkAll(land);
                    else
                        mov.MoveTo(land);
                }
                else
                {
                    Camp port = MyPort();
                    if (port != null) mov.MoveTo(port.transform.position);
                }
            }
        }

        private bool TryBuildNow(BuildingType type)
        {
            if (BuildingManager.Instance == null) return false;
            bool needWater = (type == BuildingType.Port);
            foreach (Camp camp in player.ownedCamps)
            {
                HexTile tile = FreeTile(camp.transform.position, 16f, needWater);
                if (tile == null) continue;
                if (BuildingManager.Instance.TryBuildForAI(tile, type, player))
                    return true;
            }
            return false;
        }

        private bool HasCapturedNeutral()
        {
            foreach (Camp c in player.ownedCamps)
                if (c.campType == CampType.NeutralSpecial) return true;
            return false;
        }

        private Camp BestEnemyCamp()
        {
            if (player.ownedCamps.Count == 0) return null;
            Vector3 origin = player.ownedCamps[0].transform.position;
            Camp best = null; float bestDist = float.MaxValue;
            foreach (Camp c in FindObjectsByType<Camp>(FindObjectsSortMode.None))
            {
                if (c.isNeutral || c.owner == player) continue;
                float d = Vector3.Distance(c.transform.position, origin);
                if (d < bestDist) { bestDist = d; best = c; }
            }
            return best;
        }

        private Camp NearestNeutral(float maxDist)
        {
            if (player.ownedCamps.Count == 0) return null;
            Vector3 origin = player.ownedCamps[0].transform.position;
            Camp nearest = null; float best = maxDist;
            foreach (Camp c in FindObjectsByType<Camp>(FindObjectsSortMode.None))
            {
                if (!c.isNeutral) continue;
                float d = Vector3.Distance(c.transform.position, origin);
                if (d < best) { best = d; nearest = c; }
            }
            return nearest;
        }

        private Camp NearestOwnCamp(Vector3 pos)
        {
            Camp best = null; float d = float.MaxValue;
            foreach (Camp c in player.ownedCamps)
            {
                float dist = Vector3.Distance(c.transform.position, pos);
                if (dist < d) { d = dist; best = c; }
            }
            return best;
        }

        private Camp MyPort()
        {
            foreach (Camp c in player.ownedCamps)
                if (c.campType == CampType.Port) return c;
            return null;
        }

        private List<UnitMovement> GetMovable()
        {
            var list = new List<UnitMovement>();
            foreach (UnitStats us in FindObjectsByType<UnitStats>(FindObjectsSortMode.None))
            {
                if (us.ownerId != player.playerId) continue;
                if (!us.gameObject.activeInHierarchy || us.currentHealth <= 0) continue;
                UnitMovement mov = us.GetComponent<UnitMovement>();
                if (mov != null && !mov.IsLocked) list.Add(mov);
            }
            return list;
        }

        private int CountIdleUnits()
        {
            int n = 0;
            foreach (var mov in GetMovable())
                if (!IsBusy(mov)) n++;
            return n;
        }

        private List<UnitMovement> UnitsNear(Vector3 pos, float radius)
        {
            var list = new List<UnitMovement>();
            foreach (Collider c in Physics.OverlapSphere(pos, radius))
            {
                UnitStats us = c.GetComponentInParent<UnitStats>();
                if (us == null || us.ownerId != player.playerId || us.currentHealth <= 0) continue;
                UnitMovement mov = us.GetComponent<UnitMovement>();
                if (mov != null && !mov.IsLocked && !list.Contains(mov)) list.Add(mov);
            }
            return list;
        }

        private bool IsBusy(UnitMovement mov)
        {
            if (mov.IsMoving) return true;
            UnitAttack atk = mov.GetComponent<UnitAttack>();
            return atk != null && (atk.CurrentTarget != null || atk.CurrentCampTarget != null);
        }

        private bool CampUnderAttack(Camp camp)
        {
            foreach (Collider c in Physics.OverlapSphere(camp.transform.position, 10f))
            {
                UnitStats us = c.GetComponentInParent<UnitStats>();
                if (us != null && us.ownerId != player.playerId
                    && us.ownerId != GameConstants.NEUTRAL_ID && us.currentHealth > 0)
                    return true;
            }
            return false;
        }

        private int CountUnits()
        {
            int n = 0;
            foreach (UnitStats us in FindObjectsByType<UnitStats>(FindObjectsSortMode.None))
                if (us.ownerId == player.playerId && us.gameObject.activeInHierarchy && us.currentHealth > 0)
                    n++;
            return n;
        }

        private HexTile FreeTile(Vector3 center, float radius, bool needWater)
        {
            HexTile best = null; float d = float.MaxValue;
            foreach (Collider c in Physics.OverlapSphere(center, radius))
            {
                HexTile t = c.GetComponentInParent<HexTile>();
                if (t == null || t.isOccupied || t.terrain != HexTerrain.Walkable) continue;
                if (needWater && !HasWaterNeighbor(t)) continue;

                float xz = Mathf.Sqrt(
                    (t.transform.position.x - center.x) * (t.transform.position.x - center.x) +
                    (t.transform.position.z - center.z) * (t.transform.position.z - center.z));
                if (xz < 0.05f) continue;

                float dist = Vector3.Distance(center, t.transform.position);
                if (dist < d) { d = dist; best = t; }
            }
            return best;
        }

        private static bool HasWaterNeighbor(HexTile tile)
        {
            foreach (Collider c in Physics.OverlapSphere(tile.transform.position, 3f))
            {
                HexTile nb = c.GetComponentInParent<HexTile>();
                if (nb != null && nb != tile && nb.terrain == HexTerrain.Water) return true;
            }
            return false;
        }

        private static Vector3 NearWalkable(Vector3 center, float radius)
        {
            float best = float.MaxValue; Vector3 result = Vector3.zero;
            foreach (Collider c in Physics.OverlapSphere(center, radius))
            {
                HexTile t = c.GetComponentInParent<HexTile>();
                if (t == null || t.terrain != HexTerrain.Walkable) continue;
                float d = Vector3.Distance(center, t.transform.position);
                if (d < best) { best = d; result = t.transform.position; }
            }
            return result;
        }

        private static Vector3 FormOffset(int i, int count)
        {
            if (count <= 1) return Vector3.zero;
            float r = 1.5f + count * 0.25f;
            float a = i * (360f / count) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }

        private static bool IsSupportUnit(UnitStats us) =>
            us.unitType == UnitType.Heal || us.unitType == UnitType.Support;

        private static bool IsRangedUnit(UnitStats us) =>
            us.unitType == UnitType.Range   || us.unitType == UnitType.Mortar    ||
            us.unitType == UnitType.AntiArmor || us.unitType == UnitType.Frigate ||
            us.unitType == UnitType.Destroyer;

        private struct EnemyCompo { public int heavy, total; }

        private EnemyCompo ScanEnemy()
        {
            EnemyCompo comp = default;
            foreach (UnitStats us in FindObjectsByType<UnitStats>(FindObjectsSortMode.None))
            {
                if (us.ownerId == player.playerId || us.ownerId == GameConstants.NEUTRAL_ID) continue;
                comp.total++;
                if (us.unitType == UnitType.Heavy) comp.heavy++;
            }
            return comp;
        }

        private void OnDrawGizmosSelected()
        {
            if (player == null) return;
            foreach (Camp c in player.ownedCamps)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(c.transform.position, 10f);
            }
            if (forwardPosSet)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(forwardPos, 3f + difficultyLevel);
            }
        }
    }
}
