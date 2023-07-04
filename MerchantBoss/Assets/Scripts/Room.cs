using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public GameObject thankYouText;

    public static Room instance;

    public int level;
    public DungeonEntrance dungeonEntrance;
    public DungeonEntrance exit;
    [Tooltip("How many waves will be in this room")]
    public int waves = 1;
    [Tooltip("How many enemies to spawn in next wave")]
    public int batch = 2;

    [Header("Boundaries")]
    public Vector2 minMaxX;
    public Vector2 minMaxY;

    [Header("Other")]
    public GameObject spawnIndicatorPrefab;
    public GameObject[] enemiesToSpawn;
    public Transform[] spawnPositions;

    // The index of what to spawn next
    private int spawnIndex;
    // The index of where to spawn next
    private int positionIndex;

    private void Awake()
    {
        if (instance != null) Destroy(gameObject);
        else instance = this;
    }

    void Start()
    {
        LevelManager.instance.currentRoom = this;
        StartCoroutine(Spawn(3)); // 3
        exit.Exit();
    }

    public IEnumerator Spawn(int delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        // Don't reset indexes
        int startSpawnIndex = spawnIndex;
        int startPositionIndex = positionIndex;
        int nextBatch = spawnIndex + batch;

        while(spawnIndex < nextBatch)
        {
            // Spawn indicators first
            GameObject indicator = Instantiate(spawnIndicatorPrefab, spawnPositions[positionIndex].position, Quaternion.identity);
            Destroy(indicator, 4);
            spawnIndex++;
            positionIndex++;

            if (positionIndex >= spawnPositions.Length) positionIndex = 0;

            yield return new WaitForSeconds(.25f);
        }

        spawnIndex = startSpawnIndex;
        positionIndex = startPositionIndex;

        // Wait
        yield return new WaitForSeconds(1);

        while (spawnIndex < nextBatch)
        {
            // Then spawn enemies
            Enemy enemy = Instantiate(enemiesToSpawn[spawnIndex], spawnPositions[positionIndex].position, Quaternion.identity).GetComponent<Enemy>();
            //enemy.Spawn();
            LevelManager.instance.entities.Add(enemy);
            spawnIndex++;
            positionIndex++;

            if (positionIndex >= spawnPositions.Length) positionIndex = 0;

            yield return new WaitForSeconds(.25f);
        }

        waves--;
        if (waves == 2) batch++;
    }
}
