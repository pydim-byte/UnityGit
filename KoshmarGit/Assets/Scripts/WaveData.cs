// WaveData.cs - Create via Assets > Create > Wave Data
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wave", menuName = "Game/Wave Data")]
public class WaveData : ScriptableObject
{
    [System.Serializable]
    public class SpawnEvent
    {
        [Tooltip("When to spawn (seconds from wave start)")]
        public float time = 0f;

        [Tooltip("What to spawn (null = random from pool)")]
        public GameObject enemyPrefab = null;

        [Tooltip("Spawn points to use. Each index spawns one enemy. Use -1 for random/area spawn.")]
        public int[] spawnPointIndices = new int[] { -1 };
    }

    public string waveName = "Wave";
    public List<SpawnEvent> spawnEvents = new List<SpawnEvent>();
    public float endDelay = 0f;
}