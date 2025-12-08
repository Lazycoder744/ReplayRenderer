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
        
        // Video encoder (Hollywood-style)
        private VideoEncoder videoEncoder;
        private bool useDirectEncoding = true; // Use direct FFmpeg streaming
        
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
            Plugin.Log.Info($"Saving to: {outputFolder}");
            
            // Get camera
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Plugin.Log.Error("No main camera found!");
                
                Camera[] allCameras = FindObjectsOfType<Camera>();
                if (allCameras.Length > 0)
                {
                    targetCamera = allCameras[0];
                    Plugin.Log.Info($"Using fallback camera: {targetCamera.name}");
                }
                else
                {
                    Plugin.Log.Error("No cameras found at all!");
                    enabled = false;
                    return;
                }
            }
            
            Plugin.Log.Info($"Using camera: {targetCamera.name}");
            
            // Create render texture
            int width = Plugin.Config.RenderWidth;
            int height = Plugin.Config.RenderHeight;
            
            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = Plugin.Config.AntiAliasing;
            
            screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            
            frameCount = 0;
            
            // Initialize video encoder if auto-convert is enabled
            if (Plugin.Config.AutoConvertToVideo && useDirectEncoding)
            {
                InitializeVideoEncoder();
            }
            
            Plugin.Log.Info($"FrameCapture ready - {width}x{height}, AA: {Plugin.Config.AntiAliasing}x");
        }
        
        private void InitializeVideoEncoder()
        {
            // Find audio file
            string audioPath = FFmpegConverter.GetCurrentLevelAudioPath();
            
            // Create output video path
            string videoPath = Path.Combine(outputFolder, "output.mp4");
            
            // Initialize encoder
            videoEncoder = gameObject.AddComponent<VideoEncoder>();
            bool success = videoEncoder.Initialize(
                Plugin.Config.RenderWidth,
                Plugin.Config.RenderHeight,
                Plugin.Config.TargetFrameRate,
                videoPath,
                audioPath
            );
            
            if (success)
            {
                Plugin.Log.Info("Direct video encoding enabled (Hollywood-style)");
                useDirectEncoding = true;
            }
            else
            {
                Plugin.Log.Warn("Direct encoding failed, falling back to PNG method");
                useDirectEncoding = false;
                Destroy(videoEncoder);
                videoEncoder = null;
            }
        }
        
        void LateUpdate()
        {
            if (!RenderManager.Instance.IsRendering || RenderManager.Instance.IsPaused)
            {
                return;
            }
            
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
                
                // Render to custom render texture
                targetCamera.targetTexture = renderTexture;
                targetCamera.Render();
                
                // Read pixels
                RenderTexture.active = renderTexture;
                screenshot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                screenshot.Apply();
                
                // Either stream to video encoder or save as PNG
                if (useDirectEncoding && videoEncoder != null && videoEncoder.IsReady)
                {
                    // Stream directly to FFmpeg (Hollywood-style)
                    videoEncoder.QueueFrame(screenshot);
                }
                else
                {
                    // Fallback: Save as PNG
                    SaveFrameAsPNG();
                }
                
                // Restore camera
                targetCamera.targetTexture = originalTarget;
                RenderTexture.active = null;
                
                frameCount++;
                RenderManager.Instance.OnFrameCaptured();
                
                if (frameCount % 100 == 0)
                {
                    if (useDirectEncoding && videoEncoder != null)
                    {
                        Plugin.Log.Info($"Captured {frameCount} frames, encoded {videoEncoder.FramesEncoded}");
                    }
                    else
                    {
                        Plugin.Log.Info($"Captured {frameCount} frames");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error capturing frame {frameCount}: {ex.Message}");
            }
        }
        
        private void SaveFrameAsPNG()
        {
            byte[] bytes = screenshot.EncodeToPNG();
            string filename = Path.Combine(outputFolder, $"frame_{frameCount:D6}.png");
            File.WriteAllBytes(filename, bytes);
        }
        
        void OnDestroy()
        {
            Plugin.Log.Info("FrameCapture destroying...");
            
            // Finish video encoding if active
            if (videoEncoder != null)
            {
                Plugin.Log.Info("Finishing video encoding...");
                videoEncoder.Finish();
                Destroy(videoEncoder);
            }
            
            // Clean up render texture
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
            
            if (screenshot != null)
            {
                Destroy(screenshot);
            }
            
            Plugin.Log.Info($"FrameCapture destroyed. Total frames: {frameCount}");
            
            // Create info file
            if (!useDirectEncoding || videoEncoder == null)
            {
                CreateRenderInfoFile();
            }
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

FFmpeg Command (if PNGs were saved):
ffmpeg -framerate {Plugin.Config.TargetFrameRate} -i frame_%06d.png -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p output.mp4
";
                
                File.WriteAllText(infoFile, info);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to create render info file: {ex.Message}");
            }
        }
    }
}
