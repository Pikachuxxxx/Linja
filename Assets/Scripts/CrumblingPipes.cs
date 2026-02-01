using UnityEngine;
[RequireComponent(typeof(CapsuleCollider))]
public class CrumblingPipe : MonoBehaviour
{
    public float crumbleDelay = 0.3f;
    public GameObject brokenPipePrefab;
    float timer;
    bool collapsing;

    void OnCollisionStay(Collision c)
    {
        var player = c.gameObject.GetComponent<NinjaController>();
        if (!player) return;

        timer += Time.deltaTime;
        if (timer >= crumbleDelay && !collapsing)
            Collapse();

    }
    void OnCollisionExit(Collision c)
    {
        var player = c.gameObject.GetComponent<NinjaController>();
        if (!player) return;
        
        timer = 0f;
        collapsing = false;
        GetComponent<Collider>().enabled = true;
    }
    void Collapse()
    {
        collapsing = true;
        GetComponent<Collider>().enabled = false;
        // TODO: enable rigidbody or spawn fragments here, broken pipe prefab
        // spawn a prefab for this --> that will rigidbodies and falldown, individually
        GameObject brokenPipe = Instantiate(brokenPipePrefab, transform.position, transform.rotation);
        Destroy(brokenPipe, 4.0f);
        Destroy(gameObject);
    }
}
