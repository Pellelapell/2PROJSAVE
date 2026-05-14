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

        private static readonly int[] attackThreshold = { 4, 2, 1 };

        private static readonly int[] maxQueue = { 2, 3, 4 };
        private static readonly float[] thinkDelay = { 4f, 2.5f, 1.5f };

        private void Start()
        {
            gameManager = GameManager.Instance;
            thinkTimer = Random.Range(0f, thinkDelay[difficultyLevel]);
        }

        private void Update()
        {
            if (gameManager == null || gameManager.gameOver || player == null || player.eliminated) return;

            thinkTimer -= Time.deltaTime;
            if (thinkTimer > 0f) return;
            thinkTimer = thinkDelay[difficultyLevel];

            ProduceUnits();
            GiveOrders();
        }

        private void ProduceUnits()
        {
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
            if (difficultyLevel == 0) return UnitType.Infantry;

            if (camp.campType == CampType.Port)
                return UnitType.Frigate;

            float r = Random.value;
            if (difficultyLevel == 1)
            {
                if (r < 0.5f) return UnitType.Infantry;
                if (r < 0.75f) return UnitType.Range;
                return UnitType.Heavy;
            }

            // Hard: varied composition
            if (r < 0.35f) return UnitType.Infantry;
            if (r < 0.55f) return UnitType.Range;
            if (r < 0.7f) return UnitType.Heavy;
            if (r < 0.85f) return UnitType.AntiArmor;
            return UnitType.Mortar;
        }

        private void GiveOrders()
        {
            Camp target = FindAttackTarget();
            if (target == null) return;

            List<UnitMovement> myUnits = GetMyUnits();
            if (myUnits.Count < attackThreshold[difficultyLevel]) return;

            // Hard AI keeps one defender per camp
            if (difficultyLevel == 2 && player.ownedCamps.Count > 0)
            {
                myUnits = FilterDefenders(myUnits);
                if (myUnits.Count == 0) return;
            }

            Vector3 dest = target.transform.position;
            int count = myUnits.Count;
            for (int i = 0; i < count; i++)
                myUnits[i].MoveTo(dest + FormationOffset(i, count));
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
            // Keep one unit per owned camp as defender
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

                if (difficultyLevel >= 1 && camp.isNeutral)
                    dist *= 0.7f;

                if (difficultyLevel == 2 && !camp.isNeutral)
                    dist *= 1.2f;

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
            float angle = i * (360f / count) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }
    }
}
