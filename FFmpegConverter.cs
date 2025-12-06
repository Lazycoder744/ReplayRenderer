using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ReplayRenderer
{
    public class FFmpegConverter
    {
        private readonly string framesPath;
        private readonly string audioPath;
        private readonly int frameRate;

        public FFmpegConverter(string framesPath, string audioPath, int frameRate)
        {
            this.framesPath = framesPath;
            this.audioPath = audioPath;
            this.frameRate = frameRate;
        }

        /// <summary>
        /// Convert captured frames + optional audio to video using FFmpeg
        /// </summary>
        public bool ConvertToVideo()
        {
            try
            {
                string outputFile = Path.Combine(framesPath, "output.mp4");

                // Build FFmpeg arguments
                string args;
                if (!string.IsNullOrEmpty(audioPath) && File.Exists(audioPath))
                {
                    args = $"-y -framerate {frameRate} -i \"{Path.Combine(framesPath, "frame_%06d.png")}\" -i \"{audioPath}\" -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p -c:a aac \"{outputFile}\"";
                }
                else
                {
                    args = $"-y -framerate {frameRate} -i \"{Path.Combine(framesPath, "frame_%06d.png")}\" -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p \"{outputFile}\"";
                }

                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "ffmpeg"; // must be in PATH
                    process.StartInfo.Arguments = args;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    process.OutputDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                    process.ErrorDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine(e.Data); };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit(); // blocking for .NET 4.7.2
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FFmpeg conversion failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get the audio file from the current Beat Saber level
        /// </summary>
        public static string GetCurrentLevelAudioPath()
        {
            try
            {
                // Assuming audio clip stored in UserData or elsewhere
                string audioFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Beat Saber",
                    "Audio"
                );

                if (!Directory.Exists(audioFolder))
                    return null;

                // Get first .ogg or .wav file
                string[] audioFiles = Directory.GetFiles(audioFolder, "*.ogg");
                if (audioFiles.Length == 0)
                    audioFiles = Directory.GetFiles(audioFolder, "*.wav");

                return audioFiles.Length > 0 ? audioFiles[0] : null;
            }
            catch
            {
                return null;
            }
        }
    }
}