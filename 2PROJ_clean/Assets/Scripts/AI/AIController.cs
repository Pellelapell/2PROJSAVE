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

        private float thinkTimer;
        private GameManager gameManager;

        private static readonly int[]   attackThreshold = { 4, 3, 2 };
        private static readonly int[]   maxQueue        = { 2, 3, 4 };
        private static readonly int[]   maxUnits        = { 8, 14, 20 };
        private static readonly float[] thinkDelay      = { 4f, 2.5f, 1.5f };

        private void Start()
        {
            gameManager = GameManager.Instance;
            thinkTimer  = Random.Range(0f, thinkDelay[difficultyLevel]);
        }

        private void Update()
        {
            if (gameManager == null || gameManager.gameOver || player == null || player.eliminated) return;

            thinkTimer -= Time.deltaTime;
            if (thinkTimer > 0f) return;
            thinkTimer = thinkDelay[difficultyLevel];

            TryBuildSawmill();
            TryBuildCastle();
            ProduceUnits();
            GiveOrders();
        }

        // ── Construction ─────────────────────────────────────────────

        private void TryBuildSawmill()
        {
            if (BuildingManager.Instance == null) return;

            int sawCount = CountOwnedSawmills();
            int maxSaws  = 1 + difficultyLevel;
            if (sawCount >= maxSaws) return;

            foreach (Camp camp in player.ownedCamps)
            {
                HexTile tile = FindFreeTileNear(camp.transform.position, 6f);
                if (tile != null && BuildingManager.Instance.TryBuild(tile, BuildingType.Sawmill, player))
                    return;
            }
        }

        private void TryBuildCastle()
        {
            if (difficultyLevel < 2) return;
            if (BuildingManager.Instance == null) return;

            // Max 1 château, nécessite au moins 2 camps d'abord
            if (player.ownedCamps.Count < 2) return;
            int castleCount = 0;
            foreach (Camp c in player.ownedCamps)
                if (c.campType == CampType.Castle) castleCount++;
            if (castleCount >= 1) return;

            foreach (Camp camp in player.ownedCamps)
            {
                HexTile tile = FindFreeTileNear(camp.transform.position, 6f);
                if (tile != null && BuildingManager.Instance.TryBuild(tile, BuildingType.Castle, player))
                    return;
            }
        }

        private int CountOwnedSawmills()
        {
            Sawmill[] saws = FindObjectsByType<Sawmill>(FindObjectsSortMode.None);
            int count = 0;
            foreach (Sawmill s in saws) if (s.owner == player) count++;
            return count;
        }

        private HexTile FindFreeTileNear(Vector3 center, float radius)
        {
            Collider[] hits = Physics.OverlapSphere(center, radius);
            foreach (Collider c in hits)
            {
                HexTile t = c.GetComponentInParent<HexTile>();
                if (t != null && !t.isOccupied && t.terrain == HexTerrain.Walkable)
                    return t;
            }
            return null;
        }

        // ── Production d'unités ───────────────────────────────────────

        private void ProduceUnits()
        {
            int currentUnitCount = GetMyUnits().Count;
            if (currentUnitCount >= maxUnits[difficultyLevel]) return;

            foreach (Camp camp in player.ownedCamps)
            {
                CampProduction prod = camp.GetComponent<CampProduction>();
                if (prod == null) continue;
                if (prod.GetQueueCount() >= maxQueue[difficultyLevel]) continue;
                prod.Produce(ChooseUnit(camp));
            }
        }

        private UnitType ChooseUnit(Camp camp)
        {
            if (camp.campType == CampType.Port) return UnitType.Frigate;

            if (camp.campType == CampType.Castle)
            {
                float r = Random.value;
                if (r < 0.4f) return UnitType.AntiArmor;
                if (r < 0.7f) return UnitType.Mortar;
                return UnitType.Support;
            }

            if (difficultyLevel == 0) return UnitType.Infantry;

            float rv = Random.value;
            if (difficultyLevel == 1)
            {
                if (rv < 0.5f)  return UnitType.Infantry;
                if (rv < 0.75f) return UnitType.Range;
                return UnitType.Heavy;
            }

            if (rv < 0.35f) return UnitType.Infantry;
            if (rv < 0.55f) return UnitType.Range;
            if (rv < 0.70f) return UnitType.Heavy;
            if (rv < 0.85f) return UnitType.AntiArmor;
            return UnitType.Mortar;
        }

        // ── Ordres d'attaque ──────────────────────────────────────────

        private void GiveOrders()
        {
            Camp target = FindAttackTarget();
            if (target == null) return;

            List<UnitMovement> myUnits = GetMyUnits();
            if (myUnits.Count < attackThreshold[difficultyLevel]) return;

            if (difficultyLevel == 2 && player.ownedCamps.Count > 0)
            {
                myUnits = FilterDefenders(myUnits);
                if (myUnits.Count == 0) return;
            }

            Vector3 dest  = target.transform.position;
            int count = myUnits.Count;
            for (int i = 0; i < count; i++)
            {
                myUnits[i].MoveTo(dest + FormationOffset(i, count));
                UnitAttack atk = myUnits[i].GetComponent<UnitAttack>();
                atk?.SetCampTarget(target);
            }
        }

        private List<UnitMovement> GetMyUnits()
        {
            UnitStats[] all = FindObjectsByType<UnitStats>(FindObjectsSortMode.None);
            List<UnitMovement> result = new List<UnitMovement>();
            foreach (UnitStats unit in all)
            {
                if (unit.ownerId != player.playerId) continue;
                UnitMovement mov = unit.GetComponent<UnitMovement>();
                if (mov != null) result.Add(mov);
            }
            return result;
        }

        private List<UnitMovement> FilterDefenders(List<UnitMovement> units)
        {
            HashSet<Camp> defended = new HashSet<Camp>();
            List<UnitMovement> attackers = new List<UnitMovement>(units);
            foreach (UnitMovement unit in units)
            {
                Camp nearest = FindNearestOwnCamp(unit.transform.position);
                if (nearest != null && !defended.Contains(nearest))
                {
                    defended.Add(nearest);
                    attackers.Remove(unit);
                }
            }
            return attackers;
        }

        private Camp FindAttackTarget()
        {
            if (player.ownedCamps.Count == 0) return null;

            Camp nearest = null;
            float nearestScore = float.MaxValue;
            Vector3 refPos = player.ownedCamps[0].transform.position;

            Camp[] allCamps = FindObjectsByType<Camp>(FindObjectsSortMode.None);
            foreach (Camp camp in allCamps)
            {
                if (camp.owner == player) continue;

                float dist = Vector3.Distance(camp.transform.position, refPos);

                if (difficultyLevel >= 1 && camp.isNeutral) dist *= 0.7f;
                if (difficultyLevel == 2 && !camp.isNeutral) dist *= 1.2f;

                if (dist < nearestScore) { nearestScore = dist; nearest = camp; }
            }
            return nearest;
        }

        private Camp FindNearestOwnCamp(Vector3 pos)
        {
            Camp nearest = null;
            float nearestDist = float.MaxValue;
            foreach (Camp camp in player.ownedCamps)
            {
                float dist = Vector3.Distance(camp.transform.position, pos);
                if (dist < nearestDist) { nearestDist = dist; nearest = camp; }
            }
            return nearest;
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
