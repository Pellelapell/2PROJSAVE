using UnityEngine;

namespace SupKonQuest
{
    public class CameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 20f;
        [SerializeField] private float edgeScrollSpeed = 20f;
        [SerializeField] private float edgeSize = 15f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 200f;
        [SerializeField] private float minY = 10f;
        [SerializeField] private float maxY = 50f;

        [Header("Map Bounds (auto si HexGridGenerator présent)")]
        [SerializeField] private float padding = 5f;

        private float minX, maxX, minZ, maxZ;

        private void Start()
        {
            RefreshBounds();
        }

        // Appelable après régénération de la grille si besoin
        public void RefreshBounds()
        {
            Bounds b = HexGridGenerator.MapBounds;

            // Si la grille n'a pas encore été générée (bounds vide), garder la caméra libre
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
            HandleMovement();
            HandleZoom();
            ClampPosition();
        }

        private void HandleMovement()
        {
            Vector3 move = Vector3.zero;

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical   = Input.GetAxisRaw("Vertical");

            move += transform.right * horizontal;
            move += Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * vertical;

            Vector3 mousePos = Input.mousePosition;

            if (mousePos.x <= edgeSize)
                move += -transform.right;

            if (mousePos.x >= Screen.width - edgeSize)
                move += transform.right;

            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

            if (mousePos.y <= edgeSize)
                move += -flatForward;

            if (mousePos.y >= Screen.height - edgeSize)
                move += flatForward;

            if (move != Vector3.zero)
                transform.position += move.normalized * moveSpeed * Time.deltaTime;
        }

        private void HandleZoom()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f) return;

            Vector3 pos = transform.position;
            pos.y -= scroll * zoomSpeed * Time.deltaTime;
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
    }
}
