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

        [Header("Screen Shake")]
        [SerializeField] private float baseShakeDuration = 1f;
        [SerializeField] private float baseShakeMagnitude = 0.5f;

        private Camera mainCamera;
        private Vector3 originalCameraPosition;
        private Coroutine shakeCoroutine;
        private float currentShakeDuration;
        private float currentShakeMagnitude;

        public static GameManager Instance; 
        
        
        
        void Awake()
        {
            Instance = this;
            comboText.text = combo.ToString();
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                originalCameraPosition = mainCamera.transform.localPosition;
            }
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
            // Pulse radar on beat
            if (RadarBackgroundGenerator.Instance != null)
                RadarBackgroundGenerator.Instance.Pulse();
            yield return new WaitForSeconds(interval/2); 

            StartCoroutine(UpdateInterval());
        }

        public void UpdateCombo(bool entityDetected)
        {
            if(entityDetected) combo++;
            else
            {
                // Only shake if we actually had a combo to lose
                if (combo > 1)
                {
                    ShakeScreen(combo);
                    EnemySpawner.Instance.ClearAllEntities();
                }
                combo = 1;
            }

            comboText.text = combo.ToString();
        }

        public void ShakeScreen(int lostCombo)
        {
            if (mainCamera == null) return;

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                mainCamera.transform.localPosition = originalCameraPosition;
            }

            // Calculate shake intensity based on lost combo (capped at 10 levels)
            int level = Mathf.Clamp(lostCombo, 1, 10);
            currentShakeDuration = baseShakeDuration + level / 100f;
            currentShakeMagnitude = baseShakeMagnitude + level / 10f;

            shakeCoroutine = StartCoroutine(ShakeCoroutine());
        }

        private IEnumerator ShakeCoroutine()
        {
            float elapsed = 0f;

            while (elapsed < currentShakeDuration)
            {
                // Fade out the magnitude over time
                float t = elapsed / currentShakeDuration;
                float fadedMagnitude = currentShakeMagnitude * (1f - t);

                float x = UnityEngine.Random.Range(-1f, 1f) * fadedMagnitude;
                float y = UnityEngine.Random.Range(-1f, 1f) * fadedMagnitude;

                mainCamera.transform.localPosition = originalCameraPosition + new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            mainCamera.transform.localPosition = originalCameraPosition;
            shakeCoroutine = null;
        }
    }
}
