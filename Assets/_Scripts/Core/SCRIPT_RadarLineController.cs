using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using _Scripts.Core;

public class SCRIPT_RadarLineController : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color leftClickColor = Color.red;
    [SerializeField] private Color rightClickColor = Color.green;

    [Header("Reveal Settings")]
    [SerializeField] private int revealSortingOrder = 1001;
    [SerializeField] private float revealDuration = 0.3f;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Particle Settings")]
    [SerializeField] private ParticleSystem particlePrefab;
    [SerializeField] private int burstCount = 10;
    [SerializeField] private Color enemyParticleColor = Color.red;
    [SerializeField] private Color allyParticleColor = Color.green;

    [Header("Radar Sync")]
    [SerializeField] private RadarBackgroundGenerator radarBackground;

    [Header("Trail Settings")]
    [Tooltip("Lower = smoother trail but more vertices")]
    [SerializeField] private float trailMinVertexDistance = 0.005f;
    [Tooltip("Max rotation speed in degrees per second (limits how fast the line can rotate for smooth trails)")]
    [SerializeField] private float maxRotationSpeed = 720f;

    [Header("Spin Boost Detection")]
    [Tooltip("Time threshold for a full rotation to trigger boost (seconds)")]
    [SerializeField] private float spinBoostThreshold = 0.5f;

    [Header("Spin Attack")]
    [Tooltip("Prefab with particle system to spawn at mouse during boost")]
    [SerializeField] private GameObject mouseDestructionTrailPrefab;
    [Tooltip("Maximum destruction radius during boost (at full boost)")]
    [SerializeField] private float maxDestructionRadius = 1.5f;

    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private HashSet<MovableEntitiy> revealedEntities = new HashSet<MovableEntitiy>();
    private Vector3 baseScale;
    private TrailRenderer trailRenderer;
    private float currentAngle;

    // Spin tracking
    private float _accumulatedRotation = 0f;
    private float _rotationTimer = 0f;
    private float _lastAngle;

    // Active destruction trail instance
    private GameObject _activeTrailInstance;

    // Track if current boost is a charged (special) boost
    private bool _isChargedBoost = false;

    public static SCRIPT_RadarLineController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        baseScale = transform.localScale;

        if (radarBackground == null)
        {
            radarBackground = RadarBackgroundGenerator.Instance;
        }

        // Configure trail renderer for smooth trails
        trailRenderer = GetComponentInChildren<TrailRenderer>();
        if (trailRenderer != null)
        {
            trailRenderer.minVertexDistance = trailMinVertexDistance;
        }

        // Initialize current angle
        currentAngle = transform.eulerAngles.z;
        _lastAngle = currentAngle;
    }

    private void Update()
    {
        RotateTowardsMouse();
        UpdateColor();
        SyncScaleWithRadar();
        TrackSpinBoost();
        UpdateBoostEffect();
    }

    private void UpdateBoostEffect()
    {
        if (CameraRotator.Instance == null) return;

        bool isBoosting = CameraRotator.Instance.IsBoosting;
        float boostProgress = CameraRotator.Instance.BoostProgress;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        // Only spawn trail and do destruction on CHARGED boosts
        if (_isChargedBoost)
        {
            // Spawn trail prefab when charged boost starts
            if (isBoosting && _activeTrailInstance == null && mouseDestructionTrailPrefab != null)
            {
                _activeTrailInstance = Instantiate(mouseDestructionTrailPrefab, mouseWorldPos, Quaternion.identity);
            }

            // Update trail position while boosting
            if (_activeTrailInstance != null)
            {
                if (isBoosting)
                {
                    _activeTrailInstance.transform.position = mouseWorldPos;
                }
                else
                {
                    // Boost ended - destroy the trail instance and reset charged state
                    Destroy(_activeTrailInstance);
                    _activeTrailInstance = null;
                    _isChargedBoost = false;
                }
            }

            // Destroy entities within radius while charged boost is active
            if (isBoosting && EnemySpawner.Instance != null)
            {
                float currentRadius = maxDestructionRadius * boostProgress;
                EnemySpawner.Instance.DestroyEntitiesInRadius(mouseWorldPos, currentRadius);
            }
        }

        // Reset charged boost flag when boost ends (safety check)
        if (!isBoosting && _isChargedBoost && _activeTrailInstance == null)
        {
            _isChargedBoost = false;
        }
    }

    private void TrackSpinBoost()
    {
        // Calculate the delta angle, handling wrap-around
        float deltaAngle = Mathf.DeltaAngle(_lastAngle, currentAngle);
        _lastAngle = currentAngle;

        // Accumulate rotation (positive = counter-clockwise, negative = clockwise)
        _accumulatedRotation += deltaAngle;
        _rotationTimer += Time.deltaTime;

        // Check if we've completed a full rotation (360 degrees)
        if (Mathf.Abs(_accumulatedRotation) >= 360f)
        {
            // Check if it was fast enough AND not already boosting
            // (must wait for current boost to decay before another spin can succeed)
            bool alreadyBoosting = CameraRotator.Instance != null && CameraRotator.Instance.IsBoosting;
            if (_rotationTimer <= spinBoostThreshold && !alreadyBoosting)
            {
                // Camera boost effect
                if (CameraRotator.Instance != null)
                {
                    // Determine spin direction: positive = CCW, negative = CW
                    bool spunClockwise = _accumulatedRotation < 0;
                    bool cameraGoingClockwise = CameraRotator.Instance.direction > 0;

                    if (spunClockwise == cameraGoingClockwise)
                    {
                        // Same direction - just boost
                        CameraRotator.Instance.TriggerBoost();
                    }
                    else
                    {
                        // Opposite direction - flip and boost
                        CameraRotator.Instance.FlipDirectionWithBoost();
                    }
                }

                // Check if we have a stored charge for special boost
                if (SpinChargeManager.Instance != null && SpinChargeManager.Instance.HasCharge)
                {
                    SpinChargeManager.Instance.ConsumeCharge();
                    _isChargedBoost = true;

                    // Reveal all entities on charged spin
                    if (EnemySpawner.Instance != null)
                    {
                        EnemySpawner.Instance.RevealAllEntities();
                    }
                }
            }

            // Reset tracking
            _accumulatedRotation = 0f;
            _rotationTimer = 0f;
        }

        // Reset if taking too long (prevent stale accumulation)
        if (_rotationTimer > spinBoostThreshold * 2f)
        {
            _accumulatedRotation = 0f;
            _rotationTimer = 0f;
        }
    }

    private void SyncScaleWithRadar()
    {
        if (radarBackground == null)
        {
            radarBackground = RadarBackgroundGenerator.Instance;
            if (radarBackground == null) return;
        }

        float scaleMultiplier = radarBackground.CurrentScaleMultiplier;
        transform.localScale = new Vector3(
            baseScale.x * scaleMultiplier,
            baseScale.y,
            baseScale.z
        );
    }

    

    private void RotateTowardsMouse()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector3 direction = mouseWorldPos - transform.position;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Smoothly rotate towards target with max speed limit for smooth trails
        currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, maxRotationSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    private void UpdateColor()
    {
        if (Input.GetMouseButton(0))
        {
            spriteRenderer.color = leftClickColor;
        }
        else if (Input.GetMouseButton(1))
        {
            spriteRenderer.color = rightClickColor;
        }
        else
        {
            spriteRenderer.color = defaultColor;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var entity = other.GetComponent<MovableEntitiy>();
        if (entity == null)
            entity = other.GetComponentInParent<MovableEntitiy>();

        if (entity != null && !revealedEntities.Contains(entity))
        {
            StartCoroutine(RevealEntity(entity));
        }
    }

    private IEnumerator RevealEntity(MovableEntitiy entity)
    {
        revealedEntities.Add(entity);

        var entityRenderer = entity.GetComponentInChildren<SpriteRenderer>();
        if (entityRenderer == null)
        {
            revealedEntities.Remove(entity);
            yield break;
        }

        int originalOrder = entityRenderer.sortingOrder;
        Color originalColor = entityRenderer.color;

        // Set to high sorting order to reveal
        entityRenderer.sortingOrder = revealSortingOrder;

        // Burst particles at entity position with color based on type
        Color particleColor = entity is Enemy ? enemyParticleColor : allyParticleColor;
        SpawnParticleBurst(entity.transform.position, particleColor);

        // Wait for reveal duration
        yield return new WaitForSeconds(revealDuration);

        // Fade back (unless permanently revealed)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            if (entity == null || entityRenderer == null)
            {
                revealedEntities.Remove(entity);
                yield break;
            }

            // If entity was permanently revealed, skip the fade-back
            if (entity.IsPermanentlyRevealed)
            {
                revealedEntities.Remove(entity);
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            // Lerp sorting order back (snap at the end)
            if (t >= 1f)
            {
                entityRenderer.sortingOrder = originalOrder;
            }

            // Fade alpha back to original
            Color currentColor = entityRenderer.color;
            currentColor.a = Mathf.Lerp(1f, originalColor.a, t);
            entityRenderer.color = currentColor;

            yield return null;
        }

        if (entity != null && entityRenderer != null)
        {
            entityRenderer.sortingOrder = originalOrder;
            entityRenderer.color = originalColor;
        }

        revealedEntities.Remove(entity);
    }

    private void SpawnParticleBurst(Vector3 position, Color color)
    {
        if (particlePrefab == null)
            return;

        var particles = Instantiate(particlePrefab, position, Quaternion.identity);

        var main = particles.main;
        main.startColor = color;

        particles.Emit(burstCount);
        Destroy(particles.gameObject, main.duration + main.startLifetime.constantMax);
    }
}
