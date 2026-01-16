using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace _Scripts.Core
{
    public class GameManager : MonoBehaviour
    {
        
        public float interval = 1;

        public int combo = 1;
        
        private bool _gameEnded;
        
        [SerializeField] private TMP_Text comboText;
        
        public static GameManager Instance;
        
        
        
        void Awake()
        {
            Instance = this;
            comboText.text = combo.ToString();
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

        public void UpdateCombo(bool entityDetected)
        {
            if(entityDetected) combo++;
            else
            {
                combo = 1;
            }
            
            comboText.text = combo.ToString();
        }
    }
}
