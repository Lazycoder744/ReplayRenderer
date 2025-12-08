using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;

namespace ReplayRenderer
{
    /// <summary>
    /// Streams frames directly to FFmpeg without saving PNG files first
    /// This is the PC equivalent of Quest's "Hollywood" renderer
    /// </summary>
    public class VideoEncoder : MonoBehaviour
    {
        // FFmpeg process
        private Process ffmpegProcess;
        private Stream ffmpegInputStream;
        private Thread encodingThread;
        private bool isEncoding;
        
        // Frame queue
        private ConcurrentQueue<byte[]> frameQueue = new ConcurrentQueue<byte[]>();
        private int totalFramesQueued = 0;
        private int totalFramesEncoded = 0;
        
        // Settings
        private int width;
        private int height;
        private int frameRate;
        private string outputPath;
        private string audioPath;
        
        // Performance
        private const int MAX_QUEUE_SIZE = 30; // Buffer up to 30 frames
        
        public int FramesQueued => totalFramesQueued;
        public int FramesEncoded => totalFramesEncoded;
        public bool IsReady => isEncoding && ffmpegProcess != null && !ffmpegProcess.HasExited;
        
        /// <summary>
        /// Initialize the encoder and start FFmpeg process
        /// </summary>
        public bool Initialize(int width, int height, int fps, string outputFile, string audioFile = null)
        {
            this.width = width;
            this.height = height;
            this.frameRate = fps;
            this.outputPath = outputFile;
            this.audioPath = audioFile;
            
            Plugin.Log.Info($"Initializing video encoder: {width}x{height} @ {fps}fps");
            
            string ffmpegPath = FindFFmpeg();
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                Plugin.Log.Error("FFmpeg not found!");
                return false;
            }
            
            try
            {
                // Build FFmpeg command for streaming input
                string arguments = BuildFFmpegArguments();
                
                Plugin.Log.Info($"Starting FFmpeg with args: {arguments}");
                
                ffmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                // Start process
                ffmpegProcess.Start();
                ffmpegInputStream = ffmpegProcess.StandardInput.BaseStream;
                
                // Start encoding thread
                isEncoding = true;
                encodingThread = new Thread(EncodingLoop);
                encodingThread.Start();
                
                Plugin.Log.Info("Video encoder initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to initialize encoder: {ex.Message}");
                return false;
            }
        }
        
        private string BuildFFmpegArguments()
        {
            // Input: raw RGB24 frames from stdin
            string args = $"-f rawvideo -pixel_format rgb24 -video_size {width}x{height} -framerate {frameRate} -i pipe:0";
            
            // Add audio if available
            if (!string.IsNullOrEmpty(audioPath) && File.Exists(audioPath))
            {
                args += $" -i \"{audioPath}\"";
                args += " -c:a aac -b:a 192k";
                args += " -shortest"; // Stop at shortest stream
            }
            
            // Video encoding settings
            string preset = Plugin.Config.FFmpegPreset;
            int crf = Plugin.Config.FFmpegCRF;
            
            args += $" -c:v libx264 -preset {preset} -crf {crf}";
            args += " -pix_fmt yuv420p"; // For compatibility
            
            // Output file
            args += $" \"{outputPath}\" -y";
            
            return args;
        }
        
        /// <summary>
        /// Queue a frame for encoding
        /// </summary>
        public void QueueFrame(Texture2D texture)
        {
            if (!isEncoding || frameQueue.Count >= MAX_QUEUE_SIZE)
            {
                // Wait if queue is full
                while (frameQueue.Count >= MAX_QUEUE_SIZE && isEncoding)
                {
                    Thread.Sleep(1);
                }
            }
            
            // Get raw RGB24 bytes from texture
            byte[] frameData = GetRawRGBData(texture);
            
            frameQueue.Enqueue(frameData);
            totalFramesQueued++;
        }
        
        /// <summary>
        /// Convert Unity texture to raw RGB24 bytes
        /// </summary>
        private byte[] GetRawRGBData(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            byte[] rgbData = new byte[width * height * 3];
            
            int index = 0;
            // Unity textures are bottom-to-top, FFmpeg expects top-to-bottom
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    Color32 pixel = pixels[y * width + x];
                    rgbData[index++] = pixel.r;
                    rgbData[index++] = pixel.g;
                    rgbData[index++] = pixel.b;
                }
            }
            
            return rgbData;
        }
        
        /// <summary>
        /// Background thread that writes frames to FFmpeg
        /// </summary>
        private void EncodingLoop()
        {
            Plugin.Log.Info("Encoding thread started");
            
            try
            {
                while (isEncoding)
                {
                    if (frameQueue.TryDequeue(out byte[] frameData))
                    {
                        // Write frame to FFmpeg stdin
                        ffmpegInputStream.Write(frameData, 0, frameData.Length);
                        ffmpegInputStream.Flush();
                        
                        totalFramesEncoded++;
                        
                        if (totalFramesEncoded % 100 == 0)
                        {
                            Plugin.Log.Info($"Encoded {totalFramesEncoded} frames");
                        }
                    }
                    else
                    {
                        // Queue is empty, wait a bit
                        Thread.Sleep(10);
                    }
                }
                
                Plugin.Log.Info("Encoding loop finished");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Encoding thread error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Finish encoding and close FFmpeg
        /// </summary>
        public void Finish()
        {
            Plugin.Log.Info("Finishing video encoding...");
            
            isEncoding = false;
            
            // Wait for remaining frames to be encoded
            int waitCount = 0;
            while (frameQueue.Count > 0 && waitCount < 100)
            {
                Thread.Sleep(100);
                waitCount++;
            }
            
            // Close FFmpeg stdin to signal end of input
            if (ffmpegInputStream != null)
            {
                try
                {
                    ffmpegInputStream.Close();
                }
                catch { }
            }
            
            // Wait for FFmpeg to finish
            if (ffmpegProcess != null && !ffmpegProcess.HasExited)
            {
                Plugin.Log.Info("Waiting for FFmpeg to finish...");
                ffmpegProcess.WaitForExit(30000); // 30 second timeout
                
                if (!ffmpegProcess.HasExited)
                {
                    Plugin.Log.Warn("FFmpeg did not exit, killing process");
                    ffmpegProcess.Kill();
                }
            }
            
            // Wait for encoding thread
            if (encodingThread != null && encodingThread.IsAlive)
            {
                encodingThread.Join(5000);
            }
            
            Plugin.Log.Info($"Video encoding complete. Total frames: {totalFramesEncoded}");
            
            if (File.Exists(outputPath))
            {
                FileInfo fileInfo = new FileInfo(outputPath);
                Plugin.Log.Info($"Output file: {outputPath} ({fileInfo.Length / 1024 / 1024}MB)");
            }
        }
        
        private string FindFFmpeg()
        {
            if (!string.IsNullOrEmpty(Plugin.Config.FFmpegPath) && File.Exists(Plugin.Config.FFmpegPath))
            {
                return Plugin.Config.FFmpegPath;
            }
            
            string[] possiblePaths = new string[]
            {
                Path.Combine(Application.dataPath, "..", "FFmpeg", "ffmpeg.exe"),
                Path.Combine(Application.dataPath, "..", "Libs", "ffmpeg.exe"),
                @"C:\ffmpeg\bin\ffmpeg.exe",
            };
            
            foreach (string path in possiblePaths)
            {
                try
                {
                    string fullPath = Path.GetFullPath(path);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
                catch { }
            }
            
            return "ffmpeg"; // Try PATH
        }
        
        void OnDestroy()
        {
            if (isEncoding)
            {
                Finish();
            }
        }
    }
}
