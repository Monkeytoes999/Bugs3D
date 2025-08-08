using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Bugs.AI;

namespace Unity.Bugs.AI {
    public class BugSpawner : MonoBehaviour {

        [Header("Other")] [Tooltip("Max Bugs")]
        public int MaxBugs = 10;

        [Tooltip("Max Bugs")]
        public GameObject bugPrefab;

        [Tooltip("Max Bugs")]
        public EnemyManager enemyManager;

        [Tooltip("Spawn Delay")]
        public int delay = 1;

        public int xMax;
        public int xMin;
        public int zMax;
        public int zMin;

        public int timeToScale = 30;

        float lastSpawn = 0;
        

        void Start() {
            
        }

        void Update() {
            float diffScaling = Mathf.Ceil(Time.time/timeToScale);

            if (Time.time > lastSpawn + (delay/diffScaling) && enemyManager.NumberOfEnemiesRemaining < (MaxBugs*diffScaling)) {
                lastSpawn = Time.time;
                Instantiate(bugPrefab, new Vector3(Random.Range(xMin, xMax), 0f, Random.Range(zMin, zMax)), Quaternion.identity, gameObject.transform);
                //Instantiate(bugPrefab, new Vector3(gameObject.transform.position.x + Random.Range(-5, 5), 0f, gameObject.transform.position.z + Random.Range(-5, 5)), Quaternion.identity);
            }
        }
    }
}