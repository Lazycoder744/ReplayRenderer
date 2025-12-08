using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ReplayRenderer
{
    public class RenderManager : MonoBehaviour
    {
        public static RenderManager Instance { get; private set; }

        private FrameCapture frameCapture;
        private CameraController cameraController;
        private ProgressDisplay progressDisplay;

        public bool IsRendering { get; private set; }
        public bool IsPaused { get; private set; }
        private float renderStartTime;
        private int totalFramesCaptured;
        private string currentOutputPath;
        private float originalMasterVolume;
        private bool wasMuted;

        public ReplayData CurrentReplay { get; set; }

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Plugin.Log.Info("RenderManager awake");

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Plugin.Log.Info($"Scene loaded: {scene.name}");

            if (scene.name == "GameCore" && IsRendering)
            {
                InitializeRenderComponents();
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                if (IsRendering)
                {
                    StopRendering();
                }
                else
                {
                    StartRendering();
                }
            }

            if (Input.GetKeyDown(KeyCode.F10) && !IsRendering)
            {
                RestartLevel();
            }

            if (Input.GetKeyDown(KeyCode.F11) && IsRendering)
            {
                if (IsPaused)
                    ResumeRendering();
                else
                    PauseRendering();
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                ToggleGameUI();
            }
        }

        public void StartRendering()
        {
            if (IsRendering)
            {
                Plugin.Log.Warn("Already rendering!");
                return;
            }

            Plugin.Log.Info("Starting render...");

            if (Plugin.Config.AutoRestartOnRenderStart)
            {
                RestartLevel();
                StartCoroutine(StartRenderingAfterDelay(1.0f));
                return;
            }

            IsRendering = true;
            IsPaused = false;
            totalFramesCaptured = 0;
            renderStartTime = Time.realtimeSinceStartup;

            MuteAudio();

            if (Plugin.Config.HideGameUI)
            {
                HideGameUI();
            }

            string outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Beat Saber",
                Plugin.Config.OutputFolder,
                DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")
            );

            Directory.CreateDirectory(outputPath);
            currentOutputPath = outputPath;
            Plugin.Log.Info($"Output directory: {outputPath}");

            InitializeRenderComponents();

            Plugin.Log.Info("Render started successfully");
        }

        public void StartRenderingFromBeatLeader()
        {
            Plugin.Log.Info("Starting render from BeatLeader...");
            StartRendering();
        }

        private IEnumerator StartRenderingAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartRendering();
        }

        private void InitializeRenderComponents()
        {
            frameCapture = gameObject.AddComponent<FrameCapture>();
            cameraController = gameObject.AddComponent<CameraController>();

            if (Plugin.Config.ShowProgressUI)
            {
                progressDisplay = gameObject.AddComponent<ProgressDisplay>();
            }

            Time.captureFramerate = Plugin.Config.TargetFrameRate;

            Plugin.Log.Info($"Capture framerate set to: {Time.captureFramerate}");
        }

        public void PauseRendering()
        {
            if (!IsRendering || IsPaused) return;

            if (!Plugin.Config.AllowPause)
            {
                Plugin.Log.Warn("Pausing is disabled in config");
                return;
            }

            IsPaused = true;
            Time.timeScale = 0f;
            Plugin.Log.Info("Render paused");
        }

        public void ResumeRendering()
        {
            if (!IsRendering || !IsPaused) return;

            IsPaused = false;
            Time.timeScale = 1f;
            Plugin.Log.Info("Render resumed");
        }

        public void StopRendering()
        {
            if (!IsRendering)
            {
                Plugin.Log.Warn("Not currently rendering!");
                return;
            }

            Plugin.Log.Info("Stopping render...");

            IsRendering = false;
            IsPaused = false;

            Time.captureFramerate = 0;
            Time.timeScale = 1f;

            UnmuteAudio();

            if (frameCapture != null)
            {
                Destroy(frameCapture);
                frameCapture = null;
            }

            if (cameraController != null)
            {
                Destroy(cameraController);
                cameraController = null;
            }

            if (progressDisplay != null)
            {
                Destroy(progressDisplay);
                progressDisplay = null;
            }

            float totalTime = Time.realtimeSinceStartup - renderStartTime;
            Plugin.Log.Info($"Render stopped. Total frames: {totalFramesCaptured}, Time: {totalTime:F2}s");

            Plugin.Log.Info("Video encoding will complete automatically");
        }

        public void OnFrameCaptured()
        {
            totalFramesCaptured++;
        }

        private void RestartLevel()
        {
            Plugin.Log.Info("Attempting to restart level/replay...");

            if (SceneManager.GetActiveScene().name != "GameCore")
            {
                Plugin.Log.Warn("Not in gameplay scene, cannot restart");
                return;
            }

            try
            {
                var pauseController = Resources.FindObjectsOfTypeAll<PauseController>().FirstOrDefault();
                if (pauseController != null)
                {
                    pauseController.Pause();
                    StartCoroutine(RestartAfterPause(pauseController));
                    Plugin.Log.Info("Level restart initiated via PauseController");
                    return;
                }

                Plugin.Log.Warn("Could not find PauseController for restart");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error restarting level: {ex.Message}");
            }
        }

        private IEnumerator RestartAfterPause(PauseController pauseController)
        {
            yield return new WaitForSeconds(0.1f);

            var pauseMenuManager = Resources.FindObjectsOfTypeAll<PauseMenuManager>().FirstOrDefault();
            if (pauseMenuManager != null)
            {
                try
                {
                    var method = pauseMenuManager.GetType().GetMethod("RestartButtonPressed");
                    if (method != null)
                    {
                        method.Invoke(pauseMenuManager, null);
                        Plugin.Log.Info("Restart triggered via reflection");
                    }
                    else
                    {
                        Plugin.Log.Warn("RestartButtonPressed method not found");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"Failed to restart: {ex.Message}");
                }
            }
        }

        private void ToggleGameUI()
        {
            try
            {
                var hudController = Resources.FindObjectsOfTypeAll<CoreGameHUDController>().FirstOrDefault();
                if (hudController != null)
                {
                    Canvas canvas = hudController.GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        canvas.enabled = !canvas.enabled;
                        Plugin.Log.Info($"Game HUD {(canvas.enabled ? "shown" : "hidden")}");
                    }
                    else
                    {
                        hudController.gameObject.SetActive(!hudController.gameObject.activeSelf);
                        Plugin.Log.Info($"Game HUD {(hudController.gameObject.activeSelf ? "shown" : "hidden")}");
                    }
                }

                var comboUIController = Resources.FindObjectsOfTypeAll<ComboUIController>().FirstOrDefault();
                if (comboUIController != null)
                {
                    comboUIController.gameObject.SetActive(!comboUIController.gameObject.activeSelf);
                }

                var multiplierUIController = Resources.FindObjectsOfTypeAll<ScoreMultiplierUIController>().FirstOrDefault();
                if (multiplierUIController != null)
                {
                    multiplierUIController.gameObject.SetActive(!multiplierUIController.gameObject.activeSelf);
                }

                var energyController = Resources.FindObjectsOfTypeAll<GameEnergyUIPanel>().FirstOrDefault();
                if (energyController != null)
                {
                    energyController.gameObject.SetActive(!energyController.gameObject.activeSelf);
                }

                Plugin.Log.Info("UI elements toggled");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error toggling UI: {ex.Message}");
            }
        }

        private void HideGameUI()
        {
            try
            {
                var hudController = Resources.FindObjectsOfTypeAll<CoreGameHUDController>().FirstOrDefault();
                if (hudController != null)
                {
                    Canvas canvas = hudController.GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        canvas.enabled = false;
                    }
                    else
                    {
                        hudController.gameObject.SetActive(false);
                    }
                }

                var comboUIController = Resources.FindObjectsOfTypeAll<ComboUIController>().FirstOrDefault();
                if (comboUIController != null)
                {
                    comboUIController.gameObject.SetActive(false);
                }

                var multiplierUIController = Resources.FindObjectsOfTypeAll<ScoreMultiplierUIController>().FirstOrDefault();
                if (multiplierUIController != null)
                {
                    multiplierUIController.gameObject.SetActive(false);
                }

                var energyController = Resources.FindObjectsOfTypeAll<GameEnergyUIPanel>().FirstOrDefault();
                if (energyController != null)
                {
                    energyController.gameObject.SetActive(false);
                }

                Plugin.Log.Info("Game UI hidden");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error hiding UI: {ex.Message}");
            }
        }

        private void MuteAudio()
        {
            try
            {
                originalMasterVolume = AudioListener.volume;
                wasMuted = originalMasterVolume == 0f;

                AudioListener.volume = 0f;

                AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
                foreach (var source in audioSources)
                {
                    source.mute = true;
                }

                Plugin.Log.Info($"Audio muted (was: {originalMasterVolume})");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error muting audio: {ex.Message}");
            }
        }

        private void UnmuteAudio()
        {
            try
            {
                if (!wasMuted)
                {
                    AudioListener.volume = originalMasterVolume;
                }

                AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
                foreach (var source in audioSources)
                {
                    source.mute = false;
                }

                Plugin.Log.Info("Audio unmuted");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error unmuting audio: {ex.Message}");
            }
        }
    }
}
