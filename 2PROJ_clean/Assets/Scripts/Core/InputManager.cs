using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SupKonQuest
{
    public class InputManager : MonoBehaviour
    {
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

        private readonly List<UnitMovement> selectedUnits = new List<UnitMovement>();

        // Groupes de sélection Ctrl+1-5 pour assigner, 1-5 pour rappeler
        private readonly Dictionary<int, List<UnitMovement>> unitGroups = new Dictionary<int, List<UnitMovement>>();

        private bool isDragging;
        private Vector2 dragStartScreen;

        private void Start()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            for (int i = 1; i <= 5; i++)
                unitGroups[i] = new List<UnitMovement>();
        }

        private void Update()
        {
            HandleMouse();
            HandleRightClick();
            HandleSpellHotkey();
            HandleGroupHotkeys();
            HandleDisembarkHotkey();
        }

        // ── Sélection souris ────────────────────────────────────────

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
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

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
                }
            }

            if (Physics.Raycast(ray, out RaycastHit hitCamp, 1000f, campLayerMask))
            {
                Camp camp = hitCamp.collider.GetComponent<Camp>();
                if (camp != null && camp.owner != null && camp.owner.playerId == localPlayerId)
                {
                    ClearSelection();
                    campUIManager?.SelectCamp(camp);
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

        // ── Clic droit : déplacer / embarquer ───────────────────────

        private void HandleRightClick()
        {
            if (!Input.GetMouseButtonDown(1) || selectedUnits.Count == 0) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // Clic droit sur un transport allié → embarquer
            if (Physics.Raycast(ray, out RaycastHit hitUnit, 1000f, unitLayerMask))
            {
                TransportShip transport = hitUnit.collider.GetComponentInParent<TransportShip>();
                UnitStats transportStats = hitUnit.collider.GetComponentInParent<UnitStats>();
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

                // Clic droit sur une unité ennemie → attaquer
                UnitStats target = hitUnit.collider.GetComponentInParent<UnitStats>();
                if (target != null && target.ownerId != localPlayerId)
                {
                    foreach (UnitMovement u in selectedUnits) u.MoveTo(hitUnit.point);
                    return;
                }
            }

            // Clic droit sur le sol → déplacer
            int ignoreMask = unitLayerMask | campLayerMask;
            int allButUnits = ~ignoreMask;
            if (Physics.Raycast(ray, out RaycastHit hitGround, 1000f, allButUnits))
            {
                Vector3 center = hitGround.point;
                int count = selectedUnits.Count;
                for (int i = 0; i < count; i++)
                    selectedUnits[i].MoveTo(center + FormationOffset(i, count));
            }
        }

        // ── Sorts ────────────────────────────────────────────────────

        private void HandleSpellHotkey()
        {
            if (!Input.GetKeyDown(KeyCode.Q)) return;
            if (selectedUnits.Count != 1) return;

            UnitSpell spell = selectedUnits[0].GetComponent<UnitSpell>();
            spell?.TryActivate();
        }

        // ── Débarquement ─────────────────────────────────────────────

        private void HandleDisembarkHotkey()
        {
            if (!Input.GetKeyDown(KeyCode.E)) return;
            if (selectedUnits.Count != 1) return;

            TransportShip transport = selectedUnits[0].GetComponent<TransportShip>();
            if (transport == null || transport.IsEmpty) return;

            // Débarquer au sol sous la souris si possible, sinon à la position du transport
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 disembarkPos = selectedUnits[0].transform.position;
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask))
                disembarkPos = hit.point;

            transport.DisembarkAll(disembarkPos);
        }

        // ── Groupes Ctrl+1-5 ─────────────────────────────────────────

        private void HandleGroupHotkeys()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            for (int i = 0; i < 5; i++)
            {
                KeyCode key = (KeyCode)((int)KeyCode.Alpha1 + i);
                if (!Input.GetKeyDown(key)) continue;

                int groupId = i + 1;
                if (ctrl)
                    AssignGroup(groupId);
                else
                    RecallGroup(groupId);
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

            // Nettoyer les entrées nulles du groupe
            unitGroups[groupId].RemoveAll(u => u == null);

            RefreshSpellUI();
        }

        // ── Helpers ──────────────────────────────────────────────────

        private void RefreshSpellUI()
        {
            if (spellUI == null) return;

            if (selectedUnits.Count == 1)
            {
                UnitSpell spell = selectedUnits[0].GetComponent<UnitSpell>();
                if (spell != null)
                {
                    spellUI.ShowForUnit(spell);
                    return;
                }
            }

            spellUI.HidePanel();
        }

        private void SelectUnit(UnitMovement unit)
        {
            if (unit == null || selectedUnits.Contains(unit)) return;
            selectedUnits.Add(unit);
            unit.SetSelected(true);
        }

        private void ClearSelection()
        {
            foreach (UnitMovement u in selectedUnits)
                if (u != null) u.SetSelected(false);
            selectedUnits.Clear();
        }

        private static Rect GetScreenRect(Vector2 a, Vector2 b)
        {
            return new Rect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private static Vector3 FormationOffset(int i, int count)
        {
            if (count == 1) return Vector3.zero;
            float radius = 1.5f + count * 0.3f;
            float angle = i * (360f / count) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private void OnGUI()
        {
            if (!isDragging) return;
            Rect rect = GetScreenRect(dragStartScreen, Input.mousePosition);
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
