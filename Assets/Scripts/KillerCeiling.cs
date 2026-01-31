using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BladeCeilingTrap : MonoBehaviour
{
    public GameObject bladePrefab;
    public int bladeCount = 10;

    public float bladeLifetime = 1.5f;
    public float downwardImpulse = 0f;

    bool triggered;

    void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        SpawnBlades();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        triggered = false;
    }

    void SpawnBlades()
    {
        BoxCollider bc = GetComponent<BoxCollider>();
        Bounds b = bc.bounds;

        for (int i = 0; i < bladeCount; i++)
        {
            Vector3 spawnPos = new Vector3(
                Random.Range(b.min.x, b.max.x),
                b.max.y,
                Random.Range(b.min.z, b.max.z)
            );

            GameObject blade = Instantiate(bladePrefab, spawnPos, Quaternion.identity);

            if (!blade.TryGetComponent<Collider>(out _))
                blade.AddComponent<BoxCollider>();

            Rigidbody rb = blade.GetComponent<Rigidbody>();
            if (!rb) rb = blade.AddComponent<Rigidbody>();

            rb.useGravity = true;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            if (downwardImpulse > 0f)
                rb.AddForce(Vector3.down * downwardImpulse, ForceMode.Impulse);

            Destroy(blade, bladeLifetime);
        }
    }
}
