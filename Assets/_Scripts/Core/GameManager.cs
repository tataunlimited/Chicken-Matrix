using System;
using System.Collections;
using UnityEngine;

namespace _Scripts.Core
{
    public class GameManager : MonoBehaviour
    {
        
        public float interval = 1;
        
        private bool _gameEnded;
        
        private void Start()
        {
            StartCoroutine(UpdateInterval());
        }

        IEnumerator UpdateInterval()
        {
            if(_gameEnded)
                yield break;
            EnemySpawner.Instance.UpdateEnemies();
            yield return new WaitForSeconds(interval);
            
            StartCoroutine(UpdateInterval());
        }
    }
}
