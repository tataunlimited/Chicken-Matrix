using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Core
{
    public class LightningEffect : MonoBehaviour
    {
        public static LightningEffect Instance;

        [Header("Lightning Settings")]
        [SerializeField] private Material lightningMaterial;
        [SerializeField] private int segmentCount = 8;
        [SerializeField] private float jaggedness = 0.5f;
        [SerializeField] private float mainBoltWidth = 0.15f;
        [SerializeField] private float glowBoltWidth = 0.4f;

        [Header("Arc Settings")]
        [SerializeField] private int arcSegments = 12;
        [SerializeField] private float arcRadius = 0.8f;
        [SerializeField] private float arcJaggedness = 0.3f;
        [SerializeField] private float arcBoltWidth = 0.1f;

        [Header("Timing")]
        [SerializeField] private float duration = 0.15f;
        [SerializeField] private float fadeSpeed = 8f;

        [Header("Rendering")]
        [Tooltip("Sorting order must be above 1000 to appear over the darkness")]
        [SerializeField] private int sortingOrder = 1100;

        [Header("Colors (HDR for bloom)")]
        [ColorUsage(true, true)]
        [SerializeField] private Color enemyColor = new Color(3f, 0.5f, 0.5f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color allyColor = new Color(0.5f, 3f, 1f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color neutralColor = new Color(3f, 3f, 0.5f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color spinAttackColor = new Color(2f, 0.5f, 3f, 1f); // Purple/magenta for spin
        [SerializeField] private float glowIntensity = 2f;

        /// <summary>
        /// Get the spin attack color for external use (e.g., EnemySpawner)
        /// </summary>
        public Color SpinAttackColor => spinAttackColor;

        private List<GameObject> activeLightning = new List<GameObject>();

        void Awake()
        {
            Instance = this;

            // Create default additive material if none assigned
            if (lightningMaterial == null)
            {
                lightningMaterial = new Material(Shader.Find("Sprites/Default"));
                lightningMaterial.SetFloat("_Mode", 1); // Additive-ish
            }
        }

        public void SpawnLightning(Vector3 startPos, Vector3 endPos, DetectionMode mode)
        {
            Color color = GetColorForMode(mode);
            StartCoroutine(LightningCoroutine(startPos, endPos, color));
        }

        /// <summary>
        /// Spawn lightning with a specific color (for spin attack which destroys all types)
        /// </summary>
        public void SpawnLightning(Vector3 startPos, Vector3 endPos, Color color)
        {
            StartCoroutine(LightningCoroutine(startPos, endPos, color));
        }

        private IEnumerator LightningCoroutine(Vector3 startPos, Vector3 endPos, Color baseColor)
        {
            Color glowColor = baseColor * glowIntensity;
            glowColor.a = 0.5f;

            // Create glow bolt (wider, more transparent)
            GameObject glowBolt = CreateLightningBolt(startPos, endPos, glowColor, glowBoltWidth, "LightningGlow");

            // Create main bolt (thinner, brighter)
            GameObject mainBolt = CreateLightningBolt(startPos, endPos, baseColor, mainBoltWidth, "LightningMain");

            // Create arc around the target
            GameObject arcEffect = CreateArcEffect(endPos, baseColor);

            activeLightning.Add(glowBolt);
            activeLightning.Add(mainBolt);
            activeLightning.Add(arcEffect);

            // Animate the lightning with flickering
            float elapsed = 0f;
            LineRenderer glowLR = glowBolt.GetComponent<LineRenderer>();
            LineRenderer mainLR = mainBolt.GetComponent<LineRenderer>();
            LineRenderer arcLR = arcEffect.GetComponent<LineRenderer>();

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Flicker effect
                float flicker = Random.Range(0.7f, 1f);

                // Regenerate lightning path occasionally for electric feel
                if (Random.value < 0.3f)
                {
                    SetLightningPoints(mainLR, startPos, endPos);
                    SetLightningPoints(glowLR, startPos, endPos);
                    RegenerateArc(arcLR, endPos);
                }

                // Fade out
                float alpha = Mathf.Lerp(1f, 0f, t * t) * flicker;

                Color fadeMain = baseColor;
                fadeMain.a = alpha;
                Color fadeGlow = glowColor;
                fadeGlow.a = alpha * 0.5f;

                mainLR.startColor = fadeMain;
                mainLR.endColor = fadeMain;
                glowLR.startColor = fadeGlow;
                glowLR.endColor = fadeGlow;
                arcLR.startColor = fadeMain;
                arcLR.endColor = fadeMain;

                yield return null;
            }

            // Cleanup
            activeLightning.Remove(glowBolt);
            activeLightning.Remove(mainBolt);
            activeLightning.Remove(arcEffect);

            Destroy(glowBolt);
            Destroy(mainBolt);
            Destroy(arcEffect);
        }

        private GameObject CreateLightningBolt(Vector3 start, Vector3 end, Color color, float width, string name)
        {
            GameObject bolt = new GameObject(name);
            bolt.transform.SetParent(transform);

            LineRenderer lr = bolt.AddComponent<LineRenderer>();

            // Use Universal Render Pipeline/2D/Sprite-Lit-Default or fallback for HDR bloom support
            Material mat = CreateHDRMaterial();
            lr.material = mat;
            lr.startWidth = width;
            lr.endWidth = width * 0.5f;
            lr.startColor = color;
            lr.endColor = color;
            lr.sortingOrder = sortingOrder;
            lr.useWorldSpace = true;

            SetLightningPoints(lr, start, end);

            return bolt;
        }

        private Material CreateHDRMaterial()
        {
            // Use the Particles/Standard Unlit shader which supports HDR and additive blending
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material mat = new Material(shader);

            // Configure for additive blending (like the combo particle material)
            // SrcBlend = 5 (SrcAlpha), DstBlend = 1 (One) for additive
            mat.SetFloat("_SrcBlend", 5f);
            mat.SetFloat("_DstBlend", 1f);
            mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = 3000; // Transparent queue

            mat.EnableKeyword("_ALPHABLEND_ON");

            return mat;
        }

        private void SetLightningPoints(LineRenderer lr, Vector3 start, Vector3 end)
        {
            lr.positionCount = segmentCount + 1;

            Vector3 direction = end - start;
            float distance = direction.magnitude;
            Vector3 perpendicular = Vector3.Cross(direction.normalized, Vector3.forward).normalized;

            for (int i = 0; i <= segmentCount; i++)
            {
                float t = (float)i / segmentCount;
                Vector3 basePos = Vector3.Lerp(start, end, t);

                // Add jagged offset (less at endpoints)
                float offsetMultiplier = Mathf.Sin(t * Mathf.PI); // 0 at ends, 1 in middle
                float offset = Random.Range(-jaggedness, jaggedness) * offsetMultiplier * distance * 0.1f;

                lr.SetPosition(i, basePos + perpendicular * offset);
            }
        }

        private GameObject CreateArcEffect(Vector3 center, Color color)
        {
            GameObject arc = new GameObject("LightningArc");
            arc.transform.SetParent(transform);

            LineRenderer lr = arc.AddComponent<LineRenderer>();
            Material mat = CreateHDRMaterial();
            lr.material = mat;
            lr.startWidth = arcBoltWidth;
            lr.endWidth = arcBoltWidth;
            lr.startColor = color;
            lr.endColor = color;
            lr.sortingOrder = sortingOrder;
            lr.useWorldSpace = true;
            lr.loop = false;

            RegenerateArc(lr, center);

            return arc;
        }

        private void RegenerateArc(LineRenderer lr, Vector3 center)
        {
            lr.positionCount = arcSegments + 1;

            // Random start angle and arc length (not full circle)
            float startAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float arcLength = Random.Range(180f, 300f) * Mathf.Deg2Rad;

            for (int i = 0; i <= arcSegments; i++)
            {
                float t = (float)i / arcSegments;
                float angle = startAngle + t * arcLength;

                // Base position on arc
                float radius = arcRadius + Random.Range(-arcJaggedness, arcJaggedness);
                Vector3 pos = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f
                );

                lr.SetPosition(i, pos);
            }
        }

        private Color GetColorForMode(DetectionMode mode)
        {
            switch (mode)
            {
                case DetectionMode.Aggressive:
                    return enemyColor;
                case DetectionMode.Friendly:
                    return allyColor;
                case DetectionMode.None:
                    return neutralColor;
                default:
                    return Color.white;
            }
        }

        // Clean up any remaining lightning on disable
        private void OnDisable()
        {
            foreach (var bolt in activeLightning)
            {
                if (bolt != null)
                    Destroy(bolt);
            }
            activeLightning.Clear();
        }
    }
}
