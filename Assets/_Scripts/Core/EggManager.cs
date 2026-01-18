using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _Scripts.Core
{
    /// <summary>
    /// Manages the egg collection mini-game system.
    /// Spawns eggs on radar rings, handles beat-synced click detection,
    /// manages scoring with multipliers, and integrates with spin charge system.
    /// </summary>
    public class EggManager : MonoBehaviour
    {
        public static EggManager Instance { get; private set; }

        [Header("Egg Settings")]
        [SerializeField] private EggCollectable eggPrefab;
        [Tooltip("Detection radius around mouse cursor for clicking eggs")]
        [SerializeField] private float clickDetectionRadius = 1f;
        [Tooltip("Time window around the beat pulse where clicks are valid (in seconds)")]
        [SerializeField] private float beatWindowTime = 0.15f;

        /// <summary>
        /// Public accessor for beat window time (used by EggCollectable for growth animation)
        /// </summary>
        public float BeatWindowTime => beatWindowTime;

        // Ring radii are fetched automatically from RadarBackgroundGenerator
        private float[] _ringRadii;

        [Header("Particle Effects")]
        [SerializeField] private ParticleSystem eggExplosionPrefab;
        [Tooltip("Particle colors for each multiplier level: x1, x2, x4, x4+ (index 0-3)")]
        [SerializeField] private Color[] multiplierParticleColors = new Color[]
        {
            new Color(1f, 1f, 0.5f, 1f),    // x1 - Light yellow
            new Color(1f, 0.8f, 0f, 1f),    // x2 - Orange-yellow
            new Color(1f, 0.5f, 0f, 1f),    // x4 - Orange
            new Color(1f, 0.2f, 0f, 1f)     // x4+ - Red-orange
        };
        [SerializeField] private int eggParticleBurstCount = 20;

        [Header("UI References")]
        [SerializeField] private TMP_Text eggScoreText;
        [SerializeField] private TMP_Text multiplierText;
        [SerializeField] private GameObject multiplierContainer;

        [Header("UI Animation")]
        [SerializeField] private float textPulseDuration = 0.15f;
        [SerializeField] private float textPulseScale = 1.5f;

        [Header("Spin Charge Integration")]
        [Tooltip("Fixed spin charge percentage per egg collected")]
        [SerializeField] private float spinChargePerEgg = 0.25f;

        // Scoring
        private int _eggScore;
        private int _consecutiveEggs;
        private int _currentMultiplierLevel; // 0=x1, 1=x2, 2=x4, 3=x8, 4=x16 (capped at 3 for x4 display but value goes to x16)

        // Current egg state
        private EggCollectable _currentEgg;
        private bool _eggClickedThisPulse;
        private bool _eggReadyForCollection; // True after the egg has survived one pulse (collection window is next pulse)
        private bool _inBeatWindow;
        private float _beatWindowTimer;

        // UI animation
        private Coroutine _scorePulseCoroutine;
        private Coroutine _multiplierPulseCoroutine;
        private Vector3 _scoreTextOriginalScale;
        private Vector3 _multiplierTextOriginalScale;

        // Multiplier values for each consecutive egg level
        private static readonly int[] MultiplierAdditions = { 1, 2, 4, 8, 16 };

        private void Awake()
        {
            Instance = this;

            if (eggScoreText != null)
                _scoreTextOriginalScale = eggScoreText.transform.localScale;
            if (multiplierText != null)
                _multiplierTextOriginalScale = multiplierText.transform.localScale;
        }

        private void Start()
        {
            // Get ring radii from RadarBackgroundGenerator
            FetchRingRadii();

            UpdateUI();

            // Subscribe to player pulse event for beat timing
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.OnPulse += OnBeatPulse;
            }

            // Auto-start egg spawning (skip SpawnedThisPulse check for initial spawn)
            SpawnEggInitial();
        }

        /// <summary>
        /// Fetch ring radii from RadarBackgroundGenerator
        /// </summary>
        private void FetchRingRadii()
        {
            if (RadarBackgroundGenerator.Instance != null)
            {
                _ringRadii = RadarBackgroundGenerator.Instance.RingRadii;

                // If radii not yet initialized (radar hasn't generated), calculate them
                if (_ringRadii == null || _ringRadii.Length == 0)
                {
                    int ringCount = RadarBackgroundGenerator.Instance.RingCount;
                    float maxRadius = RadarBackgroundGenerator.Instance.MaxRadius;
                    _ringRadii = new float[ringCount];
                    for (int i = 0; i < ringCount; i++)
                    {
                        _ringRadii[i] = maxRadius * ((i + 1) / (float)ringCount);
                    }
                }
            }
            else
            {
                // Fallback default if no radar generator found
                _ringRadii = new float[] { 2f, 4f, 6f, 8f, 10f };
                Debug.LogWarning("EggManager: RadarBackgroundGenerator not found, using default ring radii");
            }
        }

        private void OnDestroy()
        {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.OnPulse -= OnBeatPulse;
            }
        }

        private void Update()
        {
            // Update beat window timer
            if (_inBeatWindow)
            {
                _beatWindowTimer -= Time.deltaTime;
                if (_beatWindowTimer <= 0)
                {
                    _inBeatWindow = false;
                    OnBeatWindowEnded();
                }
            }

            // Check for egg click
            if (_currentEgg != null && !_eggClickedThisPulse)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                {
                    // Check if click is within range of egg
                    Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    mouseWorldPos.z = 0;
                    float distance = Vector3.Distance(mouseWorldPos, _currentEgg.transform.position);

                    if (distance <= clickDetectionRadius)
                    {
                        if (_eggReadyForCollection && _inBeatWindow)
                        {
                            // On beat during collection window - good collect
                            TryClickEgg();
                        }
                        else
                        {
                            // Off beat or before collection window - bad collect (destroy without collecting)
                            OnEggBadCollect();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when the beat pulse occurs (from PlayerController)
        /// </summary>
        private void OnBeatPulse()
        {
            // Beat window is managed by StartBeatWindowBeforePulse coroutine
            // which starts the window centered on the pulse

            // If no egg currently exists, try to spawn one
            // Use a coroutine to wait a frame so EnemySpawner.UpdateEnemies() runs first
            if (_currentEgg == null)
            {
                StartCoroutine(TrySpawnEggAfterEnemySpawner());
            }
        }

        /// <summary>
        /// Wait a frame for EnemySpawner to update, then try to spawn an egg
        /// </summary>
        private IEnumerator TrySpawnEggAfterEnemySpawner()
        {
            yield return null; // Wait one frame for EnemySpawner.UpdateEnemies() to set SpawnedThisPulse

            if (_currentEgg == null)
            {
                SpawnEgg();
            }
        }

        /// <summary>
        /// Start the beat window before the next pulse arrives.
        /// Beat window is centered on the pulse, so we start it (interval - beatWindowTime) after spawn.
        /// </summary>
        private IEnumerator StartBeatWindowBeforePulse()
        {
            float interval = GameManager.Instance != null ? GameManager.Instance.interval : 0.5f;
            float waitTime = interval - beatWindowTime;

            if (waitTime > 0)
            {
                yield return new WaitForSeconds(waitTime);
            }

            // Only start beat window if egg still exists and hasn't been collected yet
            if (_currentEgg != null && !_eggReadyForCollection)
            {
                _eggReadyForCollection = true;
                _inBeatWindow = true;
                _beatWindowTimer = beatWindowTime * 2f; // Full window: half before pulse + half after pulse
                _eggClickedThisPulse = false;

                // Trigger the egg's scale-up animation
                _currentEgg.OnCollectionWindowStart();
            }
        }

        /// <summary>
        /// Called when the beat window ends
        /// </summary>
        private void OnBeatWindowEnded()
        {
            // If there's an egg that was ready for collection but wasn't clicked, it's a miss
            if (_currentEgg != null && _eggReadyForCollection && !_eggClickedThisPulse)
            {
                OnEggMissed();
            }
        }

        /// <summary>
        /// Try to click the current egg (distance already checked in Update)
        /// </summary>
        private void TryClickEgg()
        {
            if (_currentEgg == null) return;

            _eggClickedThisPulse = true;
            OnEggCollected();
        }

        /// <summary>
        /// Called when the egg is clicked off the beat (bad collect)
        /// </summary>
        private void OnEggBadCollect()
        {
            if (_currentEgg == null) return;

            _eggClickedThisPulse = true;

            // Play bad collect sound
            SoundController.Instance?.PlayEggBadCollectSound();

            // Reset multiplier
            _consecutiveEggs = 0;
            UpdateMultiplierLevel();

            // Destroy egg instantly (no animation)
            _currentEgg.Miss();
            _currentEgg = null;

            // Update UI
            UpdateUI();
            // Next egg will spawn on next pulse via OnBeatPulse()
        }

        /// <summary>
        /// Called when an egg is successfully collected
        /// </summary>
        private void OnEggCollected()
        {
            if (_currentEgg == null) return;

            // Spawn explosion particles
            SpawnEggExplosion(_currentEgg.transform.position);

            // Calculate score addition based on consecutive eggs
            int additionIndex = Mathf.Min(_consecutiveEggs, MultiplierAdditions.Length - 1);
            int scoreAddition = MultiplierAdditions[additionIndex];
            _eggScore += scoreAddition;

            // Increment consecutive counter and update multiplier
            _consecutiveEggs++;
            UpdateMultiplierLevel();

            // Add spin charge
            AddSpinCharge();

            // Play collection sound
            SoundController.Instance?.PlayEggCollectSound();

            // Destroy the egg
            _currentEgg.Collect();
            _currentEgg = null;

            // Update UI
            UpdateUI();
            PulseScoreText();
            PulseMultiplierText();
            // Next egg will spawn on next pulse via OnBeatPulse()
        }

        /// <summary>
        /// Called when an egg is missed (beat window passed without collection)
        /// </summary>
        private void OnEggMissed()
        {
            if (_currentEgg == null) return;

            // Destroy egg without particles
            _currentEgg.Miss();
            _currentEgg = null;

            // Reset multiplier but keep score
            _consecutiveEggs = 0;
            UpdateMultiplierLevel();

            // Update UI
            UpdateUI();
            // Next egg will spawn on next pulse via OnBeatPulse()
        }

        /// <summary>
        /// Called when player fails on an entity (combo reset) - halves egg score
        /// </summary>
        public void OnEntityComboFail()
        {
            if (GameManager.Difficulty == Difficulty.Hard)
            {
                // Hard mode: reset score completely
                _eggScore = 0;
            }
            else
            {
                // Easy mode: halve the score
                _eggScore = _eggScore / 2;
            }

            // Reset multiplier
            _consecutiveEggs = 0;
            UpdateMultiplierLevel();

            UpdateUI();
        }

        /// <summary>
        /// Update the multiplier level based on consecutive eggs
        /// </summary>
        private void UpdateMultiplierLevel()
        {
            // Multiplier display: x1 (0-0), x2 (1), x4 (2+), capped at x4 for display
            // But internal value continues: 1, 2, 4, 8, 16
            _currentMultiplierLevel = Mathf.Min(_consecutiveEggs, MultiplierAdditions.Length - 1);
        }

        /// <summary>
        /// Add spin charge based on current multiplier
        /// Base: 10% + 10% per multiplier level, max 40%
        /// </summary>
        private void AddSpinCharge()
        {
            if (SpinChargeManager.Instance == null) return;

            // Fixed 25% charge per egg collected
            SpinChargeManager.Instance.OnParticleConverged(spinChargePerEgg);
        }

        /// <summary>
        /// Spawn egg explosion particles at position with color based on current multiplier
        /// </summary>
        private void SpawnEggExplosion(Vector3 position)
        {
            if (eggExplosionPrefab == null) return;

            var particles = Instantiate(eggExplosionPrefab, position, Quaternion.identity);

            // Configure particles
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;

            // Set particle color based on multiplier level (0=x1, 1=x2, 2=x4, 3+=x4+)
            int colorIndex = Mathf.Min(_currentMultiplierLevel, multiplierParticleColors.Length - 1);
            if (multiplierParticleColors != null && multiplierParticleColors.Length > 0)
            {
                main.startColor = multiplierParticleColors[colorIndex];
            }

            main.playOnAwake = false;
            main.loop = false;

            // Emit particles
            particles.Emit(eggParticleBurstCount);

            // Auto-destroy after particles finish
            Destroy(particles.gameObject, main.duration + main.startLifetime.constantMax);
        }

        /// <summary>
        /// Spawn a new egg on the 3rd ring at a random angle.
        /// Only spawns if no entities were spawned this pulse.
        /// </summary>
        public void SpawnEgg()
        {
            // Only spawn egg if no entities were spawned this pulse
            if (EnemySpawner.Instance != null && EnemySpawner.Instance.SpawnedThisPulse)
            {
                Debug.Log("EggManager: Skipping egg spawn - entities spawned this pulse");
                return;
            }

            SpawnEggInternal();
        }

        /// <summary>
        /// Spawn initial egg without checking SpawnedThisPulse (used at game start)
        /// </summary>
        private void SpawnEggInitial()
        {
            SpawnEggInternal();
        }

        /// <summary>
        /// Internal method to actually spawn an egg
        /// </summary>
        private void SpawnEggInternal()
        {
            if (eggPrefab == null)
            {
                Debug.LogWarning("EggManager: Cannot spawn egg - eggPrefab is not assigned!");
                return;
            }

            if (_ringRadii == null || _ringRadii.Length < 3)
            {
                Debug.LogWarning("EggManager: Cannot spawn egg - not enough ring radii available (need at least 3)!");
                return;
            }

            // Destroy existing egg if any
            if (_currentEgg != null)
            {
                Destroy(_currentEgg.gameObject);
            }

            // Use the 2nd ring (index 1) and random angle
            float ringRadius = _ringRadii[1];
            float angle = Random.Range(0f, Mathf.PI * 2f);

            // Instantiate and initialize
            _currentEgg = Instantiate(eggPrefab, Vector3.zero, Quaternion.identity);
            _currentEgg.Init(angle, ringRadius);
            _eggReadyForCollection = false; // Egg needs to survive one pulse before it can be collected
            _currentEgg.OnEggDestroyed += HandleEggDestroyed;

            _eggClickedThisPulse = false;

            // Start coroutine to enable beat window before next pulse
            // Beat window is centered on the pulse: half before, half after
            StartCoroutine(StartBeatWindowBeforePulse());

            Debug.Log($"EggManager: Spawned egg at angle {angle * Mathf.Rad2Deg:F1}° on ring radius {ringRadius:F1}");
        }

        /// <summary>
        /// Handle egg destroyed callback
        /// </summary>
        private void HandleEggDestroyed(EggCollectable egg, bool wasCollected)
        {
            egg.OnEggDestroyed -= HandleEggDestroyed;
            if (_currentEgg == egg)
            {
                _currentEgg = null;
            }
        }

        #region UI

        private void UpdateUI()
        {
            if (eggScoreText != null)
            {
                eggScoreText.text = _eggScore.ToString();
            }

            if (multiplierText != null)
            {
                // Display multiplier as x1, x2, x4 (capped at x4 for display)
                int displayMultiplier = GetDisplayMultiplier();
                multiplierText.text = $"x{displayMultiplier}";
            }

            // Show/hide multiplier container based on whether we have a multiplier > x1
            if (multiplierContainer != null)
            {
                multiplierContainer.SetActive(_consecutiveEggs > 0);
            }
        }

        /// <summary>
        /// Get the multiplier value for display (x1, x2, x4)
        /// </summary>
        private int GetDisplayMultiplier()
        {
            if (_consecutiveEggs == 0) return 1;
            if (_consecutiveEggs == 1) return 2;
            return 4; // x4 is the max displayed, even though internal value can be higher
        }

        private void PulseScoreText()
        {
            if (eggScoreText == null) return;

            if (_scorePulseCoroutine != null)
            {
                StopCoroutine(_scorePulseCoroutine);
                eggScoreText.transform.localScale = _scoreTextOriginalScale;
            }

            _scorePulseCoroutine = StartCoroutine(PulseTextCoroutine(eggScoreText.transform, _scoreTextOriginalScale));
        }

        private void PulseMultiplierText()
        {
            if (multiplierText == null) return;

            if (_multiplierPulseCoroutine != null)
            {
                StopCoroutine(_multiplierPulseCoroutine);
                multiplierText.transform.localScale = _multiplierTextOriginalScale;
            }

            _multiplierPulseCoroutine = StartCoroutine(PulseTextCoroutine(multiplierText.transform, _multiplierTextOriginalScale));
        }

        private IEnumerator PulseTextCoroutine(Transform target, Vector3 originalScale)
        {
            float elapsed = 0f;
            float halfDuration = textPulseDuration / 2f;
            Vector3 punchedScale = originalScale * textPulseScale;

            // Scale up
            while (elapsed < halfDuration)
            {
                float t = elapsed / halfDuration;
                target.localScale = Vector3.Lerp(originalScale, punchedScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Scale back down
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                float t = elapsed / halfDuration;
                target.localScale = Vector3.Lerp(punchedScale, originalScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            target.localScale = originalScale;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current egg score
        /// </summary>
        public int EggScore => _eggScore;

        /// <summary>
        /// Get current consecutive eggs count
        /// </summary>
        public int ConsecutiveEggs => _consecutiveEggs;

        /// <summary>
        /// Get current multiplier addition value (1, 2, 4, 8, or 16)
        /// </summary>
        public int CurrentMultiplierValue => MultiplierAdditions[_currentMultiplierLevel];

        /// <summary>
        /// Reset all egg game state
        /// </summary>
        public void ResetEggGame()
        {
            _eggScore = 0;
            _consecutiveEggs = 0;
            _currentMultiplierLevel = 0;

            if (_currentEgg != null)
            {
                Destroy(_currentEgg.gameObject);
                _currentEgg = null;
            }

            UpdateUI();
        }

        /// <summary>
        /// Start the egg game (spawn first egg)
        /// </summary>
        public void StartEggGame()
        {
            ResetEggGame();
            SpawnEgg();
        }

        /// <summary>
        /// Stop the egg game (remove current egg)
        /// </summary>
        public void StopEggGame()
        {
            if (_currentEgg != null)
            {
                Destroy(_currentEgg.gameObject);
                _currentEgg = null;
            }
        }

        #endregion
    }
}
