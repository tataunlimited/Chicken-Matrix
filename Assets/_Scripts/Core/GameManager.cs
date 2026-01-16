using System;
using System.Collections;
using UnityEngine;

namespace _Scripts.Core
{
    public class GameManager : MonoBehaviour
    {
        
        public float interval = 1;
        
        private bool _gameEnded;
        
        public static GameManager Instance;
        
        void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            StartCoroutine(UpdateInterval());
        }

        IEnumerator UpdateInterval()
        {
            if(_gameEnded)
                yield break;
            EnemySpawner.Instance.UpdateEnemies();
            yield return new WaitForSeconds(interval/2);

            PlayerController.Instance.UpdateInterval();
            yield return new WaitForSeconds(interval/2);
            
            StartCoroutine(UpdateInterval());
        }
    }
}
