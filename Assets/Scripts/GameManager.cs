using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public NinjaController player;
    public Transform spawnPoint;

    public float respawnDelay = 1.5f;

    bool isRespawning;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void PlayerDied()
    {
        if (isRespawning)
            return;

        isRespawning = true;
        player.Kill();
        Invoke(nameof(RespawnPlayer), respawnDelay);
    }

    void RespawnPlayer()
    {
        player.Respawn(spawnPoint.position);
        isRespawning = false;
    }
}
