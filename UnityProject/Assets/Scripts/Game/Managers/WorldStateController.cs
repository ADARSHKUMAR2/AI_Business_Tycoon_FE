using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;

namespace AIBusinessTycoon.Managers
{
    /// <summary>Cycles day/night and weather, applying visible world effects.</summary>
    public class WorldStateController : MonoBehaviour
    {
        public enum TimeOfDay { Day, Night }
        public enum Weather { Clear, Rain }

        [Header("Cycle Durations (seconds)")]
        [SerializeField, Min(1f)] private float dayDuration = 300f;
        [SerializeField, Min(1f)] private float nightDuration = 180f;
        [SerializeField, Min(1f)] private float weatherChangeInterval = 240f;

        [Header("Spawn Rate Multipliers")]
        [SerializeField, Min(0.01f)] private float rainSpawnMultiplier = 0.75f;
        [SerializeField, Min(0.01f)] private float nightSpawnMultiplier = 0.7f;
        [SerializeField, Range(0f, 1f)] private float rainChance = 0.35f;

        [Header("Visual Effects")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private TMP_Text worldStatusText;
        [SerializeField, Min(0f)] private float dayLightIntensity = 1.5f;
        [SerializeField, Min(0f)] private float nightLightIntensity = 0.12f;
        [SerializeField, Min(0f)] private float lightingTransitionSpeed = 2f;
        [SerializeField, Min(0f)] private float rainEmissionRate = 180f;

        [Header("Backend Authority")]
        [SerializeField] private bool useBackendWorldState;
        [SerializeField, Min(1f)] private float backendRefreshInterval = 20f;

        private float timeInCurrentPhase;
        private float timeSinceWeatherChange;
        private Canvas statusCanvas;
        private Image statusBackground;
        private bool createdStatusCanvas;
        private float timeUntilBackendRefresh;
        private float backendRainSpawnMultiplier;
        private float backendNightSpawnMultiplier;
        private bool hasBackendState;

        public TimeOfDay CurrentTimeOfDay { get; private set; } = TimeOfDay.Day;
        public Weather CurrentWeather { get; private set; } = Weather.Clear;
        public event Action OnWorldStateChanged;

        public float CustomerSpawnMultiplier
        {
            get
            {
                float multiplier = 1f;
                if (CurrentWeather == Weather.Rain)
                    multiplier *= hasBackendState ? backendRainSpawnMultiplier : rainSpawnMultiplier;
                if (CurrentTimeOfDay == TimeOfDay.Night)
                    multiplier *= hasBackendState ? backendNightSpawnMultiplier : nightSpawnMultiplier;
                return multiplier;
            }
        }

        private void Awake()
        {
            if (directionalLight == null) directionalLight = FindDirectionalLight();
            if (rainParticles == null) rainParticles = CreateRainParticles();
            if (worldStatusText == null) CreateStatusLabel();
            ApplyRainState();
            UpdateStatusLabel();

            if (useBackendWorldState)
                RefreshBackendWorldState();
        }

        private void Update()
        {
            if (useBackendWorldState)
            {
                timeUntilBackendRefresh -= Time.deltaTime;
                if (timeUntilBackendRefresh <= 0f)
                    RefreshBackendWorldState();
            }
            else
            {
                UpdateLocalWorldState();
            }

            UpdateLighting();
            UpdateRainPosition();
        }

        private void UpdateLocalWorldState()
        {
            timeInCurrentPhase += Time.deltaTime;
            timeSinceWeatherChange += Time.deltaTime;
            bool changed = false;

            while (timeInCurrentPhase >= CurrentPhaseDuration())
            {
                timeInCurrentPhase -= CurrentPhaseDuration();
                CurrentTimeOfDay = CurrentTimeOfDay == TimeOfDay.Day ? TimeOfDay.Night : TimeOfDay.Day;
                Debug.Log($"[WorldStateController] Time changed to {CurrentTimeOfDay}.");
                changed = true;
            }

            float interval = Mathf.Max(1f, weatherChangeInterval);
            while (timeSinceWeatherChange >= interval)
            {
                timeSinceWeatherChange -= interval;
                CurrentWeather = UnityEngine.Random.value < rainChance ? Weather.Rain : Weather.Clear;
                Debug.Log($"[WorldStateController] Weather changed to {CurrentWeather}.");
                changed = true;
            }

            if (changed)
            {
                ApplyRainState();
                UpdateStatusLabel();
                OnWorldStateChanged?.Invoke();
            }
        }

        private void RefreshBackendWorldState()
        {
            timeUntilBackendRefresh = Mathf.Max(1f, backendRefreshInterval);
            TycoonAPIService.Instance.GetWorldState(ApplyBackendWorldState, error =>
            {
                hasBackendState = false;
                Debug.LogWarning($"[WorldStateController] Backend world state unavailable; using local state. {error}");
            });
        }

        private void ApplyBackendWorldState(WorldStateResponse response)
        {
            if (response == null)
                return;

            TimeOfDay newTimeOfDay = string.Equals(response.time_of_day, "night", StringComparison.OrdinalIgnoreCase)
                ? TimeOfDay.Night
                : TimeOfDay.Day;
            Weather newWeather = string.Equals(response.weather, "rain", StringComparison.OrdinalIgnoreCase)
                ? Weather.Rain
                : Weather.Clear;
            bool changed = newTimeOfDay != CurrentTimeOfDay || newWeather != CurrentWeather;

            CurrentTimeOfDay = newTimeOfDay;
            CurrentWeather = newWeather;
            backendRainSpawnMultiplier = Mathf.Max(0.01f, response.rain_spawn_multiplier);
            backendNightSpawnMultiplier = Mathf.Max(0.01f, response.night_spawn_multiplier);
            hasBackendState = true;

            ApplyRainState();
            UpdateStatusLabel();
            if (changed)
            {
                Debug.Log($"[WorldStateController] Backend state changed to {CurrentTimeOfDay} / {CurrentWeather}.");
                OnWorldStateChanged?.Invoke();
            }
        }

        private float CurrentPhaseDuration()
        {
            return Mathf.Max(1f, CurrentTimeOfDay == TimeOfDay.Day ? dayDuration : nightDuration);
        }

        private void UpdateLighting()
        {
            if (directionalLight == null) return;
            bool night = CurrentTimeOfDay == TimeOfDay.Night;
            float intensity = night ? nightLightIntensity : dayLightIntensity;
            if (CurrentWeather == Weather.Rain) intensity *= 0.75f;
            Color color = night ? new Color(0.48f, 0.57f, 0.82f) : new Color(1f, 0.94f, 0.82f);
            if (CurrentWeather == Weather.Rain) color = Color.Lerp(color, new Color(0.68f, 0.75f, 0.85f), 0.45f);
            float blend = 1f - Mathf.Exp(-lightingTransitionSpeed * Time.deltaTime);
            directionalLight.intensity = Mathf.Lerp(directionalLight.intensity, intensity, blend);
            directionalLight.color = Color.Lerp(directionalLight.color, color, blend);
            directionalLight.transform.rotation = Quaternion.Slerp(directionalLight.transform.rotation,
                Quaternion.Euler(night ? 165f : 50f, -30f, 0f), blend);
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight,
                night ? new Color(0.12f, 0.15f, 0.24f) : new Color(0.55f, 0.57f, 0.60f), blend);
        }

        private void ApplyRainState()
        {
            if (rainParticles == null) return;
            if (CurrentWeather == Weather.Rain && !rainParticles.isPlaying) rainParticles.Play();
            else if (CurrentWeather == Weather.Clear && rainParticles.isPlaying)
                rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private Light FindDirectionalLight()
        {
            Light[] lights = FindObjectsByType<Light>();
            foreach (Light sceneLight in lights)
            {
                if (sceneLight.type == LightType.Directional)
                    return sceneLight;
            }

            return lights.Length > 0 ? lights[0] : null;
        }

        private ParticleSystem CreateRainParticles()
        {
            GameObject rainObject = new GameObject("World Rain Particles");
            rainObject.transform.SetParent(transform, false);

            ParticleSystem particles = rainObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 1.4f;
            main.startSpeed = 16f;
            main.startSize = 0.055f;
            main.startColor = new Color(0.72f, 0.83f, 1f, 0.65f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2500;
            main.gravityModifier = 0.15f;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = rainEmissionRate;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(18f, 0.2f, 18f);

            ParticleSystemRenderer renderer = rainObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.08f;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader != null) renderer.material = new Material(shader);

            return particles;
        }

        private void UpdateRainPosition()
        {
            if (rainParticles == null || Camera.main == null) return;

            Vector3 cameraPosition = Camera.main.transform.position;
            rainParticles.transform.position = new Vector3(cameraPosition.x, cameraPosition.y + 9f, cameraPosition.z);
        }

        private void CreateStatusLabel()
        {
            statusCanvas = FindAnyObjectByType<Canvas>();
            if (statusCanvas == null)
            {
                GameObject canvasObject = new GameObject("World State HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                statusCanvas = canvasObject.GetComponent<Canvas>();
                statusCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                createdStatusCanvas = true;
            }

            GameObject backgroundObject = new GameObject("World State Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(statusCanvas.transform, false);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.one;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.pivot = Vector2.one;
            backgroundRect.anchoredPosition = new Vector2(-20f, -20f);
            backgroundRect.sizeDelta = new Vector2(250f, 52f);
            statusBackground = backgroundObject.GetComponent<Image>();
            statusBackground.color = new Color(0.04f, 0.07f, 0.12f, 0.82f);

            GameObject labelObject = new GameObject("World State Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(backgroundObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 4f);
            labelRect.offsetMax = new Vector2(-12f, -4f);

            worldStatusText = labelObject.GetComponent<TextMeshProUGUI>();
            worldStatusText.alignment = TextAlignmentOptions.Center;
            worldStatusText.fontSize = 20f;
            worldStatusText.fontStyle = FontStyles.Bold;
            worldStatusText.color = Color.white;
            if (TMP_Settings.defaultFontAsset != null)
                worldStatusText.font = TMP_Settings.defaultFontAsset;
        }

        private void UpdateStatusLabel()
        {
            if (worldStatusText == null) return;

            string weather = CurrentWeather == Weather.Rain ? "RAIN" : "CLEAR";
            worldStatusText.text = $"{CurrentTimeOfDay.ToString().ToUpperInvariant()} | {weather}";

            if (statusBackground != null)
            {
                statusBackground.color = CurrentWeather == Weather.Rain
                    ? new Color(0.06f, 0.12f, 0.22f, 0.88f)
                    : new Color(0.04f, 0.07f, 0.12f, 0.82f);
            }
        }

        private void OnDestroy()
        {
            if (createdStatusCanvas && statusCanvas != null)
                Destroy(statusCanvas.gameObject);
        }
    }
}

