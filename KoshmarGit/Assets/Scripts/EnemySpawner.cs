using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Transform")]
    public float heightOffset = 2f;
    public Vector3 rotationOffset = new Vector3(0f, 90f, 0f);
    public enum SpawnMode { Points, Area }
    public SpawnMode spawnMode = SpawnMode.Points;

    [Header("Default Prefabs")]
    public GameObject defaultEnemyPrefab;
    public GameObject[] enemyPrefabs;

    [Header("Spawn Warning")]
    public GameObject spawnWarningPrefab;
    public float warningTime = 2f;
    public float warningHeightOffset = 0.05f;

    [Header("Spawn points")]
    public Transform[] spawnPoints;

    [Header("Area")]
    public Vector3 areaCenter = Vector3.zero;
    public Vector3 areaSize = new Vector3(10f, 0f, 10f);

    [Header("Settings")]
    public int maxSimultaneous = 10;
    public bool startOnAwake = true;
    public bool randomizeSpawnPoint = true;

    [Header("Waves - Just drag ScriptableObjects here!")]
    public List<WaveData> waves = new List<WaveData>();
    public bool loopWaves = false;

    [Header("Shop System")]
    public int healthIncrementPerShop = 10;
    public int[] wavesToOpenShop = { 3, 6 };
    public GameModeManager gameModeManager;

    private int _waveCounter = 0;
    private int _extraEnemyHealth = 0;
    private int _currentAlive;
    private bool _running = false;
    private Coroutine _waveCoroutine = null;

    void Awake()
    {
        if (startOnAwake) StartSpawning();
    }

    public void StartSpawning()
    {
        _running = true;
        if (_waveCoroutine != null) StopCoroutine(_waveCoroutine);
        _waveCoroutine = StartCoroutine(RunWaves());
    }

    public void StopSpawning()
    {
        _running = false;
        if (_waveCoroutine != null)
        {
            StopCoroutine(_waveCoroutine);
            _waveCoroutine = null;
        }
    }

    private GameObject PickPrefab(GameObject instructionPrefab)
    {
        if (instructionPrefab != null) return instructionPrefab;
        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
            return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        return defaultEnemyPrefab;
    }

    private IEnumerator RunWaves()
    {
        if (waves == null || waves.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: No waves configured.");
            yield break;
        }

        do
        {
            for (int w = 0; w < waves.Count; w++)
            {
                WaveData wave = waves[w];

                // Sort by time
                wave.spawnEvents.Sort((a, b) => a.time.CompareTo(b.time));

                List<Coroutine> scheduledSpawns = new List<Coroutine>();
                float waveStartTime = Time.time;

                foreach (var ev in wave.spawnEvents)
                {
                    float targetTime = waveStartTime + ev.time;
                    while (_running && Time.time < targetTime)
                        yield return null;

                    if (!_running) yield break;

                    // Spawn one enemy at each specified spawn point
                    foreach (int spawnIndex in ev.spawnPointIndices)
                    {
                        if (!_running) break;
                        if (_currentAlive >= maxSimultaneous) break;

                        Vector3 pos;
                        if (spawnMode == SpawnMode.Points && spawnPoints != null &&
                            spawnIndex >= 0 && spawnIndex < spawnPoints.Length)
                        {
                            // Use the specific spawn point
                            pos = spawnPoints[spawnIndex].position;
                        }
                        else
                        {
                            // Use random/area spawn for -1 or invalid indices
                            pos = GetSpawnPosition();
                        }

                        GameObject prefabToSpawn = PickPrefab(ev.enemyPrefab);
                        if (prefabToSpawn == null)
                        {
                            Debug.LogWarning("EnemySpawner: No prefab available. Skipping.");
                            continue;
                        }

                        scheduledSpawns.Add(StartCoroutine(SpawnWithWarningAtPosition(pos, prefabToSpawn)));
                    }
                }

                foreach (var c in scheduledSpawns)
                    yield return c;

                while (_running && _currentAlive > 0)
                    yield return null;

                _waveCounter++;

                int waveIndexInLoop = (w % waves.Count) + 1;
                if (System.Array.Exists(wavesToOpenShop, x => x == waveIndexInLoop))
                {
                    if (gameModeManager != null)
                        gameModeManager.OpenShop();
                    _extraEnemyHealth += healthIncrementPerShop;
                }

                if (_running && wave.endDelay > 0f)
                {
                    float endTime = Time.time + wave.endDelay;
                    while (_running && Time.time < endTime)
                        yield return null;
                }
            }
        } while (loopWaves && _running);

        _waveCoroutine = null;
        _running = false;
    }

    // Spawn coroutine now inspects prefab for type-specific offsets
    private IEnumerator SpawnWithWarningAtPosition(Vector3 spawnPos, GameObject prefab)
    {
        GameObject warning = null;
        if (spawnWarningPrefab != null)
        {
            Vector3 warningPos = spawnPos + Vector3.up * warningHeightOffset;
            warning = Instantiate(spawnWarningPrefab, warningPos, Quaternion.Euler(90f, 0f, 0f));
        }

        // wait warning
        yield return new WaitForSeconds(warningTime);

        // check cap
        if (_currentAlive >= maxSimultaneous)
        {
            if (warning != null) Destroy(warning);
            yield break;
        }

        // Determine per-prefab offsets (reading from the prefab asset)
        // NOTE: prefab parameter is the prefab asset, GetComponent works on it.
        SpawnSettings prefabSettings = (prefab != null) ? prefab.GetComponent<SpawnSettings>() : null;

        // Choose rotation: prefab override -> spawner default
        Quaternion spawnRotation;
        if (prefabSettings != null && prefabSettings.useSpawnRotation)
        {
            spawnRotation = Quaternion.Euler(prefabSettings.spawnRotationEuler);
        }
        else
        {
            spawnRotation = Quaternion.Euler(rotationOffset);
        }

        // Choose height: prefab override -> spawner default
        float spawnHeight = (prefabSettings != null && prefabSettings.useSpawnHeight)
            ? prefabSettings.spawnHeightOffset
            : heightOffset;

        // Instantiate with chosen rotation and height
        Vector3 finalPos = spawnPos + Vector3.up * spawnHeight;
        GameObject enemy = Instantiate(prefab, finalPos, spawnRotation);

        // Apply extra health and hook death callback
        EnemyHealth healthScript = enemy.GetComponent<EnemyHealth>();
        if (healthScript != null)
        {
            healthScript.maxHealth += _extraEnemyHealth;
            healthScript.OnDeath += OnEnemyDestroyed;
        }
        else
        {
            Debug.LogWarning("EnemySpawner: spawned prefab has no EnemyHealth component.");
        }

        _currentAlive++;

        if (warning != null)
            Destroy(warning);
    }



    Vector3 GetSpawnPosition()
    {
        if (spawnMode == SpawnMode.Points && spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform t = randomizeSpawnPoint ?
                spawnPoints[Random.Range(0, spawnPoints.Length)] :
                spawnPoints[0];
            return t.position;
        }
        else
        {
            Vector3 localRandom = new Vector3(
                Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
                Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f),
                Random.Range(-areaSize.z * 0.5f, areaSize.z * 0.5f)
            );
            return transform.TransformPoint(areaCenter + localRandom);
        }
    }

    public void OnEnemyDestroyed()
    {
        _currentAlive = Mathf.Max(0, _currentAlive - 1);
    }

    void OnDrawGizmosSelected()
    {
        if (spawnMode == SpawnMode.Area)
        {
            Gizmos.color = new Color(1f, 0.5f, 0.0f, 0.25f);
            Matrix4x4 tr = Matrix4x4.TRS(transform.TransformPoint(areaCenter), transform.rotation, transform.lossyScale);
            Gizmos.matrix = tr;
            Gizmos.DrawCube(Vector3.zero, areaSize);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}