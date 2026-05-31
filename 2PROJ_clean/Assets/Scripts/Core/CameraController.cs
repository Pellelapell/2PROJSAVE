using UnityEngine;

namespace SupKonQuest
{
    public class CameraController : MonoBehaviour
    {
        [Header("Déplacement clavier / bord d'écran")]
        [SerializeField] private float moveSpeed = 40f;
        [SerializeField] private float edgeScrollSpeed = 35f;
        [SerializeField] private float edgeSize = 20f;

        [Header("Clic-molette (drag)")]
        [SerializeField] private float dragSensitivity = 0.4f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 300f;
        [SerializeField] private float minY = 8f;
        [SerializeField] private float maxY = 60f;

        [Header("Map Bounds")]
        [SerializeField] private float padding = 5f;

        private float minX, maxX, minZ, maxZ;

        private bool  isDragging;
        private Vector3 dragOriginWorld;

        private void Start()
        {
            RefreshBounds();
            CenterOnPlayerCamp();
        }

        private void CenterOnPlayerCamp()
        {
            if (GameManager.Instance == null) return;
            PlayerData localPlayer = null;
            foreach (PlayerData p in GameManager.Instance.activePlayers)
                if (!p.isAI) { localPlayer = p; break; }
            if (localPlayer == null || localPlayer.ownedCamps.Count == 0) return;
            Vector3 campPos = localPlayer.ownedCamps[0].transform.position;
            Vector3 pos = transform.position;
            pos.x = campPos.x;
            pos.z = campPos.z;
            transform.position = pos;
        }

        public void RefreshBounds()
        {
            Bounds b = HexGridGenerator.MapBounds;
            if (b.size == Vector3.zero)
            {
                minX = -1000f; maxX = 1000f;
                minZ = -1000f; maxZ = 1000f;
                return;
            }
            minX = b.min.x - padding;
            maxX = b.max.x + padding;
            minZ = b.min.z - padding;
            maxZ = b.max.z + padding;
        }

        private void Update()
        {
            HandleMiddleMouseDrag();
            HandleKeyboardAndEdge();
            HandleZoom();
            ClampPosition();
        }

        private void HandleMiddleMouseDrag()
        {
            if (Input.GetMouseButtonDown(2))
            {
                isDragging = true;
                dragOriginWorld = GetGroundPoint();
            }

            if (Input.GetMouseButtonUp(2))
                isDragging = false;

            if (!isDragging) return;

            Vector3 current = GetGroundPoint();
            Vector3 delta = dragOriginWorld - current;
            delta.y = 0f;
            transform.position += delta * dragSensitivity;
        }

        private void HandleKeyboardAndEdge()
        {
            Vector3 move = Vector3.zero;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            move += transform.right * h;
            move += Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * v;

            if (!isDragging)
            {
                Vector3 mousePos = Input.mousePosition;
                Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

                if (mousePos.x <= edgeSize)                          move += -transform.right;
                else if (mousePos.x >= Screen.width  - edgeSize)     move += transform.right;
                if (mousePos.y <= edgeSize)                          move += -flatForward;
                else if (mousePos.y >= Screen.height - edgeSize)     move += flatForward;
            }

            if (move == Vector3.zero) return;

            float speedMult = Mathf.Lerp(1f, 2.5f, (transform.position.y - minY) / Mathf.Max(1f, maxY - minY));
            float speed = (h != 0f || v != 0f) ? moveSpeed : edgeScrollSpeed;
            transform.position += move.normalized * speed * speedMult * Time.deltaTime;
        }

        private void HandleZoom()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f) return;

            Vector3 target = GetGroundPoint();
            Vector3 dir = (target - transform.position).normalized;

            Vector3 pos = transform.position + dir * scroll * zoomSpeed * Time.deltaTime;
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            transform.position = pos;
        }

        private void ClampPosition()
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            transform.position = pos;
        }

        private Vector3 GetGroundPoint()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float dist))
                return ray.GetPoint(dist);
            return transform.position;
        }
    }
}
