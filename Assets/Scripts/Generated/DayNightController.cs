using UnityEngine;

namespace ClaudeAssistant.Generated
{

    /// <summary>
    /// Controls a day/night cycle: lights, fog, ambient light AND skybox exposure.
    /// Attach to any GameObject in the scene.
    /// </summary>
    public class DayNightController : MonoBehaviour
    {
        [Header("Lighting")]
        public Light sunLight;
        public Light moonLight;

        [Header("Day/Night Cycle")]
        [Range(1f, 300f)]
        public float cycleDuration = 60f;

        [Header("Day Colors")]
        public Color dayAmbientColor = new Color(0.5f, 0.6f, 0.7f);
        public Color daySkyColor = new Color(0.5f, 0.8f, 1f);
        public Color dayFogColor = new Color(0.7f, 0.8f, 1f);

        [Header("Night Colors")]
        public Color nightAmbientColor = new Color(0.02f, 0.02f, 0.08f);
        public Color nightSkyColor = new Color(0.01f, 0.01f, 0.05f);
        public Color nightFogColor = new Color(0.02f, 0.02f, 0.06f);

        [Header("Skybox")]
        [Tooltip("Exposición del skybox de día. Default Unity = 1.")]
        [Range(0f, 2f)] public float dayExposure = 1.3f;
        [Tooltip("Exposición del skybox de noche. Bajalo para un cielo bien oscuro.")]
        [Range(0f, 0.5f)] public float nightExposure = 0.03f;
        [Tooltip("Tinte del skybox durante el día.")]
        public Color daySkyboxTint = new Color(1f, 1f, 1f);
        [Tooltip("Tinte del skybox durante la noche (azul muy oscuro).")]
        public Color nightSkyboxTint = new Color(0.03f, 0.04f, 0.15f);

        [Header("Sun Rotation")]
        [Tooltip("Rotá el sol a lo largo del ciclo para un efecto más realista.")]
        public bool rotateSun = true;

        [Header("Current State (read-only)")]
        [SerializeField] private bool isDay = true;
        [SerializeField] private float currentTime = 0f;
        [SerializeField] private float transitionProgress = 0f;

        private Camera _mainCamera;
        private Material _skyboxMaterial;

        // Cached shader property IDs (avoid string lookup every frame)
        private static readonly int PropExposure = Shader.PropertyToID("_Exposure");
        private static readonly int PropTint = Shader.PropertyToID("_SkyTint");

        // ── Unity lifecycle ───────────────────────────────────────

        void Start()
        {
            _mainCamera = Camera.main ?? FindAnyObjectByType<Camera>();

            // Create a writable instance so we never modify the shared asset on disk
            if (RenderSettings.skybox != null)
            {
                _skyboxMaterial = new Material(RenderSettings.skybox);
                RenderSettings.skybox = _skyboxMaterial;
            }
            else
            {
                Debug.LogWarning("[DayNightController] No skybox found in RenderSettings. " +
                                 "Assign one via Window → Rendering → Lighting → Environment.");
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.01f;

            SetDayState();
        }

        void Update()
        {
            currentTime += Time.deltaTime;

            if (currentTime >= cycleDuration)
            {
                currentTime = 0f;
                isDay = !isDay;
            }

            float cycleProgress = currentTime / cycleDuration;

            if (isDay)
            {
                if (cycleProgress <= 0.1f)
                {
                    transitionProgress = Mathf.Lerp(1f, 0f, cycleProgress * 10f);
                    ApplyTransition(transitionProgress);
                }
                else SetDayState();
            }
            else
            {
                if (cycleProgress <= 0.1f)
                {
                    transitionProgress = Mathf.Lerp(0f, 1f, cycleProgress * 10f);
                    ApplyTransition(transitionProgress);
                }
                else SetNightState();
            }

            // Rotate sun across the sky each cycle
            if (rotateSun && sunLight != null)
            {
                float angle = (currentTime / cycleDuration) * 180f;
                if (!isDay) angle += 180f;
                sunLight.transform.rotation = Quaternion.Euler(angle - 90f, -30f, 0f);
            }
        }

        // ── State setters ─────────────────────────────────────────

        void SetDayState()
        {
            if (sunLight) { sunLight.enabled = true; sunLight.intensity = 1.2f; }
            if (moonLight) { moonLight.enabled = false; }

            RenderSettings.ambientLight = dayAmbientColor;
            RenderSettings.fogColor = dayFogColor;
            if (_mainCamera) _mainCamera.backgroundColor = daySkyColor;

            ApplySkybox(dayExposure, daySkyboxTint);
        }

        void SetNightState()
        {
            if (sunLight) { sunLight.enabled = false; }
            if (moonLight) { moonLight.enabled = true; moonLight.intensity = 0.4f; }

            RenderSettings.ambientLight = nightAmbientColor;
            RenderSettings.fogColor = nightFogColor;
            if (_mainCamera) _mainCamera.backgroundColor = nightSkyColor;

            ApplySkybox(nightExposure, nightSkyboxTint);
        }

        void ApplyTransition(float t)
        {
            // t = 0 → día completo | t = 1 → noche completa
            if (sunLight)
            {
                sunLight.enabled = t < 0.5f;
                sunLight.intensity = Mathf.Lerp(1.2f, 0f, t * 2f);
            }
            if (moonLight)
            {
                moonLight.enabled = t > 0.5f;
                moonLight.intensity = Mathf.Lerp(0f, 0.4f, (t - 0.5f) * 2f);
            }

            RenderSettings.ambientLight = Color.Lerp(dayAmbientColor, nightAmbientColor, t);
            RenderSettings.fogColor = Color.Lerp(dayFogColor, nightFogColor, t);
            if (_mainCamera)
                _mainCamera.backgroundColor = Color.Lerp(daySkyColor, nightSkyColor, t);

            ApplySkybox(
                Mathf.Lerp(dayExposure, nightExposure, t),
                Color.Lerp(daySkyboxTint, nightSkyboxTint, t));
        }

        // ── Skybox helper ─────────────────────────────────────────

        /// <summary>
        /// Sets exposure and tint on the cached skybox material.
        /// Compatible with Skybox/6 Sided, Skybox/Procedural and Skybox/Cubemap shaders.
        /// </summary>
        private void ApplySkybox(float exposure, Color tint)
        {
            if (_skyboxMaterial == null) return;

            if (_skyboxMaterial.HasProperty(PropExposure))
                _skyboxMaterial.SetFloat(PropExposure, exposure);

            if (_skyboxMaterial.HasProperty(PropTint))
                _skyboxMaterial.SetColor(PropTint, tint);

            // Recalculate GI from the updated skybox
            DynamicGI.UpdateEnvironment();
        }

        // ── Context menu helpers ──────────────────────────────────

        [ContextMenu("Toggle Day/Night")]
        void ToggleDayNight() { isDay = !isDay; currentTime = 0f; }

        [ContextMenu("Force Day")]
        void ForceDay() { isDay = true; currentTime = cycleDuration; SetDayState(); }

        [ContextMenu("Force Night")]
        void ForceNight() { isDay = false; currentTime = cycleDuration; SetNightState(); }
    }

} // namespace ClaudeAssistant.Generated