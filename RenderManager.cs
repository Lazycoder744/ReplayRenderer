using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ReplayRenderer
{
    public class RenderManager : MonoBehaviour
    {
        public static RenderManager Instance { get; private set; }
        
        // Components
        private FrameCapture frameCapture;
        private CameraController cameraController;
        private ProgressDisplay progressDisplay;
        
        // State
        public bool IsRendering { get; private set; }
        public bool IsPaused { get; private set; }
        private float renderStartTime;
        private int totalFramesCaptured;
        private string currentOutputPath;
        private float originalMasterVolume;
        private bool wasMuted;
        
        // Replay data (will be set when replay starts)
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
            
            // Subscribe to scene changes
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Plugin.Log.Info($"Scene loaded: {scene.name}");
            
            // If we're in gameplay scene and should be rendering, initialize
            if (scene.name == "GameCore" && IsRendering)
            {
                InitializeRenderComponents();
            }
        }
        
        void Update()
        {
            // Check for hotkey to toggle rendering (for testing)
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
            
            // F10 - Restart level/replay when not rendering
            if (Input.GetKeyDown(KeyCode.F10) && !IsRendering)
            {
                RestartLevel();
            }
            
            // Pause/Resume with F11 during render (changed from F10)
            if (Input.GetKeyDown(KeyCode.F11) && IsRendering)
            {
                if (IsPaused)
                    ResumeRendering();
                else
                    PauseRendering();
            }
            
            // Hide/Show GUI with H
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
            
            // Auto-restart level if configured
            if (Plugin.Config.AutoRestartOnRenderStart)
            {
                RestartLevel();
                // Wait for restart to complete before starting render
                StartCoroutine(StartRenderingAfterDelay(1.0f));
                return;
            }
            
            IsRendering = true;
            IsPaused = false;
            totalFramesCaptured = 0;
            renderStartTime = Time.realtimeSinceStartup;
            
            // Mute all audio
            MuteAudio();
            
            // Auto-hide UI if configured
            if (Plugin.Config.HideGameUI)
            {
                HideGameUI();
            }
            
            // Set up output directory
            string outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Beat Saber",
                Plugin.Config.OutputFolder,
                DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")
            );
            
            Directory.CreateDirectory(outputPath);
            currentOutputPath = outputPath;
            Plugin.Log.Info($"Output directory: {outputPath}");
            
            // Initialize rendering
            InitializeRenderComponents();
            
            Plugin.Log.Info("Render started successfully");
        }
        
        /// <summary>
        /// Start rendering from BeatLeader UI button click
        /// </summary>
        public void StartRenderingFromBeatLeader()
        {
            Plugin.Log.Info("Starting render from BeatLeader...");
            // In the future, we can get replay info from BeatLeader here
            StartRendering();
        }
        
        private System.Collections.IEnumerator StartRenderingAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartRendering();
        }
        
        private void InitializeRenderComponents()
        {
            // Add FrameCapture component
            frameCapture = gameObject.AddComponent<FrameCapture>();
            
            // Add CameraController component
            cameraController = gameObject.AddComponent<CameraController>();
            
            // Add progress display UI if enabled
            if (Plugin.Config.ShowProgressUI)
            {
                progressDisplay = gameObject.AddComponent<ProgressDisplay>();
            }
            
            // Set Unity's capture framerate - this is the magic that makes it work
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
            
            // Reset Unity's capture framerate
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            
            // Unmute audio
            UnmuteAudio();
            
            // Clean up components
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
            
            // TODO: Optionally trigger FFmpeg conversion here
            if (Plugin.Config.AutoConvertToVideo)
            {
                ConvertToVideo();
            }
        }
        
        public void OnFrameCaptured()
        {
            totalFramesCaptured++;
        }
        
        private void ConvertToVideo()
        {
            if (!Plugin.Config.AutoConvertToVideo)
            {
                Plugin.Log.Info("Auto-convert disabled, skipping video creation");
                return;
            }
            
            Plugin.Log.Info("Starting automatic video conversion...");
            
            // Get audio file path
            string audioPath = FFmpegConverter.GetCurrentLevelAudioPath();
            if (string.IsNullOrEmpty(audioPath))
            {
                Plugin.Log.Warn("Could not find audio file, will create video without audio");
            }
            
            // Create converter and run async
            var converter = new FFmpegConverter(currentOutputPath, audioPath, Plugin.Config.TargetFrameRate);
            
            // Run conversion asynchronously
            Task.Run(async () =>
            {
                bool success = await converter.ConvertToVideo();
                
                if (success)
                {
                    Plugin.Log.Info("Video conversion completed successfully!");
                    
                    // Delete frames if configured
                    if (Plugin.Config.DeleteFramesAfterConversion)
                    {
                        try
                        {
                            var frameFiles = Directory.GetFiles(currentOutputPath, "frame_*.png");
                            foreach (var file in frameFiles)
                            {
                                File.Delete(file);
                            }
                            Plugin.Log.Info($"Deleted {frameFiles.Length} frame files");
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.Error($"Failed to delete frames: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Plugin.Log.Error("Video conversion failed!");
                }
            });
        }
        
        private void RestartLevel()
        {
            Plugin.Log.Info("Attempting to restart level/replay...");
            
            // Check if we're in the gameplay scene
            if (SceneManager.GetActiveScene().name != "GameCore")
            {
                Plugin.Log.Warn("Not in gameplay scene, cannot restart");
                return;
            }
            
            try
            {
                // Method 1: Try to find and use the PauseController
                var pauseController = Resources.FindObjectsOfTypeAll<PauseController>().FirstOrDefault();
                if (pauseController != null)
                {
                    // Pause the game first
                    pauseController.Pause();
                    
                    // Wait a frame, then restart
                    StartCoroutine(RestartAfterPause(pauseController));
                    Plugin.Log.Info("Level restart initiated via PauseController");
                    return;
                }
                
                // Method 2: Try to find StandardLevelGameplayManager or similar
                // Note: This is a placeholder - actual restart mechanism may vary by BS version
                Plugin.Log.Warn("Could not find PauseController for restart");
                return;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error restarting level: {ex.Message}");
            }
        }
        
        private System.Collections.IEnumerator RestartAfterPause(PauseController pauseController)
        {
            yield return new WaitForSeconds(0.1f);
            
            // Try to trigger restart through pause menu
            var pauseMenuManager = Resources.FindObjectsOfTypeAll<PauseMenuManager>().FirstOrDefault();
            if (pauseMenuManager != null)
            {
                // Call the HandleMenuButton method with restart action
                // Note: This may need adjustment based on Beat Saber version
                try
                {
                    // Try reflection to call restart
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
                // Find and toggle CoreGameHUDController (main game UI)
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
                        // Try toggling the GameObject itself
                        hudController.gameObject.SetActive(!hudController.gameObject.activeSelf);
                        Plugin.Log.Info($"Game HUD {(hudController.gameObject.activeSelf ? "shown" : "hidden")}");
                    }
                }
                
                // Also hide/show multiplier and combo UI
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
                
                // Hide energy bar
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
                // Hide all game UI elements
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
                // Store original volume
                originalMasterVolume = AudioListener.volume;
                wasMuted = originalMasterVolume == 0f;
                
                // Mute everything
                AudioListener.volume = 0f;
                
                // Also try to find and mute all audio sources
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
                // Restore original volume
                if (!wasMuted)
                {
                    AudioListener.volume = originalMasterVolume;
                }
                
                // Unmute audio sources
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