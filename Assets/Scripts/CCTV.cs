using UnityEngine;

public class ConeLOS3D : MonoBehaviour
{
    [Header("Visual Light")]
    public Light spotLight;

    [Header("Vision")]
    public float viewDistance = 10f;
    [Range(0f, 180f)]
    public float viewAngle = 60f;

    [Header("References")]
    public Transform player;
    public Renderer coneRenderer;

    [Header("Layers")]
    public LayerMask obstacleMask;

    bool detected;

    void Update()
    {
        detected = CheckLOS();
        coneRenderer.material.color = detected ? Color.red : Color.green;

        detected = CheckLOS();

        if (coneRenderer)
            coneRenderer.material.color = detected ? Color.red : Color.green;

        if (spotLight)
            spotLight.color = detected ? Color.red : Color.green;
    }

    bool CheckLOS()
    {
        if (!player) return false;

        Vector3 origin = transform.position;
        Vector3 toPlayer = player.position - origin;

        if (toPlayer.magnitude > viewDistance)
            return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > viewAngle * 0.5f)
            return false;

        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, viewDistance, obstacleMask))
        {
            if (hit.transform != player)
                return false;
        }

        return true;
    }

    void OnValidate()
    {
        SyncSpotLight();
    }

    void Start()
    {
        SyncSpotLight();
    }


void SyncSpotLight()
{
    if (!spotLight) return;

    spotLight.type = LightType.Spot;
    spotLight.spotAngle = viewAngle;     // full angle
    spotLight.range = viewDistance;
    spotLight.transform.position = transform.position;
    spotLight.transform.rotation = transform.rotation;
}


#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = detected ? Color.red : Color.green;

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        Vector3 up = transform.up;
        Vector3 right = transform.right;

        Gizmos.DrawLine(origin, origin + forward * viewDistance);

        int ringCount = 6;     // how many slices along length
        int segments = 24;     // how round the cone looks

        float halfAngleRad = viewAngle * 0.5f * Mathf.Deg2Rad;

        for (int r = 1; r <= ringCount; r++)
        {
            float t = (float)r / ringCount;
            float dist = viewDistance * t;

            float radius = Mathf.Tan(halfAngleRad) * dist;
            Vector3 center = origin + forward * dist;

            Vector3 prevPoint = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float theta = (float)i / segments * Mathf.PI * 2f;
                Vector3 offset =
                    right * Mathf.Cos(theta) * radius +
                    up * Mathf.Sin(theta) * radius;

                Vector3 point = center + offset;

                if (i > 0)
                    Gizmos.DrawLine(prevPoint, point);

                // draw spokes from origin only on outer ring
                if (r == ringCount)
                    Gizmos.DrawLine(origin, point);

                prevPoint = point;
            }
        }
    }

#endif
}
