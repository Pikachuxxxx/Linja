using UnityEngine;

public class KillZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("KillZone triggered by " + other.name);
        NinjaController player = other.GetComponent<NinjaController>();
        if (player != null)
            GameManager.Instance.PlayerDied();
    }
}
