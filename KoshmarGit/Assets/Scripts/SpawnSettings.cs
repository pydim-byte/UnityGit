using UnityEngine;

[DisallowMultipleComponent]
public class SpawnSettings : MonoBehaviour
{
    [Tooltip("If true, use this prefab's spawn rotation instead of the spawner's global rotationOffset.")]
    public bool useSpawnRotation = true;

    [Tooltip("Euler degrees applied to the spawned instance (used when useSpawnRotation == true).")]
    public Vector3 spawnRotationEuler = Vector3.zero;

    [Tooltip("If true, use this prefab's spawn height instead of the spawner's global heightOffset.")]
    public bool useSpawnHeight = false;

    [Tooltip("Y offset added to spawn position (used when useSpawnHeight == true).")]
    public float spawnHeightOffset = 0f;
}
