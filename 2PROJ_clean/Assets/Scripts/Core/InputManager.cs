using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SupKonQuest
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("References")]
        public Camera mainCamera;
        public CampUIManager campUIManager;
        public SpellUI spellUI;

        [Header("Layers")]
        public LayerMask unitLayerMask;
        public LayerMask campLayerMask;
        public LayerMask groundLayerMask;

        [Header("Local Player")]
        public int localPlayerId = 1;

        public UnitStats SelectedUnitStats { get; private set; }

        public Camp SelectedCampBuilding { get; private set; }
        public BuildingHealth SelectedHealthBuilding { get; private set; }

        private readonly List<UnitMovement> selectedUnits = new List<UnitMovement>();
        private readonly Dictionary<int, List<UnitMovement>> unitGroups = new Dictionary<int, List<UnitMovement>>();
        private readonly List<UnitStats> highlightedTargets = new List<UnitStats>();
        private readonly List<Camp>      highlightedCamps   = new List<Camp>();

        private bool isDragging;
        private Vector2 dragStartScreen;

        public bool IsAttackMoveMode { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            for (int i = 1; i <= 5; i++)
                unitGroups[i] = new List<UnitMovement>();
        }

        private void Update()
        {
            HandleAttackMoveToggle();
            HandleMouse();
            HandleRightClick();
            HandleSpellHotkey();
            HandleGroupHotkeys();
            HandleDisembarkHotkey();
            UpdateTargetHighlights();
        }

        private float highlightRefreshTimer;

        private void UpdateTargetHighlights()
        {
            if (!IsAttackMoveMode)
            {
                if (highlightedTargets.Count > 0)
                {
                    foreach (UnitStats u in highlightedTargets) if (u != null) u.SetAsTarget(false);
                    highlightedTargets.Clear();
                }
                if (highlightedCamps.Count > 0)
                {
                    foreach (Camp c in highlightedCamps) if (c != null) c.SetAsTarget(false);
                    highlightedCamps.Clear();
                }
                return;
            }

            highlightRefreshTimer -= Time.deltaTime;
            if (highlightRefreshTimer > 0f) return;
            highlightRefreshTimer = 0.2f;

            foreach (UnitStats u in highlightedTargets) if (u != null) u.SetAsTarget(false);
            highlightedTargets.Clear();
            foreach (Camp c in highlightedCamps) if (c != null) c.SetAsTarget(false);
            highlightedCamps.Clear();

            // Unités ennemies
            UnitStats[] allUnits = FindObjectsByType<UnitStats>(FindObjectsSortMode.None);
            foreach (UnitStats other in allUnits)
            {
                if (other == null || other.currentHealth <= 0 || other.ownerId == localPlayerId) continue;
                other.SetAsTarget(true);
                highlightedTargets.Add(other);
            }

            // Camps ennemis / neutres
            Camp[] allCamps = FindObjectsByType<Camp>(FindObjectsSortMode.None);
            foreach (Camp camp in allCamps)
            {
                if (camp.owner != null && camp.owner.playerId == localPlayerId) continue;
                camp.SetAsTarget(true);
                highlightedCamps.Add(camp);
            }
        }

        private void HandleAttackMoveToggle()
        {
            IsAttackMoveMode = Input.GetKey(KeyCode.Q) && selectedUnits.Count > 0;
        }

        private void HandleMouse()
        {
            if (Input.GetMouseButtonDown(0))
            {
                dragStartScreen = Input.mousePosition;
                isDragging = false;
            }

            if (Input.GetMouseButton(0))
                if (Vector2.Distance(dragStartScreen, Input.mousePosition) > 5f)
                    isDragging = true;

            if (Input.GetMouseButtonUp(0))
            {
                if (isDragging) EndDragSelection();
                else SingleClick();
                isDragging = false;
            }
        }

        private void SingleClick()
        {
            if (BuilderHUD.Instance != null && BuilderHUD.Instance.IsMouseOverPanel) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (IsAttackMoveMode && selectedUnits.Count > 0)
            {
                // Tout ce que le rayon touche, sans filtre de layer
                RaycastHit[] atkHits = Physics.RaycastAll(ray, 1000f);
                System.Array.Sort(atkHits, (a, b) => a.distance.CompareTo(b.distance));

                UnitStats firstUnit = null;
                Camp      firstCamp = null;

                foreach (RaycastHit h in atkHits)
                {
                    if (firstUnit == null)
                    {
                        UnitStats t = h.collider.GetComponentInParent<UnitStats>();
                        if (t != null && t.ownerId != localPlayerId && t.currentHealth > 0)
                            firstUnit = t;
                    }
                    if (firstCamp == null)
                    {
                        Camp c = h.collider.GetComponentInParent<Camp>();
                        if (c != null && (c.isNeutral || c.owner == null || c.owner.playerId != localPlayerId))
                            firstCamp = c;
                    }
                    if (firstUnit != null && firstCamp != null) break;
                }

                // Priorité au camp : les gardes neutres bloquaient le bâtiment
                if (firstCamp != null)
                {
                    foreach (UnitMovement u in selectedUnits)
                    {
                        u.MoveTo(firstCamp.transform.position);
                        u.GetComponent<UnitAttack>()?.SetCampTarget(firstCamp);
                    }
                    return;
                }
                if (firstUnit != null)
                {
                    foreach (UnitMovement u in selectedUnits)
                        u.GetComponent<UnitAttack>()?.SetUnitTarget(firstUnit);
                    return;
                }
                return;
            }

            if (campUIManager != null && campUIManager.IsPickingSpawnPoint)
            {
                RaycastHit[] spawnHits = Physics.RaycastAll(ray, 1000f);
                foreach (RaycastHit h in spawnHits)
                {
                    HexTile tile = h.collider.GetComponentInParent<HexTile>();
                    if (tile != null && tile.terrain == HexTerrain.Walkable)
                    {
                        campUIManager.ConfirmSpawnPoint(tile.transform.position);
                        return;
                    }
                }
                campUIManager.CancelPickingSpawnPoint();
                return;
            }

            if (BuilderHUD.Instance != null && BuilderHUD.Instance.HasPendingBuild)
            {
                RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
                foreach (RaycastHit h in hits)
                {
                    HexTile tile = h.collider.GetComponentInParent<HexTile>();
                    if (tile != null) { BuilderHUD.Instance.TryPlaceOnTile(tile); return; }
                }
                BuilderHUD.Instance.CancelPending();
                return;
            }

            if (Physics.Raycast(ray, out RaycastHit hitUnit, 1000f, unitLayerMask))
            {
                UnitMovement unit = hitUnit.collider.GetComponentInParent<UnitMovement>();
                if (unit != null)
                {
                    UnitStats stats = unit.GetComponent<UnitStats>();
                    if (stats != null && stats.ownerId == localPlayerId)
                    {
                        if (!Input.GetKey(KeyCode.LeftShift)) ClearSelection();
                        SelectUnit(unit);
                        campUIManager?.HideUI();
                        RefreshSpellUI();
                        return;
                    }

                    if (stats != null)
                    {
                        ClearSelection();
                        SelectedUnitStats = stats;
                        campUIManager?.HideUI();
                        spellUI?.HidePanel();
                        return;
                    }
                }
            }

            if (Physics.Raycast(ray, out RaycastHit hitCamp, 1000f, campLayerMask))
            {
                Camp camp = hitCamp.collider.GetComponentInParent<Camp>();
                if (camp != null)
                {
                    ClearSelection();
                    SelectedCampBuilding = camp;
                    if (camp.owner != null && camp.owner.playerId == localPlayerId)
                        campUIManager?.SelectCamp(camp);
                    else
                        campUIManager?.HideUI();
                    spellUI?.HidePanel();
                    return;
                }
            }

            RaycastHit[] buildingHits = Physics.RaycastAll(ray, 1000f);
            foreach (RaycastHit h in buildingHits)
            {
                BuildingHealth bh = h.collider.GetComponentInParent<BuildingHealth>();
                if (bh != null && h.collider.GetComponentInParent<Camp>() == null)
                {
                    ClearSelection();
                    campUIManager?.HideUI();
                    SelectedHealthBuilding = bh;
                    spellUI?.HidePanel();
                    return;
                }
            }

            ClearSelection();
            campUIManager?.HideUI();
            spellUI?.HidePanel();
        }

        private void EndDragSelection()
        {
            ClearSelection();
            campUIManager?.HideUI();

            Rect selectionRect = GetScreenRect(dragStartScreen, Input.mousePosition);
            UnitMovement[] allUnits = FindObjectsByType<UnitMovement>(FindObjectsSortMode.None);

            foreach (UnitMovement unit in allUnits)
            {
                UnitStats stats = unit.GetComponent<UnitStats>();
                if (stats == null || stats.ownerId != localPlayerId) continue;
                Vector3 screenPos = mainCamera.WorldToScreenPoint(unit.transform.position);
                if (screenPos.z > 0f && selectionRect.Contains(new Vector2(screenPos.x, screenPos.y)))
                    SelectUnit(unit);
            }

            RefreshSpellUI();
        }

        private void HandleRightClick()
        {
            if (!Input.GetMouseButtonDown(1) || selectedUnits.Count == 0) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // Transport embark (propre transport uniquement)
            if (Physics.Raycast(ray, out RaycastHit hitTransport, 1000f, unitLayerMask))
            {
                TransportShip transport = hitTransport.collider.GetComponentInParent<TransportShip>();
                UnitStats transportStats = hitTransport.collider.GetComponentInParent<UnitStats>();
                if (transport != null && transportStats != null && transportStats.ownerId == localPlayerId)
                {
                    foreach (UnitMovement u in selectedUnits)
                    {
                        UnitStats us = u.GetComponent<UnitStats>();
                        if (us != null && us.unitType != UnitType.Transport)
                            transport.Embark(us);
                    }
                    ClearSelection();
                    return;
                }
            }

            // Camp ennemi/neutre → déplacement + capture
            if (Physics.Raycast(ray, out RaycastHit hitCamp, 1000f, campLayerMask))
            {
                Camp camp = hitCamp.collider.GetComponentInParent<Camp>();
                if (camp != null && (camp.isNeutral || (camp.owner != null && camp.owner.playerId != localPlayerId)))
                {
                    foreach (UnitMovement u in selectedUnits)
                    {
                        u.MoveTo(camp.transform.position);
                        UnitAttack atk = u.GetComponent<UnitAttack>();
                        if (atk != null) atk.SetCampTarget(camp);
                    }
                    return;
                }
            }

            // Déplacement au sol (le clic passe à travers les unités et camps)
            int ignoreMask = unitLayerMask | campLayerMask;
            if (Physics.Raycast(ray, out RaycastHit hitGround, 1000f, ~ignoreMask))
            {
                Vector3 center = hitGround.point;
                int count = selectedUnits.Count;
                for (int i = 0; i < count; i++)
                    selectedUnits[i].MoveTo(center + FormationOffset(i, count));
            }
        }

        private void HandleSpellHotkey()
        {
            if (!Input.GetKeyDown(KeyCode.A)) return;
            if (selectedUnits.Count != 1) return;

            UnitSpell spell = selectedUnits[0].GetComponent<UnitSpell>();
            spell?.TryActivate();
        }

        private void HandleDisembarkHotkey()
        {
            if (!Input.GetKeyDown(KeyCode.E)) return;
            if (selectedUnits.Count != 1) return;

            TransportShip transport = selectedUnits[0].GetComponent<TransportShip>();
            if (transport == null || transport.IsEmpty) return;

            Vector3 shipPos = selectedUnits[0].transform.position;
            Vector3 landPos = FindNearestWalkable(shipPos, 5f);
            if (landPos == Vector3.zero) return;

            transport.DisembarkAll(landPos);
        }

        private static Vector3 FindNearestWalkable(Vector3 from, float range)
        {
            Collider[] hits = Physics.OverlapSphere(from, range);
            float bestDist = float.MaxValue;
            Vector3 best = Vector3.zero;
            foreach (Collider c in hits)
            {
                HexTile tile = c.GetComponentInParent<HexTile>();
                if (tile == null || tile.terrain != HexTerrain.Walkable) continue;
                float d = Vector3.Distance(from, tile.transform.position);
                if (d < bestDist) { bestDist = d; best = tile.transform.position; }
            }
            return best;
        }

        private void HandleGroupHotkeys()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            for (int i = 0; i < 5; i++)
            {
                KeyCode key = (KeyCode)((int)KeyCode.Alpha1 + i);
                if (!Input.GetKeyDown(key)) continue;

                int groupId = i + 1;
                if (ctrl) AssignGroup(groupId);
                else      RecallGroup(groupId);
            }
        }

        private void AssignGroup(int groupId)
        {
            unitGroups[groupId].Clear();
            foreach (UnitMovement unit in selectedUnits)
                if (unit != null) unitGroups[groupId].Add(unit);
        }

        private void RecallGroup(int groupId)
        {
            if (!unitGroups.ContainsKey(groupId)) return;

            ClearSelection();
            campUIManager?.HideUI();

            foreach (UnitMovement unit in unitGroups[groupId])
            {
                if (unit == null) continue;
                UnitStats stats = unit.GetComponent<UnitStats>();
                if (stats != null && stats.ownerId == localPlayerId)
                    SelectUnit(unit);
            }

            unitGroups[groupId].RemoveAll(u => u == null);
            RefreshSpellUI();
        }

        private void RefreshSpellUI()
        {
            if (selectedUnits.Count == 1)
            {
                UnitStats stats = selectedUnits[0].GetComponent<UnitStats>();

                if (stats != null && stats.unitType == UnitType.Infantry)
                {
                    BuilderHUD.Instance?.ShowForUnit(stats);
                    spellUI?.HidePanel();
                    return;
                }

                BuilderHUD.Instance?.Hide();
                if (spellUI != null)
                {
                    UnitSpell spell = selectedUnits[0].GetComponentInChildren<UnitSpell>();
                    if (spell != null) { spellUI.ShowForUnit(spell); return; }
                }
            }
            else
            {
                BuilderHUD.Instance?.Hide();
            }

            spellUI?.HidePanel();
        }

        private void SelectUnit(UnitMovement unit)
        {
            if (unit == null || selectedUnits.Contains(unit)) return;
            SelectedCampBuilding = null;
            SelectedHealthBuilding = null;

            UnitStats stats = unit.GetComponent<UnitStats>();
            if (stats != null) stats.OnDeath += HandleSelectedUnitDeath;

            selectedUnits.Add(unit);
            unit.SetSelected(true);
            SelectedUnitStats = selectedUnits.Count == 1 ? stats : null;
        }

        private void HandleSelectedUnitDeath(UnitStats dead)
        {
            dead.OnDeath -= HandleSelectedUnitDeath;

            UnitMovement mov = dead.GetComponent<UnitMovement>();
            selectedUnits.Remove(mov);

            SelectedUnitStats = selectedUnits.Count == 1
                ? selectedUnits[0]?.GetComponent<UnitStats>()
                : null;

            RefreshSpellUI();
        }

        private void ClearSelection()
        {
            foreach (UnitMovement u in selectedUnits)
            {
                if (u == null) continue;
                u.SetSelected(false);
                UnitStats s = u.GetComponent<UnitStats>();
                if (s != null) s.OnDeath -= HandleSelectedUnitDeath;
            }
            selectedUnits.Clear();
            SelectedUnitStats = null;
            SelectedCampBuilding = null;
            SelectedHealthBuilding = null;
            BuilderHUD.Instance?.Hide();

            foreach (UnitStats u in highlightedTargets) if (u != null) u.SetAsTarget(false);
            highlightedTargets.Clear();
            foreach (Camp c in highlightedCamps) if (c != null) c.SetAsTarget(false);
            highlightedCamps.Clear();
        }

        private static Rect GetScreenRect(Vector2 a, Vector2 b) =>
            new Rect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        private static Vector3 FormationOffset(int i, int count)
        {
            if (count == 1) return Vector3.zero;
            float radius = 1.5f + count * 0.3f;
            float angle  = i * (360f / count) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private void OnGUI()
        {
            if (!isDragging) return;
            Rect rect    = GetScreenRect(dragStartScreen, Input.mousePosition);
            Rect guiRect = new Rect(rect.x, Screen.height - rect.y - rect.height, rect.width, rect.height);
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.2f, 0.6f, 1f, 0.15f));
            tex.Apply();
            GUI.DrawTexture(guiRect, tex);
            GUI.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            GUI.Box(guiRect, GUIContent.none);
            GUI.color = Color.white;
        }
    }
}
