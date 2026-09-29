using System;
using UnityEngine;
using TMPro;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Cycles day/night and weather, drives scene lighting and rain, and
    /// updates a scene-placed TMP label. Assign all Inspector references
    /// before entering Play Mode - no UI or particles are created at runtime.
    /// </summary>
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

        [Header("Scene References - Assign in Inspector")]
        [Tooltip("Scene Directional Light.")]
        [SerializeField] private Light directionalLight;
        [Tooltip("Scene Rain Particle System (kept stopped; started by controller).")]
        [SerializeField] private ParticleSystem rainParticles;
        [Tooltip("Scene TMP_Text label for the world-state HUD.")]
        [SerializeField] private TMP_Text worldStatusText;

        [Header("Lighting")]
        [SerializeField, Min(0f)] private float dayLightIntensity = 1.5f;
        [SerializeField, Min(0f)] private float nightLightIntensity = 0.12f;
        [SerializeField, Min(0f)] private float lightingTransitionSpeed = 2f;

        [Header("Backend Authority")]
        [SerializeField] private bool useBackendWorldState;
        [SerializeField, Min(1f)] private float backendRefreshInterval = 20f;

        private float timeInCurrentPhase;
        private float timeSinceWeatherChange;
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
            if (directionalLight == null)
                directionalLight = FindDirectionalLight();
            if (rainParticles == null)
                Debug.LogWarning("[WorldStateController] Rain Particles not assigned in Inspector.");
            if (worldStatusText == null)
                Debug.LogWarning("[WorldStateController] World Status Text not assigned in Inspector.");

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

        private void UpdateStatusLabel()
        {
            if (worldStatusText == null) return;
            string weather = CurrentWeather == Weather.Rain ? "RAIN" : "CLEAR";
            worldStatusText.text = $"{CurrentTimeOfDay.ToString().ToUpperInvariant()} | {weather}";
        }

        private Light FindDirectionalLight()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Light l in lights)
                if (l.type == LightType.Directional) return l;
            return lights.Length > 0 ? lights[0] : null;
        }
    }
}


