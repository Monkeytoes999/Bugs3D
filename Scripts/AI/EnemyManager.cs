using System.Collections.Generic;
using Unity.Bugs.Game;
using UnityEngine;

namespace Unity.Bugs.AI
{
    public class EnemyManager : MonoBehaviour
    {
        public List<EnemyAI> Enemies { get; private set; }
        public int NumberOfEnemiesTotal { get; private set; }
        public int NumberOfEnemiesRemaining => Enemies.Count;

        void Awake()
        {
            Enemies = new List<EnemyAI>();
        }

        public void RegisterEnemy(EnemyAI enemy)
        {
            Enemies.Add(enemy);

            NumberOfEnemiesTotal++;
        }

        public void RegisterEnemy(EnemyController ec) {}
        public void UnregisterEnemy(EnemyController ec) {}


        public void UnregisterEnemy(EnemyAI enemyKilled)
        {
            int enemiesRemainingNotification = NumberOfEnemiesRemaining - 1;

            EnemyKillEvent evt = Events.EnemyKillEvent;
            evt.Enemy = enemyKilled.gameObject;
            evt.RemainingEnemyCount = enemiesRemainingNotification;
            EventManager.Broadcast(evt);

            Enemies.Remove(enemyKilled);
        }
    }
}