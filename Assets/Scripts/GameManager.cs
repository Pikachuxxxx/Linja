using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;


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

        FindFirstObjectByType<ColorSocketReceiver>()?.Shutdown();

        // restart level logic call SceneManager.LoadScene can be added here
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    void RespawnPlayer()
    {
        player.Respawn(spawnPoint.position);
        isRespawning = false;
    }
}
