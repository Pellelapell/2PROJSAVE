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

        // ── Paramètres par difficulté ────────────────────────────────
        // 0=Facile  1=Moyen  2=Difficile
        private static readonly int[]   attackThreshold = { 5, 3, 2 };
        private static readonly int[]   maxQueue        = { 1, 3, 5 };
        private static readonly int[]   maxUnits        = { 6, 14, 22 };
        private static readonly float[] thinkDelay      = { 5f, 2.5f, 1.2f };

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

            // Facile : pas de bâtiments, attaque naïve
            // Moyen  : scierie seulement
            // Difficile : tout construit
            if (difficultyLevel >= 1) TryBuildSawmill();
            if (difficultyLevel >= 2) TryBuildCastle();
            if (difficultyLevel >= 2) TryBuildPort();

            ProduceUnits();
            GiveOrders();
        }

        // ── Construction ─────────────────────────────────────────────

        private void TryBuildSawmill()
        {
            if (BuildingManager.Instance == null) return;

            int maxSaws = difficultyLevel; // moyen=1, difficile=2
            if (CountOwnedSawmills() >= maxSaws) return;

            foreach (Camp camp in player.ownedCamps)
            {
                HexTile tile = FindFreeTileNear(camp.transform.position, 8f, false);
                if (tile != null && BuildingManager.Instance.TryBuild(tile, BuildingType.Sawmill, player))
                {
                    SendInfantryTo(tile.transform.position);
                    return;
                }
            }
        }

        private void TryBuildCastle()
        {
            if (BuildingManager.Instance == null) return;
            if (player.ownedCamps.Count < 2) return;

            foreach (Camp c in player.ownedCamps)
                if (c.campType == CampType.Castle) return; // déjà un château

            foreach (Camp camp in player.ownedCamps)
            {
                HexTile tile = FindFreeTileNear(camp.transform.position, 8f, false);
                if (tile != null && BuildingManager.Instance.TryBuild(tile, BuildingType.Castle, player))
                {
                    SendInfantryTo(tile.transform.position);
                    return;
                }
            }
        }

        private void TryBuildPort()
        {
            if (BuildingManager.Instance == null) return;

            foreach (Camp c in player.ownedCamps)
                if (c.campType == CampType.Port) return; // déjà un port

            // Chercher une case côtière dans un grand rayon
            foreach (Camp camp in player.ownedCamps)
            {
                HexTile tile = FindFreeTileNear(camp.transform.position, 20f, requireWaterNeighbor: true);
                if (tile != null && BuildingManager.Instance.TryBuild(tile, BuildingType.Port, player))
                {
                    SendInfantryTo(tile.transform.position);
                    return;
                }
            }
        }

        private void SendInfantryTo(Vector3 pos)
        {
            UnitStats[] all = FindObjectsByType<UnitStats>(FindObjectsSortMode.None);
            float best = float.MaxValue;
            UnitMovement bestMov = null;

            foreach (UnitStats us in all)
            {
                if (us.ownerId != player.playerId || us.unitType != UnitType.Infantry) continue;
                float d = Vector3.Distance(us.transform.position, pos);
                if (d < best) { best = d; bestMov = us.GetComponent<UnitMovement>(); }
            }
            bestMov?.MoveTo(pos);
        }

        private int CountOwnedSawmills()
        {
            Sawmill[] saws = FindObjectsByType<Sawmill>(FindObjectsSortMode.None);
            int count = 0;
            foreach (Sawmill s in saws) if (s.owner == player) count++;
            return count;
        }

        private HexTile FindFreeTileNear(Vector3 center, float radius, bool requireWaterNeighbor)
        {
            Collider[] hits = Physics.OverlapSphere(center, radius);
            foreach (Collider c in hits)
            {
                HexTile t = c.GetComponentInParent<HexTile>();
                if (t == null || t.isOccupied || t.terrain != HexTerrain.Walkable) continue;
                if (requireWaterNeighbor && !HasWaterNeighbor(t)) continue;
                return t;
            }
            return null;
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
            if (camp.campType == CampType.Port)
            {
                // Difficile : mix naval ; sinon juste frégate
                if (difficultyLevel < 2) return UnitType.Frigate;
                return Random.value < 0.6f ? UnitType.Frigate : UnitType.Destroyer;
            }

            if (camp.campType == CampType.Castle)
            {
                float r = Random.value;
                if (r < 0.4f) return UnitType.AntiArmor;
                if (r < 0.7f) return UnitType.Mortar;
                return UnitType.Support;
            }

            // Camp normal
            if (difficultyLevel == 0)
            {
                // Facile : seulement infanterie avec erreurs fréquentes
                return Random.value < 0.15f ? UnitType.Range : UnitType.Infantry;
            }

            if (difficultyLevel == 1)
            {
                float rv = Random.value;
                if (rv < 0.45f) return UnitType.Infantry;
                if (rv < 0.70f) return UnitType.Range;
                if (rv < 0.85f) return UnitType.Heavy;
                return UnitType.Heal;
            }

            // Difficile : mix complet
            float r2 = Random.value;
            if (r2 < 0.25f) return UnitType.Infantry;
            if (r2 < 0.45f) return UnitType.Range;
            if (r2 < 0.60f) return UnitType.Heavy;
            if (r2 < 0.72f) return UnitType.AntiArmor;
            if (r2 < 0.82f) return UnitType.Mortar;
            if (r2 < 0.90f) return UnitType.Heal;
            return UnitType.Support;
        }

        // ── Ordres d'attaque ──────────────────────────────────────────

        private void GiveOrders()
        {
            Camp target = FindAttackTarget();
            if (target == null) return;

            List<UnitMovement> myUnits = GetMyUnits();
            if (myUnits.Count < attackThreshold[difficultyLevel]) return;

            // Difficile : garde un défenseur par camp
            if (difficultyLevel == 2 && player.ownedCamps.Count > 0)
            {
                myUnits = FilterDefenders(myUnits);
                if (myUnits.Count == 0) return;
            }

            // Facile : chance de cibler aléatoirement (joue mal)
            if (difficultyLevel == 0 && Random.value < 0.3f)
                target = FindRandomEnemyCamp();

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

                // Moyen+ préfère les camps neutres (plus facile à capturer)
                if (difficultyLevel >= 1 && camp.isNeutral) dist *= 0.6f;
                // Difficile : priorise moins les camps ennemis forts (prudence)
                if (difficultyLevel == 2 && !camp.isNeutral) dist *= 1.3f;

                if (dist < nearestScore) { nearestScore = dist; nearest = camp; }
            }
            return nearest;
        }

        private Camp FindRandomEnemyCamp()
        {
            Camp[] allCamps = FindObjectsByType<Camp>(FindObjectsSortMode.None);
            List<Camp> enemies = new List<Camp>();
            foreach (Camp c in allCamps)
                if (c.owner != player) enemies.Add(c);
            return enemies.Count > 0 ? enemies[Random.Range(0, enemies.Count)] : null;
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
