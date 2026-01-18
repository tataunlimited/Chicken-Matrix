using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Scripts.Core
{
    
    public enum Difficulty {Easy, Hard, KonamiMode}
    public class GameManager : MonoBehaviour
    {

        public float interval = 1;

        public int combo = 1;

        private bool _gameEnded;

        [SerializeField] private TMP_Text comboText;
        [SerializeField] private Image MainComboMeterRankIMG;
        [SerializeField] private Image SSSComboMeterRankIMG;
        [SerializeField] private Image fadeToBlack;

        [Header("Combo Rank Progression")]
        [SerializeField] private Sprite[] comboRankSprites;

        [Header("Screen Shake")]
        [SerializeField] private float baseShakeDuration = 1f;
        [SerializeField] private float baseShakeMagnitude = 0.5f;
        [SerializeField] private float miniShakeDuration = 0.05f;
        [SerializeField] private float miniShakeMagnitude = 0.03f;

        [Header("Combo Text Pulse")]
        [SerializeField] private float pulseDuration = 0.15f;
        [SerializeField] private float pulseScale = 1.5f;

        [Header("Rank Up Effects")]
        [SerializeField] private GameObject rankUpExplosionPrefab;

        private Camera mainCamera;
        private Coroutine comboPulseCoroutine;
        private Vector3 comboTextOriginalScale;
        private Vector3 originalCameraPosition;
        private Coroutine shakeCoroutine;
        private float currentShakeDuration;
        private float currentShakeMagnitude;
        private int currentRankIndex = -1;

        // Neutral barrage auto-combo timer (combo 51-60)
        private Coroutine neutralBarrageCoroutine;
        private const float NeutralBarrageComboInterval = 1.6f;

        public static GameManager Instance; 
        public static Difficulty Difficulty = Difficulty.Easy;
        
        
        
        void Awake()
        {
            Instance = this;
            comboText.text = combo.ToString();
            comboTextOriginalScale = comboText.transform.localScale;
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                originalCameraPosition = mainCamera.transform.localPosition;
            }
            if (SSSComboMeterRankIMG != null)
            {
                SSSComboMeterRankIMG.enabled = false;
            }
              //  SSSComboMeterRankIMG.enabled = false;
            if (MainComboMeterRankIMG != null)
            {
                MainComboMeterRankIMG.enabled = false;
            }
        }
        
        private void Start()
        {
            Cursor.lockState = CursorLockMode.Confined;
            StartCoroutine(UpdateInterval());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                //increase combo by 10 for testing
                UpdateCombo(true, 10);
            }
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

        #region Combo System Management

        private void PulseComboText()
        {
            if (comboText == null) return;

            if (comboPulseCoroutine != null)
            {
                StopCoroutine(comboPulseCoroutine);
                comboText.transform.localScale = comboTextOriginalScale;
            }

            comboPulseCoroutine = StartCoroutine(PulseComboTextCoroutine());
        }

        private IEnumerator PulseComboTextCoroutine()
        {
            float elapsed = 0f;
            float halfDuration = pulseDuration / 2f;

            // Scale up
            while (elapsed < halfDuration)
            {
                float t = elapsed / halfDuration;
                comboText.transform.localScale = Vector3.Lerp(comboTextOriginalScale, comboTextOriginalScale * pulseScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Scale back down
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                float t = elapsed / halfDuration;
                comboText.transform.localScale = Vector3.Lerp(comboTextOriginalScale * pulseScale, comboTextOriginalScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            comboText.transform.localScale = comboTextOriginalScale;
            comboPulseCoroutine = null;
        }

        public void UpdateCombo(bool entityDetected, int comboValue = 1)
        {
            if (entityDetected)
            {
                combo += comboValue;
                MiniShakeScreen();
                UpdateComboRankDisplay();
            }
            else
            {
                // Only shake if we actually had a combo to lose
                if (combo > 1)
                {
                    ShakeScreen(combo);
                    EnemySpawner.Instance.ClearAllEntities();

                    // Play combo fail sound
                    SoundController.Instance?.PlayComboFailSound();
                    
                    // Notify egg manager of combo fail (halves/resets egg score)
                    EggManager.Instance?.OnEntityComboFail();

                }
                // Hard mode: reset to 1
                // Easy mode: snap to start of previous tier (1, 11, 21, 31, etc.) to maintain music sync
                if (Difficulty == Difficulty.Hard)
                {
                    combo = 1;
                }
                else
                {
                    // Calculate previous tier start: floor to nearest 10, then +1
                    // e.g., combo 25 -> tier start 21, combo 15 -> tier start 11, combo 5 -> tier start 1
                    int currentTier = (combo - 1) / 10; // 0-based tier index
                    int previousTierStart = Mathf.Max((currentTier - 1) * 10 + 1, 1);
                    combo = previousTierStart;
                }
                UpdateComboRankDisplay();

                // Sync music and spawner to new combo position
                SoundController.Instance?.SetTrackTimeForCombo(combo);
                EnemySpawner.Instance?.SyncToCombo(combo);
            }

            comboText.text = combo.ToString();
            PulseComboText();

            // Update music based on combo
            if (SoundController.Instance != null)
            {
                SoundController.Instance.UpdateMusicForCombo(combo);

                // Punch volume on successful kill
                if (entityDetected)
                {
                    SoundController.Instance.PunchVolume();
                }
            }

            // Check if we need to start/stop neutral barrage auto-combo
            UpdateNeutralBarrageState();

            if (combo >= 100 && !_gameEnded)
            {

                switch (Difficulty)
                {
                    case Difficulty.Easy:
                        PlayerPrefs.SetInt("Trophy_Easy", 1);
                        break;
                    case Difficulty.Hard:
                        PlayerPrefs.SetInt("Trophy_Hard", 1);
                        break;
                    case Difficulty.KonamiMode:
                        PlayerPrefs.SetInt("Trophy_Konami", 1);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                StartCoroutine(VictorySequence());
            }
        }

        private IEnumerator VictorySequence()
        {
            _gameEnded = true;

            // Stop spawning new entities
            EnemySpawner.Instance.StopSpawning();

            // Trigger rank up effects (explosion particles)
            OnRankUp();

            // Destroy all entities on the board with particles
            EnemySpawner.Instance.DestroyAllEntitiesWithParticles();

            // Start the final track and get its duration
            float trackDuration = 0f;
            if (SoundController.Instance != null)
            {
                trackDuration = SoundController.Instance.PlayFinalTrack();
            }

            // Start cycling radar colors
            if (RadarBackgroundGenerator.Instance != null)
            {
                RadarBackgroundGenerator.Instance.StartVictoryColorCycle();
            }

            // Start the credits sequence - it should complete 3 seconds before track ends
            // Credits will handle its own fade-in delay and scroll timing
            float creditsAvailableTime = Mathf.Max(0f, trackDuration - 3f);
            if (CreditsController.Instance != null && creditsAvailableTime > 0f)
            {
                CreditsController.Instance.StartCredits(creditsAvailableTime);
            }

            // Continue pulsing the radar and cycling colors while the final track plays
            // Wait until 3 seconds before the track ends to start fading
            float fadeStartDelay = Mathf.Max(0f, trackDuration - 3f);

            float elapsed = 0f;
            while (elapsed < fadeStartDelay)
            {
                elapsed += interval;
                yield return new WaitForSeconds(interval / 2);

                // Pulse radar on beat
                if (RadarBackgroundGenerator.Instance != null)
                {
                    RadarBackgroundGenerator.Instance.CycleVictoryColor();
                    RadarBackgroundGenerator.Instance.Pulse();
                }

                yield return new WaitForSeconds(interval / 2);
            }

            // Stop color cycling
            if (RadarBackgroundGenerator.Instance != null)
            {
                RadarBackgroundGenerator.Instance.StopVictoryColorCycle();
            }

            // Fade to black over the final 3 seconds
            fadeToBlack.gameObject.SetActive(true);
            fadeToBlack.DOFade(1, 3f);
            yield return new WaitForSeconds(3.5f);

            // Transition to chicken room
            SceneManager.LoadScene("ChickenScene");
        }

        /// <summary>
        /// Check if we're in the neutral barrage phase and manage the auto-combo timer.
        /// </summary>
        private void UpdateNeutralBarrageState()
        {
            bool inNeutralBarrage = combo >= 51 && combo <= 60;

            if (inNeutralBarrage && neutralBarrageCoroutine == null)
            {
                // Start auto-combo timer
                neutralBarrageCoroutine = StartCoroutine(NeutralBarrageCoroutine());
            }
            else if (!inNeutralBarrage && neutralBarrageCoroutine != null)
            {
                // Stop auto-combo timer
                StopCoroutine(neutralBarrageCoroutine);
                neutralBarrageCoroutine = null;
            }
        }

        private IEnumerator NeutralBarrageCoroutine()
        {
            while (combo >= 51 && combo <= 60)
            {
                yield return new WaitForSeconds(NeutralBarrageComboInterval);

                // Only increment if still in barrage range
                if (combo >= 51 && combo < 61)
                {
                    combo++;
                    comboText.text = combo.ToString();
                    PulseComboText();
                    MiniShakeScreen();
                    UpdateComboRankDisplay();

                    if (SoundController.Instance != null)
                    {
                        SoundController.Instance.UpdateMusicForCombo(combo);
                        SoundController.Instance.PunchVolume();
                    }
                }
            }

            neutralBarrageCoroutine = null;
        }

        private void UpdateComboRankDisplay()
        {
            if (comboRankSprites == null || comboRankSprites.Length == 0)
            {
                Debug.LogWarning("Combo Rank Sprites array is empty! Please assign 7 sprites in order: D, C, B, A, S, SS, SSS");
                return;
            }

            // Calculate which rank sprite to display based on combo value
            int rankIndex = GetRankIndex(combo);

            // Only update if the rank has changed to avoid unnecessary updates
            if (rankIndex != currentRankIndex)
            {
                // Check if rank increased (not decreased or reset)
                bool rankIncreased = rankIndex > currentRankIndex && currentRankIndex >= 0;

                currentRankIndex = rankIndex;

                // Trigger rank up effects when ranking up
                if (rankIncreased)
                {
                    OnRankUp(); 
                }

                // ===== NO RANK STATE (Combo 0-10) =====
                if (rankIndex == -1)
                {
                    // Disable main meter - combo doesn't meet minimum
                    if (MainComboMeterRankIMG != null)
                    {
                        MainComboMeterRankIMG.enabled = false;
                    }
                     //   MainComboMeterRankIMG.enabled = false;

                    if (SSSComboMeterRankIMG != null)
                    {
                        SSSComboMeterRankIMG.enabled = false;
                    }
                      //  SSSComboMeterRankIMG.enabled = false;

                    Debug.Log("Combo Dropped! Returning to No Rank state. Main meter disabled.");
                }

                // ===== MAIN COMBO METER VISIBILITY (Ranks D-SS) =====
                else if (rankIndex >= 0 && rankIndex < 6)
                {
                    // Enable main meter and set sprite
                    if (MainComboMeterRankIMG != null)
                    {
                        MainComboMeterRankIMG.enabled = true;
                        MainComboMeterRankIMG.sprite = comboRankSprites[rankIndex];
                    }

                    // Disable SSS meter for non-SSS ranks
                    if (SSSComboMeterRankIMG != null)
                    {
                        SSSComboMeterRankIMG.enabled = false;
                    }
                       // SSSComboMeterRankIMG.enabled = false;
                }

                // ===== SSS COMBO METER VISIBILITY =====
                else if (rankIndex == 6)
                {
                    // Show SSS meter and hide main meter
                    if (SSSComboMeterRankIMG != null)
                    {
                        SSSComboMeterRankIMG.enabled = true;
                        SSSComboMeterRankIMG.sprite = comboRankSprites[rankIndex];
                    }

                    // Disable main meter when SSS is reached
                    if (MainComboMeterRankIMG != null)
                    {
                        MainComboMeterRankIMG.enabled = false;
                    }
                       // MainComboMeterRankIMG.enabled = false;
                }

                string[] rankNames = { "D", "C", "B", "A", "S", "SS", "SSS" };
                string rankName = rankIndex >= 0 ? rankNames[rankIndex] : "None";
                Debug.Log($"Combo Rank Updated: Combo = {combo}, Rank = {rankName}");
            }
        }

        private void OnRankUp()
        {
            // Play rank-up sound
            SoundController.Instance?.PlayRankUpSound();

            // Spawn explosion at origin with radar grid color
            if (rankUpExplosionPrefab != null)
            {
                var explosion = Instantiate(rankUpExplosionPrefab, Vector3.zero, Quaternion.identity);

                // Set particle color to match radar grid (ensure full alpha)
                var particleSystem = explosion.GetComponent<ParticleSystem>();
                if (particleSystem != null && RadarBackgroundGenerator.Instance != null)
                {
                    var main = particleSystem.main;
                    Color radarColor = RadarBackgroundGenerator.Instance.GetCurrentComboColor();
                    radarColor.a = 1f;
                    main.startColor = radarColor;
                }
            }

            // Destroy all entities with particles (but don't update combo)
            EnemySpawner.Instance.DestroyAllEntitiesWithParticles();
        }

        /// <summary>
        /// Gets the rank index based on combo value
        /// Returns -1 for no rank (0-10)
        /// 11-20: D (0), 21-30: C (1), 31-50: B (2), 51-60: A (3), 61-70: S (4), 71-89: SS (5), 90-100: SSS (6)
        /// </summary>
        private int GetRankIndex(int comboValue)
        {
            if (comboValue <= 10) return -1;       // No rank display
            if (comboValue <= 20) return 0;        // D Rank
            if (comboValue <= 30) return 1;        // C Rank
            if (comboValue <= 50) return 2;        // B Rank
            if (comboValue <= 60) return 3;        // A Rank
            if (comboValue <= 70) return 4;        // S Rank
            if (comboValue <= 89) return 5;        // SS Rank
            return 6;                              // SSS Rank (90+)
        }

        #endregion

        #region Shake System

        private void MiniShakeScreen()
        {
            if (mainCamera == null) return;

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                mainCamera.transform.localPosition = originalCameraPosition;
            }

            currentShakeDuration = miniShakeDuration;
            currentShakeMagnitude = miniShakeMagnitude;

            shakeCoroutine = StartCoroutine(ShakeCoroutine());
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

        public void TriggerKonamiEffect()
        {
            Debug.Log("Konami Code Triggered!");
            Difficulty = Difficulty.KonamiMode;
            PlayerController.Instance.EnableKonamiMode();
        }
    }
    #endregion

}
