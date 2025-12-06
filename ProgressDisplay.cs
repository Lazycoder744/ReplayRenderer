using UnityEngine;
using System;

namespace ReplayRenderer
{
    public class ProgressDisplay : MonoBehaviour
    {
        // UI Styles
        private GUIStyle titleStyle;
        private GUIStyle textStyle;
        private GUIStyle smallTextStyle;
        private GUIStyle boxStyle;
        
        // Display data
        private float songDuration = 180f;
        private float currentTime = 0f;
        private int framesRendered = 0;
        private float renderStartTime;
        private float estimatedTotalFrames;
        
        // Black screen overlay
        private Texture2D blackTexture;
        
        void Start()
        {
            Plugin.Log.Info("ProgressDisplay initialized");
            
            renderStartTime = Time.realtimeSinceStartup;
            
            // Calculate estimated total frames
            if (RenderManager.Instance.CurrentReplay != null)
            {
                songDuration = RenderManager.Instance.CurrentReplay.SongDuration;
            }
            
            estimatedTotalFrames = songDuration * Plugin.Config.TargetFrameRate;
            
            // Create black texture for full screen overlay
            blackTexture = new Texture2D(1, 1);
            blackTexture.SetPixel(0, 0, Color.black);
            blackTexture.Apply();
            
            SetupStyles();
        }
        
        private void SetupStyles()
        {
            // Title style - large, bold
            titleStyle = new GUIStyle();
            titleStyle.fontSize = 32;
            titleStyle.normal.textColor = new Color(0.2f, 1f, 0.3f); // Bright green
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;
            
            // Main text style
            textStyle = new GUIStyle();
            textStyle.fontSize = 20;
            textStyle.normal.textColor = Color.white;
            textStyle.alignment = TextAnchor.MiddleLeft;
            textStyle.fontStyle = FontStyle.Normal;
            textStyle.padding = new RectOffset(20, 20, 10, 10);
            
            // Small text for controls
            smallTextStyle = new GUIStyle();
            smallTextStyle.fontSize = 16;
            smallTextStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            smallTextStyle.alignment = TextAnchor.MiddleLeft;
            smallTextStyle.fontStyle = FontStyle.Italic;
            
            // Semi-transparent dark box
            boxStyle = new GUIStyle();
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.85f));
            boxStyle.border = new RectOffset(2, 2, 2, 2);
        }
        
        void Update()
        {
            // Update current time based on frame count
            framesRendered = Time.frameCount;
            currentTime = (float)framesRendered / Plugin.Config.TargetFrameRate;
        }
        
        void OnGUI()
        {
            if (!RenderManager.Instance.IsRendering)
                return;
            
            // Draw full-screen black overlay
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTexture);
            
            // Calculate center position for UI
            float panelWidth = 600f;
            float panelHeight = 400f;
            float centerX = (Screen.width - panelWidth) / 2f;
            float centerY = (Screen.height - panelHeight) / 2f;
            
            Rect panelRect = new Rect(centerX, centerY, panelWidth, panelHeight);
            
            // Draw semi-transparent background panel
            GUI.Box(panelRect, "", boxStyle);
            
            // Calculate stats
            float progress = Mathf.Clamp01(currentTime / songDuration);
            float elapsedRealTime = Time.realtimeSinceStartup - renderStartTime;
            float fps = framesRendered / Mathf.Max(elapsedRealTime, 0.01f);
            float eta = (estimatedTotalFrames - framesRendered) / Mathf.Max(fps, 0.01f);
            
            // Title
            Rect titleRect = new Rect(centerX, centerY + 20, panelWidth, 40);
            GUI.Label(titleRect, RenderManager.Instance.IsPaused ? "RENDER PAUSED" : "RENDERING IN PROGRESS", titleStyle);
            
            // Build display text
            float yOffset = centerY + 80;
            float lineHeight = 30f;
            
            // Song info
            if (RenderManager.Instance.CurrentReplay != null)
            {
                DrawTextLine($"Song: {RenderManager.Instance.CurrentReplay.SongName}", centerX + 30, yOffset, panelWidth - 60);
                yOffset += lineHeight;
                DrawTextLine($"Player: {RenderManager.Instance.CurrentReplay.PlayerName}", centerX + 30, yOffset, panelWidth - 60);
                yOffset += lineHeight + 10;
            }
            
            // Progress
            DrawTextLine($"Progress: {progress * 100:F1}%", centerX + 30, yOffset, panelWidth - 60);
            yOffset += lineHeight;
            
            // Time
            DrawTextLine($"Time: {FormatTime(currentTime)} / {FormatTime(songDuration)}", centerX + 30, yOffset, panelWidth - 60);
            yOffset += lineHeight;
            
            // Frames
            DrawTextLine($"Frames: {framesRendered:N0} / {estimatedTotalFrames:N0}", centerX + 30, yOffset, panelWidth - 60);
            yOffset += lineHeight + 10;
            
            // Render settings
            DrawTextLine($"Resolution: {Plugin.Config.RenderWidth}x{Plugin.Config.RenderHeight} @ {Plugin.Config.TargetFrameRate} fps", 
                centerX + 30, yOffset, panelWidth - 60);
            yOffset += lineHeight;
            
            // Performance
            DrawTextLine($"Capture Rate: {fps:F1} fps  |  Elapsed: {FormatTime(elapsedRealTime)}", 
                centerX + 30, yOffset, panelWidth - 60);
            yOffset += lineHeight;
            
            DrawTextLine($"ETA: {FormatTime(eta)}", centerX + 30, yOffset, panelWidth - 60);
            yOffset += lineHeight + 15;
            
            // Progress bar
            float barWidth = panelWidth - 60;
            float barHeight = 30f;
            Rect barRect = new Rect(centerX + 30, yOffset, barWidth, barHeight);
            DrawProgressBar(barRect, progress);
            yOffset += barHeight + 20;
            
            // Controls - smaller text at bottom
            DrawSmallText("F9: Stop Render  |  F11: Pause/Resume  |  H: Toggle UI", 
                centerX + 30, yOffset, panelWidth - 60);
        }
        
        private void DrawTextLine(string text, float x, float y, float width)
        {
            GUI.Label(new Rect(x, y, width, 30), text, textStyle);
        }
        
        private void DrawSmallText(string text, float x, float y, float width)
        {
            GUI.Label(new Rect(x, y, width, 25), text, smallTextStyle);
        }
        
        private void DrawProgressBar(Rect rect, float progress)
        {
            // Background (dark gray)
            Texture2D bgTex = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 1f));
            GUI.DrawTexture(rect, bgTex);
            
            // Progress fill (bright green gradient)
            Rect fillRect = new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * progress, rect.height - 4);
            Color progressColor = Color.Lerp(
                new Color(0.2f, 0.8f, 0.3f), // Green
                new Color(0.3f, 1f, 0.4f),   // Brighter green
                Mathf.PingPong(Time.time * 0.5f, 1f) // Subtle pulse
            );
            Texture2D fillTex = MakeTex(2, 2, progressColor);
            GUI.DrawTexture(fillRect, fillTex);
            
            // Border (light gray)
            Texture2D borderTex = MakeTex(2, 2, new Color(0.5f, 0.5f, 0.5f, 1f));
            DrawBorder(rect, borderTex, 2);
            
            // Progress percentage text overlay
            GUIStyle percentStyle = new GUIStyle(textStyle);
            percentStyle.alignment = TextAnchor.MiddleCenter;
            percentStyle.fontSize = 18;
            percentStyle.fontStyle = FontStyle.Bold;
            
            // Add shadow for readability
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), 
                $"{progress * 100:F1}%", 
                percentStyle);
        }
        
        private void DrawBorder(Rect rect, Texture2D texture, int borderWidth)
        {
            // Top
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, borderWidth), texture);
            // Bottom
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - borderWidth, rect.width, borderWidth), texture);
            // Left
            GUI.DrawTexture(new Rect(rect.x, rect.y, borderWidth, rect.height), texture);
            // Right
            GUI.DrawTexture(new Rect(rect.x + rect.width - borderWidth, rect.y, borderWidth, rect.height), texture);
        }
        
        private string FormatTime(float seconds)
        {
            if (float.IsInfinity(seconds) || float.IsNaN(seconds))
                return "--:--";
            
            int minutes = (int)(seconds / 60);
            int secs = (int)(seconds % 60);
            return $"{minutes:D2}:{secs:D2}";
        }
        
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }
            
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            
            return result;
        }
        
        void OnDestroy()
        {
            if (blackTexture != null)
            {
                Destroy(blackTexture);
            }
        }
    }
}