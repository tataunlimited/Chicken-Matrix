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

    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private HashSet<MovableEntitiy> revealedEntities = new HashSet<MovableEntitiy>();

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        RotateTowardsMouse();
        UpdateColor();
    }

    private void RotateTowardsMouse()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector3 direction = mouseWorldPos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
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

        // Fade back
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            if (entity == null || entityRenderer == null)
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
