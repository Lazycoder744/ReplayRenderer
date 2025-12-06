using System;
using System.IO;
using UnityEngine;

namespace ReplayRenderer
{
    public class FrameCapture : MonoBehaviour
    {
        private RenderTexture renderTexture;
        private Texture2D screenshot;
        private Camera targetCamera;
        private string outputFolder;
        private int frameCount;

        void Start()
        {
            Plugin.Log.Info("FrameCapture initialized");

            // Set up output folder
            outputFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Beat Saber",
                Plugin.Config.OutputFolder,
                DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")
            );

            Directory.CreateDirectory(outputFolder);
            Plugin.Log.Info($"Saving frames to: {outputFolder}");

            // Get or create the main camera
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Plugin.Log.Error("No main camera found!");
                return;
            }

            // Create render texture at desired resolution
            int width = Plugin.Config.RenderWidth;
            int height = Plugin.Config.RenderHeight;

            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = Plugin.Config.AntiAliasing
            };

            // Create texture for reading pixels
            screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);

            frameCount = 0;

            Plugin.Log.Info($"FrameCapture ready - Resolution: {width}x{height}, AA: {Plugin.Config.AntiAliasing}x");
        }

        void LateUpdate()
        {
            if (!RenderManager.Instance.IsRendering || RenderManager.Instance.IsPaused)
                return;

            if (targetCamera == null)
            {
                Plugin.Log.Error("Camera lost during render!");
                return;
            }

            CaptureFrame();
        }

        private void CaptureFrame()
        {
            try
            {
                // Store original camera target
                RenderTexture originalTarget = targetCamera.targetTexture;

                // Set camera to render to our custom render texture
                targetCamera.targetTexture = renderTexture;

                // Manually render the camera
                targetCamera.Render();

                // Read render texture
                RenderTexture.active = renderTexture;
                screenshot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                screenshot.Apply();

                // Save PNG
                byte[] bytes = screenshot.EncodeToPNG();
                string filename = Path.Combine(outputFolder, $"frame_{frameCount:D6}.png");
                File.WriteAllBytes(filename, bytes);

                // Restore original camera output
                targetCamera.targetTexture = originalTarget;
                RenderTexture.active = null;

                frameCount++;

                RenderManager.Instance.OnFrameCaptured();

                if (frameCount % 100 == 0)
                    Plugin.Log.Info($"Captured {frameCount} frames");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error capturing frame {frameCount}: {ex.Message}");
                Plugin.Log.Error(ex.StackTrace);
            }
        }

        void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }

            if (screenshot != null)
                Destroy(screenshot);

            Plugin.Log.Info($"FrameCapture destroyed. Total frames captured: {frameCount}");

            CreateRenderInfoFile();
        }

        private void CreateRenderInfoFile()
        {
            try
            {
                string infoFile = Path.Combine(outputFolder, "render_info.txt");

                string info = $@"Render Information
==================
Date: {DateTime.Now}
Total Frames: {frameCount}
Resolution: {Plugin.Config.RenderWidth}x{Plugin.Config.RenderHeight}
Frame Rate: {Plugin.Config.TargetFrameRate} fps
Anti-Aliasing: {Plugin.Config.AntiAliasing}x
Duration: {(float)frameCount / Plugin.Config.TargetFrameRate:F2} seconds

FFmpeg Command:
ffmpeg -framerate {Plugin.Config.TargetFrameRate} -i frame_%06d.png -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p output.mp4

HQ:
ffmpeg -framerate {Plugin.Config.TargetFrameRate} -i frame_%06d.png -c:v libx264 -preset slow -crf 15 -pix_fmt yuv420p output_hq.mp4
";

                File.WriteAllText(infoFile, info);

                Plugin.Log.Info("Render info file created");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to create render info file: {ex.Message}");
            }
        }
    }
}