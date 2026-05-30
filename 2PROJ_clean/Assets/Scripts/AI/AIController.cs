using System.Collections.Generic;
using UnityEngine;

namespace SupKonQuest
{
    public class AIController : MonoBehaviour
    {
        [Header("Player")]
        public PlayerData player;

        [Header("Difficulty")]
        [Range(0, 2)] public int difficultyLevel = 0;

        private static readonly float[] ThinkDelay      = { 6.0f, 3.0f, 1.5f };
        private static readonly int[]   MaxUnits        = { 8,   16,   26   };
        private static readonly int[]   MaxQueue        = { 1,    2,    4   };
        private static readonly int[]   AttackBase      = { 8,    6,    4   };
        private static readonly float[] AttackCooldown  = { 40f, 25f, 14f  };
        private static readonly int[]   MaxSawmills     = { 0,    1,    2   };
        private static readonly float[] EconomyMinTime  = { 999f, 60f, 40f };

        private float thinkTimer;
        private float attackCooldownLeft;
        private float economyTimer;
        private GameManager gameManager;
        private bool isIslandMap;

        private enum Phase { Economy, Military, Attack }
        private Phase phase = Phase.Economy;

        private void Start()
        {
            gameManager  = GameManager.Instance;
            thinkTimer   = Random.Range(0f, ThinkDelay[difficultyLevel]);
            isIslandMap  = PlayerPrefs.GetInt("MapType", 0) == (int)MapType.Island;
        }

        private void Update()
        {
            if (gameManager == null || gameManager.gameOver || player == null || player.eliminated) return;
            if (attackCooldownLeft > 0f) attackCooldownLeft -= Time.deltaTime;
            if (phase == Phase.Economy) economyTimer += Time.deltaTime;

            thinkTimer -= Time.deltaTime;
            if (thinkTimer > 0f) return;
            thinkTimer = ThinkDelay[difficultyLevel] + Random.Range(-0.4f, 0.6f);

            Think();
        }

        private void Think()
        {
            if (difficultyLevel >= 1 && CheckAndDefend()) return;

            UpdatePhase();
            ProduceUnits();

            switch (phase)
            {
                case Phase.Economy:
                    RunEconomy();
                    break;
                case Phase.Military:
                    RunMilitary();
                    break;
                case Phase.Attack:
                    RunAttack();
                    break;
            }

            if (isIslandMap && difficultyLevel >= 1)
                ManageTransports();
        }

        private void UpdatePhase()
        {
            int units = CountMyUnits();
            int threshold = AttackBase[difficultyLevel] + player.ownedCamps.Count;

            if (phase == Phase.Economy)
            {
                bool timerOk  = economyTimer >= EconomyMinTime[difficultyLevel];
                bool hasSaw   = CountOwnedSawmills() > 0;
                bool ready = difficultyLevel == 0 || (timerOk && hasSaw) || timerOk;
                if (ready) phase = Phase.Military;
            }
            else if (phase == Phase.Military)
            {
                if (units >= threshold && attackCooldownLeft <= 0f)
                    phase = Phase.Attack;
            }
        }

        private void RunEconomy()
        {
            if (difficultyLevel >= 1) TryBuildSawmill();
            if (difficultyLevel >= 1) TryBuildPort();
            TryCaptureNeutral(minUnits: 3, maxDistToNeutral: 18f);
        }

        private void RunMilitary()
        {
            if (difficultyLevel >= 1) TryBuildSawmill();
            if (difficultyLevel >= 1) TryBuildPort();
            if (difficultyLevel >= 2) TryBuildCastle();
            int surplus = CountMyUnits() - (AttackBase[difficultyLevel] + player.ownedCamps.Count - 2);
            if (surplus >= 3) TryCaptureNeutral(minUnits: 4, maxDistToNeutral: 25f);
        }

        private void RunAttack()
        {
            Camp target = FindBestAttackTarget();
            if (target == null) { phase = Phase.Military; return; }

            List<UnitMovement> units = GetMyMovableUnits();
            if (units.Count < AttackBase[difficultyLevel]) { phase = Phase.Military; return; }

            List<UnitMovement> attackers = difficultyLevel == 2
                ? RemoveDefenders(units)
                : units;

            if (attackers.Count == 0) { phase = Phase.Military; return; }

            Vector3 dest = target.transform.position;
            for (int i = 0; i < attackers.Count; i++)
            {
                attackers[i].MoveTo(dest + FormationOffset(i, attackers.Count));
                attackers[i].GetComponent<UnitAttack>()?.SetCampTarget(target);
            }

            attackCooldownLeft = AttackCooldown[difficultyLevel];
            phase = Phase.Military;
        }

        private bool CheckAndDefend()
        {
            foreach (Camp camp in player.ownedCamps)
            {
                if (!IsCampUnderAttack(camp)) continue;

                List<UnitMovement> units = GetMyMovableUnits();
                foreach (UnitMovement u in units)
                {
                    if (Vector3.Distance(u.transform.position, camp.transform.position) > 30f) continue;
                    u.MoveTo(camp.transform.position + FormationOffset(units.IndexOf(u), units.Count));
                }
                return true;
            }
            return false;
        }

        private bool IsCampUnderAttack(Camp camp)
        {
            Collider[] hits = Physics.OverlapSphere(camp.transform.position, 8f);
            foreach (Collider c in hits)
            {
                UnitStats us = c.GetComponent<UnitStats>();
                if (us != null && us.ownerId != player.playerId && us.ownerId != 0)
                    return true;
            }
            return false;
        }

        private void TryCaptureNeutral(int minUnits, float maxDistToNeutral)
        {
            List<UnitMovement> myUnits = GetMyMovableUnits();
            if (myUnits.Count < minUnits) return;

            Camp target = FindNearestNeutralCamp(maxDistToNeutral);
            if (target == null) return;

            myUnits.Sort((a, b) =>
                Vector3.Distance(a.transform.position, target.transform.position)
                    .CompareTo(Vector3.Distance(b.transform.position, target.transform.position)));

            int sendCount = Mathf.Min(minUnits + 1, myUnits.Count);
            Vector3 dest  = target.transform.position;
            for (int i = 0; i < sendCount; i++)
            {
                myUnits[i].MoveTo(dest + FormationOffset(i, sendCount));
                myUnits[i].GetComponent<UnitAttack>()?.SetCampTarget(target);
            }
        }

        private Camp FindNearestNeutralCamp(float maxDist)
        {
            if (player.ownedCamps.Count == 0) return null;
            Vector3 refPos = player.ownedCamps[0].transform.position;

            Camp[] allCamps = FindObjectsByType<Camp>(FindObjectsSortMode.None);
            Camp nearest = null;
            float nearestDist = maxDist;
            foreach (Camp c in allCamps)
            {
                if (!c.isNeutral) continue;
                float d = Vector3.Distance(c.transform.position, refPos);
                if (d < nearestDist) { nearestDist = d; nearest = c; }
            }
            return nearest;
        }

        private void TryBuildSawmill()
        {
            if (BuildingManager.Instance == null) return;
            if (CountOwnedSawmills() >= MaxSawmills[difficultyLevel]) return;

            foreach (Camp camp in player.ownedCamps)
            {
                HexTile tile = FindFreeTileNear(camp.transform.position, 10f, requireWaterNeighbor: false);
                if (tile != null && BuildingManager.Instance.TryBuild(tile, BuildingType.Sawmill, player))
                    return;
            }
        }

        private void TryBuildCastle()
        {
            if (BuildingManager.Instance == null) return;
            if (player.ownedCamps.Count < 2) return;
            foreach (Camp c in player.ownedCamps)
                if (c.campType == CampType.Castle) return;

            foreach (Camp camp in player.ownedCamps)
            {
                HexTile tile = FindFreeTileNear(camp.transform.position, 10f, requireWaterNeighbor: false);
                if (tile != null && BuildingManager.Instance.TryBuild(tile, BuildingType.Castle, player))
                    return;
            }
        }

        private void TryBuildPort()
        {
            if (BuildingManager.Instance == null) return;
            foreach (Camp c in player.ownedCamps)
                if (c.campType == CampType.Port) return;

            foreach (Camp camp in player.ownedCamps)
            {
                HexTile tile = FindFreeTileNear(camp.transform.position, 22f, requireWaterNeighbor: true);
                if (tile != null && BuildingManager.Instance.TryBuild(tile, BuildingType.Port, player))
                    return;
            }
        }

        private void ProduceUnits()
        {
            int popCap = player.ownedCamps.Count * 10;
            if (CountMyUnits() >= Mathf.Min(MaxUnits[difficultyLevel], popCap)) return;

            EnemyCompo comp = difficultyLevel >= 2 ? AnalyzeEnemyCompo() : default;

            foreach (Camp camp in player.ownedCamps)
            {
                CampProduction prod = camp.GetComponent<CampProduction>();
                if (prod == null || prod.GetQueueCount() >= MaxQueue[difficultyLevel]) continue;
                prod.Produce(ChooseUnit(camp, comp));
            }
        }

        private struct EnemyCompo
        {
            public int heavy, total;
        }

        private EnemyCompo AnalyzeEnemyCompo()
        {
            EnemyCompo c = default;
            UnitStats[] all = FindObjectsByType<UnitStats>(FindObjectsSortMode.None);
            foreach (UnitStats us in all)
            {
                if (us.ownerId == player.playerId || us.ownerId == 0) continue;
                c.total++;
                if (us.unitType == UnitType.Heavy) c.heavy++;
            }
            return c;
        }

        private UnitType ChooseUnit(Camp camp, EnemyCompo comp)
        {
            if (camp.campType == CampType.Port)
            {
                if (difficultyLevel < 2) return UnitType.Frigate;
                float r = Random.value;
                if (isIslandMap && r < 0.30f) return UnitType.Transport;
                if (r < 0.65f) return UnitType.Frigate;
                return UnitType.Destroyer;
            }

            if (camp.campType == CampType.Castle)
            {
                bool manyHeavies = comp.total > 0 && comp.heavy > comp.total * 0.3f;
                float r = Random.value;
                if (difficultyLevel == 2 && manyHeavies)
                    return r < 0.50f ? UnitType.AntiArmor : UnitType.Mortar;
                if (r < 0.35f) return UnitType.AntiArmor;
                if (r < 0.65f) return UnitType.Mortar;
                if (r < 0.82f) return UnitType.Support;
                return UnitType.Heal;
            }

            if (difficultyLevel == 0)
                return Random.value < 0.20f ? UnitType.Range : UnitType.Infantry;

            if (difficultyLevel == 1)
            {
                float rv = Random.value;
                if (rv < 0.40f) return UnitType.Infantry;
                if (rv < 0.62f) return UnitType.Range;
                if (rv < 0.78f) return UnitType.Heavy;
                return UnitType.Heal;
            }

            bool needAntiArmor = comp.total > 0 && comp.heavy > comp.total * 0.25f;
            float r2 = Random.value;
            if (needAntiArmor && r2 < 0.30f) return UnitType.AntiArmor;
            if (r2 < 0.25f) return UnitType.Infantry;
            if (r2 < 0.45f) return UnitType.Range;
            if (r2 < 0.60f) return UnitType.Heavy;
            if (r2 < 0.72f) return UnitType.AntiArmor;
            if (r2 < 0.82f) return UnitType.Heal;
            return UnitType.Support;
        }

        private void ManageTransports()
        {
            UnitStats[] all = FindObjectsByType<UnitStats>(FindObjectsSortMode.None);
            foreach (UnitStats us in all)
            {
                if (us.ownerId != player.playerId || us.unitType != UnitType.Transport) continue;
                TransportShip ship = us.GetComponent<TransportShip>();
                UnitMovement  mov  = us.GetComponent<UnitMovement>();
                if (ship == null || mov == null) continue;

                if (!ship.IsEmpty)
                {
                    Camp target = FindBestAttackTarget();
                    if (target == null) continue;

                    Vector3 landing = FindNearWalkable(target.transform.position, 12f);
                    if (landing == Vector3.zero) continue;

                    if (Vector3.Distance(us.transform.position, landing) <= 6f)
                        ship.DisembarkAll(landing);
                    else
                        mov.MoveTo(landing);
                }
                else
                {
                    Camp port = GetMyPort();
                    if (port != null) mov.MoveTo(port.transform.position);
                }
            }
        }

        private Camp GetMyPort()
        {
            foreach (Camp c in player.ownedCamps)
                if (c.campType == CampType.Port) return c;
            return null;
        }

        private Camp FindBestAttackTarget()
        {
            if (player.ownedCamps.Count == 0) return null;
            Vector3 refPos = player.ownedCamps[0].transform.position;

            Camp[] allCamps = FindObjectsByType<Camp>(FindObjectsSortMode.None);
            Camp best = null;
            float bestScore = float.MaxValue;

            foreach (Camp camp in allCamps)
            {
                if (camp.owner == player) continue;
                float dist = Vector3.Distance(camp.transform.position, refPos);

                if (difficultyLevel >= 1 && camp.isNeutral) dist *= 0.55f;
                if (difficultyLevel == 2 && !camp.isNeutral) dist *= 1.20f;
                if (difficultyLevel == 0 && Random.value < 0.25f) dist *= Random.Range(0.5f, 2f);

                if (dist < bestScore) { bestScore = dist; best = camp; }
            }
            return best;
        }

        private List<UnitMovement> GetMyMovableUnits()
        {
            UnitStats[] all = FindObjectsByType<UnitStats>(FindObjectsSortMode.None);
            var result = new List<UnitMovement>();
            foreach (UnitStats us in all)
            {
                if (us.ownerId != player.playerId) continue;
                if (!us.gameObject.activeInHierarchy) continue;
                UnitMovement mov = us.GetComponent<UnitMovement>();
                if (mov != null) result.Add(mov);
            }
            return result;
        }

        private int CountMyUnits()
        {
            UnitStats[] all = FindObjectsByType<UnitStats>(FindObjectsSortMode.None);
            int count = 0;
            foreach (UnitStats us in all)
                if (us.ownerId == player.playerId && us.gameObject.activeInHierarchy) count++;
            return count;
        }

        private List<UnitMovement> RemoveDefenders(List<UnitMovement> units)
        {
            var defended  = new HashSet<Camp>();
            var attackers = new List<UnitMovement>(units);
            foreach (UnitMovement u in units)
            {
                Camp nearest = NearestOwnCamp(u.transform.position);
                if (nearest != null && !defended.Contains(nearest))
                {
                    defended.Add(nearest);
                    attackers.Remove(u);
                }
            }
            return attackers;
        }

        private Camp NearestOwnCamp(Vector3 pos)
        {
            Camp nearest = null;
            float best = float.MaxValue;
            foreach (Camp c in player.ownedCamps)
            {
                float d = Vector3.Distance(c.transform.position, pos);
                if (d < best) { best = d; nearest = c; }
            }
            return nearest;
        }

        private int CountOwnedSawmills()
        {
            Sawmill[] saws = FindObjectsByType<Sawmill>(FindObjectsSortMode.None);
            int count = 0;
            foreach (Sawmill s in saws)
                if (s.owner == player) count++;
            return count;
        }

        private HexTile FindFreeTileNear(Vector3 center, float radius, bool requireWaterNeighbor)
        {
            Collider[] hits = Physics.OverlapSphere(center, radius);
            float best = float.MaxValue;
            HexTile bestTile = null;
            foreach (Collider c in hits)
            {
                HexTile t = c.GetComponentInParent<HexTile>();
                if (t == null || t.isOccupied || t.terrain != HexTerrain.Walkable) continue;
                if (requireWaterNeighbor && !HasWaterNeighbor(t)) continue;
                float d = Vector3.Distance(center, t.transform.position);
                if (d < best) { best = d; bestTile = t; }
            }
            return bestTile;
        }

        private static bool HasWaterNeighbor(HexTile tile)
        {
            Collider[] hits = Physics.OverlapSphere(tile.transform.position, 3f);
            foreach (Collider c in hits)
            {
                HexTile nb = c.GetComponentInParent<HexTile>();
                if (nb != null && nb != tile && nb.terrain == HexTerrain.Water) return true;
            }
            return false;
        }

        private static Vector3 FindNearWalkable(Vector3 center, float radius)
        {
            Collider[] hits = Physics.OverlapSphere(center, radius);
            float best = float.MaxValue;
            Vector3 result = Vector3.zero;
            foreach (Collider c in hits)
            {
                HexTile tile = c.GetComponentInParent<HexTile>();
                if (tile == null || tile.terrain != HexTerrain.Walkable) continue;
                float d = Vector3.Distance(center, tile.transform.position);
                if (d < best) { best = d; result = tile.transform.position; }
            }
            return result;
        }

        private static Vector3 FormationOffset(int i, int count)
        {
            if (count <= 1) return Vector3.zero;
            float radius = 1.5f + count * 0.25f;
            float angle  = i * (360f / count) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }
    }
}
