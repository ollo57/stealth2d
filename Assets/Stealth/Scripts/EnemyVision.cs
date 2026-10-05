using UnityEngine;

namespace AlleyStealth
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class EnemyVision : MonoBehaviour
    {
        [Header("Sight: shared by cone and player detection")]
        [SerializeField] private FreeMovementController player;
        [SerializeField] private LayerMask sightBlockers;
        [SerializeField, Min(0.1f)] private float viewDistance = 6f;
        [SerializeField, Range(1f, 170f)] private float viewAngle = 80f;
        [SerializeField, Min(0.1f)] private float sightHeight = 0.45f;
        [SerializeField, Range(16, 360)] private int coneSegments = 160;
        [SerializeField] private float floorOverlayHeight = 0.04f;

        [Header("Slow lookout sweep (zero angle keeps it stationary)")]
        [SerializeField, Range(0f, 90f)] private float scanHalfAngle = 55f;
        [SerializeField, Min(1f)] private float scanPeriod = 10f;
        [SerializeField] private float scanPhase;
        [SerializeField] private SpriteRenderer guardSprite;

        private Mesh coneMesh;
        private Vector3[] vertices;
        private int[] triangles;
        private float currentYaw;
        public bool CanSeePlayer { get; private set; }
        public bool PlayerInViewSector { get; private set; }
        public Vector3 EyePosition => transform.position + Vector3.up * sightHeight;
        public Vector3 ViewDirection => transform.TransformDirection(Quaternion.Euler(0f, currentYaw, 0f) * Vector3.forward);
        public float ViewDistance => viewDistance;
        public float ViewAngle => viewAngle;

        private void Awake()
        {
            coneMesh = new Mesh { name = "Clipped lookout vision" };
            coneMesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = coneMesh;
            // Overlay is below all upright sprites, above the illustrated ground.
            GetComponent<MeshRenderer>().sortingOrder = -9000;
        }

        private void LateUpdate()
        {
            currentYaw = Mathf.Sin((Time.time / scanPeriod + scanPhase) * Mathf.PI * 2f) * scanHalfAngle;
            if (guardSprite != null) guardSprite.flipX = ViewDirection.x < 0f;
            PlayerInViewSector = player != null && IsInViewSector(player.transform.position);
            CanSeePlayer = player != null && CanSeePosition(player.transform.position);
            UpdateCone();
        }

        public bool IsInViewSector(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= viewDistance * viewDistance &&
                (offset.sqrMagnitude < 0.000001f || Vector3.Angle(ViewDirection, offset) <= viewAngle * 0.5f);
        }

        public bool CanSeePosition(Vector3 position)
        {
            if (!IsInViewSector(position)) return false;
            Vector3 offset = position - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            // A single center sample matches the ground cone. No zone state grants invisibility.
            return distance < 0.001f || VisibleDistance(offset / distance) + 0.001f >= distance;
        }

        public float VisibleDistance(Vector3 direction)
        {
            direction.y = 0f;
            if (Physics.Raycast(EyePosition, direction.normalized, out RaycastHit hit,
                viewDistance, sightBlockers, QueryTriggerInteraction.Ignore)) return hit.distance;
            return viewDistance;
        }

        private void UpdateCone()
        {
            if (vertices == null || vertices.Length != coneSegments + 2)
            {
                vertices = new Vector3[coneSegments + 2];
                triangles = new int[coneSegments * 3];
                for (int i = 0; i < coneSegments; i++)
                {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
                coneMesh.Clear();
            }
            vertices[0] = Vector3.up * floorOverlayHeight;
            for (int i = 0; i <= coneSegments; i++)
            {
                float angle = -viewAngle * 0.5f + viewAngle * i / coneSegments;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * ViewDirection;
                Vector3 end = transform.position + direction * VisibleDistance(direction);
                end.y = transform.position.y + floorOverlayHeight;
                vertices[i + 1] = transform.InverseTransformPoint(end);
            }
            coneMesh.vertices = vertices;
            coneMesh.triangles = triangles;
            coneMesh.RecalculateBounds();
        }

        private void OnDisable()
        {
            CanSeePlayer = false;
            PlayerInViewSector = false;
            if (coneMesh != null) coneMesh.Clear();
        }

        private void OnDestroy()
        {
            if (coneMesh != null) Destroy(coneMesh);
        }
    }
}
