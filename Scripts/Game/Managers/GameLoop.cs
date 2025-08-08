using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unity.Bugs.Game {
    public class GameLoop : MonoBehaviour {

        [Header("Loss")] [Tooltip("The menu scene")]
        public string menu = "LoseScene";

        [Tooltip("This scene again")]
        public string restart = "LoseScene";

        [Header("Other")] [Tooltip("Kill Counter")]
        public Text KillCounter;

        [Tooltip("Menu")]
        public GameObject deathNav;

        public bool gamePaused {get; set; }
        public bool gameOver {get; private set; }

        float overDelay;
        int bugsKilled = 0;

        void Awake() {
            //EventManager.AddListener<AllObjectivesCompletedEvent>(OnAllObjectivesCompleted);
            EventManager.AddListener<PlayerDeathEvent>(OnPlayerDeath);
        }

        void Start() {
            AudioUtility.SetMasterVolume(1);
        }

        void Update() {
            if (gameOver) {
                deathNav.SetActive(true);
            }
        }

        void OnPlayerDeath(PlayerDeathEvent evt) {
            EndGame();
        }

        void EndGame() {
            gameOver = true;
            Time.timeScale = 0f;
        }

        public void Restart() {
            SceneManager.LoadScene(restart);
            gameOver = false;
            Time.timeScale = 1f;
        }

        public void ToMenu() {
            SceneManager.LoadScene(menu);
            gameOver = false;
            Time.timeScale = 1f;
        }

        public void bugKilled() {
            bugsKilled += 1;
            KillCounter.text = "Bugs Killed: " + bugsKilled;
        }

        void OnDestroy() {
            //EventManager.RemoveListener<AllObjectivesCompletedEvent>(OnAllObjectivesCompleted);
            EventManager.RemoveListener<PlayerDeathEvent>(OnPlayerDeath);
        }
    }
}
