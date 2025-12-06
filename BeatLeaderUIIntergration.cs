using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BeatSaberMarkupLanguage;

namespace ReplayRenderer
{
    /// <summary>
    /// Integrates a "Render Replay" button into BeatLeader's score details UI
    /// </summary>
    public class BeatLeaderUIIntegration : MonoBehaviour
    {
        private Button renderButton;
        private string currentReplayPath;
        private static bool hasAddedButton = false;
        
        void Start()
        {
            Plugin.Log.Info("BeatLeaderUIIntegration started");
            TryAddRenderButton();
        }
        
        void Update()
        {
            // Periodically check if we need to add the button
            if (!hasAddedButton && Time.frameCount % 60 == 0)
            {
                TryAddRenderButton();
            }
        }
        
        private void TryAddRenderButton()
        {
            try
            {
                // Find BeatLeader's score details panel
                // Look for the replay button first to locate the right UI
                var allButtons = Resources.FindObjectsOfTypeAll<Button>();
                
                foreach (var button in allButtons)
                {
                    // Look for BeatLeader's "Watch Replay" or similar button
                    var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null && 
                        (buttonText.text.Contains("Watch") || buttonText.text.Contains("Replay")))
                    {
                        Plugin.Log.Info($"Found potential replay button: {buttonText.text}");
                        
                        // Try to add our button next to it
                        if (AddRenderButtonNextTo(button))
                        {
                            hasAddedButton = true;
                            Plugin.Log.Info("Successfully added Render Replay button!");
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error adding render button: {ex.Message}");
            }
        }
        
        private bool AddRenderButtonNextTo(Button watchReplayButton)
        {
            try
            {
                // Get the parent transform
                Transform parent = watchReplayButton.transform.parent;
                if (parent == null) return false;
                
                // Create a new button by duplicating the watch replay button
                GameObject renderButtonObj = Instantiate(watchReplayButton.gameObject, parent);
                renderButtonObj.name = "RenderReplayButton";
                
                // Position it next to the watch replay button
                RectTransform renderRect = renderButtonObj.GetComponent<RectTransform>();
                RectTransform watchRect = watchReplayButton.GetComponent<RectTransform>();
                
                if (renderRect != null && watchRect != null)
                {
                    // Place it to the right of the watch button
                    Vector2 offset = new Vector2(watchRect.rect.width + 10f, 0);
                    renderRect.anchoredPosition = watchRect.anchoredPosition + offset;
                }
                
                // Get the button component
                renderButton = renderButtonObj.GetComponent<Button>();
                
                // Update the button text
                var textComponent = renderButton.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = "Render Replay";
                }
                
                // Remove old click listeners and add our own
                renderButton.onClick.RemoveAllListeners();
                renderButton.onClick.AddListener(OnRenderButtonClicked);
                
                Plugin.Log.Info("Render button created and configured");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error creating render button: {ex.Message}");
                return false;
            }
        }
        
        private void OnRenderButtonClicked()
        {
            Plugin.Log.Info("Render Replay button clicked!");
            
            try
            {
                // Get the current replay info from BeatLeader
                // This is a simplified approach - you'd need to hook into BeatLeader's API
                // For now, we'll trigger the standard F9 behavior
                
                // Start rendering
                if (RenderManager.Instance != null)
                {
                    RenderManager.Instance.StartRenderingFromBeatLeader();
                }
                else
                {
                    Plugin.Log.Error("RenderManager not found!");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error starting render: {ex.Message}");
            }
        }
        
        void OnDestroy()
        {
            if (renderButton != null)
            {
                Destroy(renderButton.gameObject);
            }
            hasAddedButton = false;
        }
    }
}
