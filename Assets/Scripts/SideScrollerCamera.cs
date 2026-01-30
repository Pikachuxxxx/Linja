using UnityEngine;

public class SideScrollerCamera : MonoBehaviour
{
    public Transform target;

    [Header("Offsets")]
    public Vector2 offset = new Vector2(0f, 1.5f);

    [Header("Dead Zone")]
    public float deadZoneX = 2.0f;
    public float deadZoneY = 1.2f;

    [Header("Smoothing")]
    public float smoothTimeX = 0.15f;
    public float smoothTimeY = 0.20f;

    [Header("Look Ahead")]
    public float lookAheadDistance = 2.0f;
    public float lookAheadSmooth = 0.2f;

    float lookAhead;
    float lookAheadVelocity;
    float velX;
    float velY;
    float lastTargetX;

    void Start()
    {
        if (target == null)
        {
            enabled = false;
            return;
        }

        lastTargetX = target.position.x;
    }

    void LateUpdate()
    {
        Vector3 camPos = transform.position;
        Vector3 targetPos = target.position + (Vector3)offset;

        float dx = target.position.x - lastTargetX;
        lastTargetX = target.position.x;

        float desiredLookAhead = Mathf.Sign(dx) * lookAheadDistance;
        lookAhead = Mathf.SmoothDamp(
            lookAhead,
            desiredLookAhead,
            ref lookAheadVelocity,
            lookAheadSmooth
        );

        targetPos.x += lookAhead;

        float diffX = targetPos.x - camPos.x;
        if (Mathf.Abs(diffX) > deadZoneX)
        {
            float targetX = camPos.x + (diffX - Mathf.Sign(diffX) * deadZoneX);
            camPos.x = Mathf.SmoothDamp(camPos.x, targetX, ref velX, smoothTimeX);
        }

        float diffY = targetPos.y - camPos.y;
        if (Mathf.Abs(diffY) > deadZoneY)
        {
            float targetY = camPos.y + (diffY - Mathf.Sign(diffY) * deadZoneY);
            camPos.y = Mathf.SmoothDamp(camPos.y, targetY, ref velY, smoothTimeY);
        }

        transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
    }
}
