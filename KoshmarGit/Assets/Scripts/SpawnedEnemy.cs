using UnityEngine;

// Add this small helper to each spawned enemy so the spawner is notified when it gets destroyed.
public class SpawnedEnemy : MonoBehaviour
{
    public EnemySpawner spawner;

    void OnDestroy()
    {
        if (spawner != null)
            spawner.OnEnemyDestroyed();
    }
}
