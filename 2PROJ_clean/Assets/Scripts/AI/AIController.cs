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

        private static readonly float[] ThinkInterval      = { 2f,   1.5f,  1f  };
        private static readonly float[] ProductionInterval = { 12f,  8f,    4f  };

        private float thinkTimer;
        private float scoutTimer;
        private float waveTimer;
        private float navalTimer;
        private float productionTimer;

        private GameManager gm;
        private bool isIslandMap;

        private bool sawmillQueued;
        private bool portQueued;
        private bool castleQueued;

        private bool neutralRushActive;
        private Camp neutralRushTarget;

        private bool earlySawmillDone;

        private void Start()
        {
            gm             = GameManager.Instance;
            difficultyLevel = Mathf.Clamp(PlayerPrefs.GetInt("AIDifficulty", difficultyLevel), 0, 2);
            isIslandMap    = PlayerPrefs.GetInt("MapType", 0) == (int)MapType.Island;

            thinkTimer = Random.Range(0f, ThinkInterval[difficultyLevel]);
            scoutTimer = Random.Range(8f, 20f);
            waveTimer  = 0f;
            navalTimer = Random.Range(60f, 90f);
        }

        private void Update()
        {
            if (gm == null || gm.gameOver || player == null || player.eliminated) return;

            thinkTimer       -= Time.deltaTime;
            scoutTimer       -= Time.deltaTime;
            waveTimer        -= Time.deltaTime;
            navalTimer       -= Time.deltaTime;
            productionTimer  -= Time.deltaTime;

            if (thinkTimer > 0f) return;
            thinkTimer = ThinkInterval[difficultyLevel] + Random.Range(-0.2f, 0.4f);

            switch (difficultyLevel)
            {
                case 0: DoEasy();   break;
                case 1: DoMedium(); break;
                case 2: DoHard();   break;
            }
        }

        private void DoEasy()
        {
            TrainBasic(maxUnits: 15, queueMax: 1);

            if (scoutTimer <= 0f)
            {
                scoutTimer = 30f + Random.Range(-4f, 6f);
                SendInfantryScout();
            }

            TryRushNeutral(threshold: 6);

            if (HasCapturedNeutral())
            {
                if (!sawmillQueued)
                    sawmillQueued = TryBuildNow(BuildingType.Sawmill);

                if (sawmillQueued && !portQueued)
                    portQueued = TryBuildNow(BuildingType.Port);
            }

            if (waveTimer <= 0f)
                TryPush(threshold: 10, reserveCount: 0);

            AssignIdle();
        }

        private void DoMedium()
        {
            if (!earlySawmillDone)
                earlySawmillDone = TryBuildNow(BuildingType.Sawmill);

            TrainMedium();

            if (scoutTimer <= 0f)
            {
                scoutTimer = 30f + Random.Range(-4f, 6f);
                SendInfantryScout();
            }

            TryRushNeutral(threshold: 6);

            if (HasCapturedNeutral() && !portQueued)
                portQueued = TryBuildNow(BuildingType.Port);

            if (navalTimer <= 0f && HasPort())
            {
                navalTimer = Random.Range(50f, 80f);
                TryNavalAttack();
            }

            if (waveTimer <= 0f)
                TryPush(threshold: 10, reserveCount: 2);

            AssignIdle();
        }

        private void TrainMedium()
        {
            if (productionTimer > 0f) return;

            int pop    = CountUnits();
            int popCap = player.ownedCamps.Count * 10;
            if (pop >= Mathf.Min(20, popCap)) return;

            bool trained = false;
            foreach (Camp camp in player.ownedCamps)
            {
                CampProduction prod = camp.GetComponent<CampProduction>();
                if (prod == null || prod.GetQueueCount() >= 1) continue;

                UnitType unit = camp.campType == CampType.Port
                    ? (Random.value < 0.55f ? UnitType.Frigate : UnitType.Transport)
                    : (Random.value < 0.60f ? UnitType.Infantry : UnitType.Range);

                if (prod.Produce(unit)) trained = true;
            }

            if (trained)
                productionTimer = ProductionInterval[difficultyLevel] + Random.Range(-1f, 2f);
        }

        private void TryNavalAttack()
        {
            Camp target = BestEnemyCamp();
            if (target == null) return;

            var naval = GetNavalUnits();
            if (naval.Count < 2) return;

            for (int i = 0; i < naval.Count; i++)
            {
                naval[i].MoveTo(target.transform.position + FormOffset(i, naval.Count));
                naval[i].GetComponent<UnitAttack>()?.SetCampTarget(target);
            }
        }

        private void DoHard()
        {
            if (RespondToThreat()) return;

            TrainHard();
            TryBuildHard();

            TryRushNeutral(threshold: 5);

            AssignDefenders(perCamp: 3);

            if (waveTimer <= 0f && CountUnits() >= 8)
            {
                waveTimer = 18f + Random.Range(-4f, 6f);
                TryPush(threshold: 8, reserveCount: 3);
            }

            if (isIslandMap) ManageNavy();
        }

        private void TrainHard()
        {
            if (productionTimer > 0f) return;

            int pop    = CountUnits();
            int popCap = player.ownedCamps.Count * 10;
            if (pop >= Mathf.Min(30, popCap)) return;

            EnemyCompo comp   = ScanEnemy();
            bool       trained = false;

            foreach (Camp camp in player.ownedCamps)
            {
                CampProduction prod = camp.GetComponent<CampProduction>();
                if (prod == null || prod.GetQueueCount() >= 2) continue;
                if (prod.Produce(PickUnitHard(camp, comp))) trained = true;
            }

            if (trained)
                productionTimer = ProductionInterval[difficultyLevel] + Random.Range(-1f, 2f);
        }

        private UnitType PickUnitHard(Camp camp, EnemyCompo comp)
        {
            if (camp.campType == CampType.Port)
            {
                float r = Random.value;
                if (isIslandMap && r < 0.25f) return UnitType.Transport;
                return r < 0.5f ? UnitType.Frigate : UnitType.Destroyer;
            }
            if (camp.campType == CampType.Castle)
            {
                bool vsHeavy = comp.total > 0 && comp.heavy > comp.total * 0.3f;
                float r = Random.value;
                if (vsHeavy && r < 0.40f) return UnitType.AntiArmor;
                if (r < 0.30f) return UnitType.Mortar;
                if (r < 0.55f) return UnitType.Heavy;
                if (r < 0.75f) return UnitType.Heal;
                return UnitType.AntiArmor;
            }
            bool needAA = comp.total > 0 && comp.heavy > comp.total * 0.25f;
            float r2 = Random.value;
            if (needAA && r2 < 0.30f) return UnitType.AntiArmor;
            if (r2 < 0.30f) return UnitType.Infantry;
            if (r2 < 0.50f) return UnitType.Range;
            if (r2 < 0.65f) return UnitType.Heavy;
            return UnitType.AntiArmor;
        }

        private void TryBuildHard()
        {
            if (CountSawmills() < 2 && !sawmillQueued)
                sawmillQueued = TryBuildNow(BuildingType.Sawmill);
            else if (CountSawmills() >= 1)
                sawmillQueued = true;

            if (!portQueued && !HasCampOfType(CampType.Port))
                portQueued = TryBuildNow(BuildingType.Port);

            if (!castleQueued && player.ownedCamps.Count >= 3 && !HasCampOfType(CampType.Castle))
                castleQueued = TryBuildNow(BuildingType.Castle);
        }


        private void TrainBasic(int maxUnits, int queueMax)
        {
            if (productionTimer > 0f) return;

            int pop    = CountUnits();
            int popCap = player.ownedCamps.Count * 10;
            if (pop >= Mathf.Min(maxUnits, popCap)) return;

            bool trained = false;
            foreach (Camp camp in player.ownedCamps)
            {
                CampProduction prod = camp.GetComponent<CampProduction>();
                if (prod == null || prod.GetQueueCount() >= queueMax) continue;
                if (camp.campType == CampType.Port || camp.campType == CampType.Castle) continue;
                if (prod.Produce(UnitType.Infantry)) trained = true;
            }

            if (trained)
                productionTimer = ProductionInterval[difficultyLevel] + Random.Range(-1f, 2f);
        }

        private void SendInfantryScout()
        {
            Camp target = RandomEnemyCamp();
            if (target == null) return;

            foreach (UnitMovement mov in GetMovable())
            {
                UnitStats us = mov.GetComponent<UnitStats>();
                if (us == null || us.unitType != UnitType.Infantry) continue;
                if (IsBusy(mov)) continue;

                mov.MoveTo(target.transform.position + FormOffset(0, 1));
                mov.GetComponent<UnitAttack>()?.SetCampTarget(target);
                return;
            }
        }

        private void TryRushNeutral(int threshold)
        {
            if (neutralRushActive)
            {
                if (neutralRushTarget == null || !neutralRushTarget.isNeutral)
                {
                    neutralRushActive = false;
                    neutralRushTarget = null;
                }
                else
                {
                    foreach (UnitMovement mov in GetMovable())
                    {
                        if (IsBusy(mov)) continue;
                        mov.MoveTo(neutralRushTarget.transform.position + FormOffset(0, 1));
                        mov.GetComponent<UnitAttack>()?.SetCampTarget(neutralRushTarget);
                    }
                    return;
                }
            }

            if (CountUnits() < threshold) return;

            Camp neutral = NearestNeutral(60f);
            if (neutral == null) return;

            var units = GetMovable();
            units.Sort((a, b) =>
                Vector3.Distance(a.transform.position, neutral.transform.position)
                    .CompareTo(Vector3.Distance(b.transform.position, neutral.transform.position)));

            int count = Mathf.Min(threshold, units.Count);
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                units[i].MoveTo(neutral.transform.position + FormOffset(i, count));
                units[i].GetComponent<UnitAttack>()?.SetCampTarget(neutral);
            }

            neutralRushActive = true;
            neutralRushTarget = neutral;
        }

        private void TryPush(int threshold, int reserveCount)
        {
            if (CountUnits() < threshold) return;

            Camp target = BestEnemyCamp();
            if (target == null) return;

            var all = GetMovable();

            var reserved = new HashSet<UnitMovement>();
            if (reserveCount > 0 && player.ownedCamps.Count > 0)
            {
                var near = UnitsNear(player.ownedCamps[0].transform.position, 22f);
                near.Sort((a, b) =>
                    Vector3.Distance(a.transform.position, player.ownedCamps[0].transform.position)
                        .CompareTo(Vector3.Distance(b.transform.position, player.ownedCamps[0].transform.position)));
                for (int i = 0; i < Mathf.Min(reserveCount, near.Count); i++)
                    reserved.Add(near[i]);
            }

            var attackers = new List<UnitMovement>();
            foreach (var u in all)
            {
                if (reserved.Contains(u)) continue;
                attackers.Add(u);
                if (attackers.Count >= threshold) break;
            }

            if (attackers.Count < threshold / 2) return;

            for (int i = 0; i < attackers.Count; i++)
            {
                attackers[i].MoveTo(target.transform.position + FormOffset(i, attackers.Count));
                attackers[i].GetComponent<UnitAttack>()?.SetCampTarget(target);
            }

            waveTimer = (difficultyLevel == 0 ? 45f : 25f) + Random.Range(-5f, 8f);
        }

        private void AssignDefenders(int perCamp)
        {
            foreach (Camp camp in player.ownedCamps)
            {
                var near = UnitsNear(camp.transform.position, 18f);
                int assigned = 0;
                foreach (var mov in near)
                {
                    if (assigned >= perCamp) break;
                    if (IsBusy(mov)) continue;
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float dist  = Random.Range(2f, 4f);
                    mov.MoveTo(camp.transform.position +
                        new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist));
                    assigned++;
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
            if (BuildingManager.Instance == null)
            {
                Debug.LogWarning("[AI] BuildingManager.Instance est null");
                return false;
            }

            bool needWater = (type == BuildingType.Port);

            foreach (Camp camp in player.ownedCamps)
            {
                HexTile tile = FreeTile(camp.transform.position, 16f, needWater);
                if (tile == null)
                {
                    Debug.LogWarning($"[AI] Aucune tuile libre pour {type} prÃ¨s de {camp.name} ({camp.transform.position})");
                    continue;
                }
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

        private bool HasCampOfType(CampType type)
        {
            foreach (Camp c in player.ownedCamps)
                if (c.campType == type) return true;
            return false;
        }

        private bool HasPort() => HasCampOfType(CampType.Port);

        private Camp MyPort()
        {
            foreach (Camp c in player.ownedCamps)
                if (c.campType == CampType.Port) return c;
            return null;
        }

        private Camp RandomEnemyCamp()
        {
            var enemies = new List<Camp>();
            foreach (Camp c in FindObjectsByType<Camp>(FindObjectsSortMode.None))
            {
                if (c.isNeutral || c.owner == player) continue;
                enemies.Add(c);
            }
            return enemies.Count > 0 ? enemies[Random.Range(0, enemies.Count)] : null;
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

        private List<UnitMovement> GetNavalUnits()
        {
            var list = new List<UnitMovement>();
            foreach (UnitStats us in FindObjectsByType<UnitStats>(FindObjectsSortMode.None))
            {
                if (us.ownerId != player.playerId) continue;
                if (!us.gameObject.activeInHierarchy || us.currentHealth <= 0) continue;
                if (us.unitType != UnitType.Frigate &&
                    us.unitType != UnitType.Destroyer &&
                    us.unitType != UnitType.Transport) continue;
                UnitMovement mov = us.GetComponent<UnitMovement>();
                if (mov != null && !mov.IsLocked) list.Add(mov);
            }
            return list;
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

        private void AssignIdle()
        {
            foreach (UnitMovement mov in GetMovable())
            {
                if (IsBusy(mov)) continue;
                Camp nearest = NearestOwnCamp(mov.transform.position);
                if (nearest == null) continue;
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist  = Random.Range(2f, 5f);
                mov.MoveTo(nearest.transform.position +
                    new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist));
            }
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

        private int CountSawmills()
        {
            int n = 0;
            foreach (Sawmill s in FindObjectsByType<Sawmill>(FindObjectsSortMode.None))
                if (s.owner == player) n++;
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

                float xzDist = Mathf.Sqrt(
                    (t.transform.position.x - center.x) * (t.transform.position.x - center.x) +
                    (t.transform.position.z - center.z) * (t.transform.position.z - center.z));
                if (xzDist < 0.05f) continue;

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
        }
    }
}
