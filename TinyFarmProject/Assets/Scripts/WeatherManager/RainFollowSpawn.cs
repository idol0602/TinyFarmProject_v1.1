using UnityEngine;
using UnityEngine.SceneManagement;

public class RainFollowSpawn : MonoBehaviour
{
    [Header("Rain Spawn Point")]
    [SerializeField] private GameObject rainSpawn;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        MoveToSpawnPoint();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        MoveToSpawnPoint();
    }

    private void MoveToSpawnPoint()
    {
        if (rainSpawn == null)
        {
            Debug.LogWarning("⚠ RainSpawn chưa được gán trong Inspector");
            return;
        }

        transform.position = rainSpawn.transform.position;
        Debug.Log("🌧 RainParticle moved to spawn point");
    }
}
