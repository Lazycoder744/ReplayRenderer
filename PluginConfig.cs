using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace ReplayRenderer
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }

        // Render Settings
        public int FFmpegCRF = 18;
        public string FFmpegPreset { get; set; } = "medium";
        public virtual int RenderWidth { get; set; } = 1920;
        public virtual int RenderHeight { get; set; } = 1080;
        public virtual int TargetFrameRate { get; set; } = 60;
        public virtual string OutputFolder { get; set; } = "RenderOutput";
        
        // Camera Settings
        public virtual float FOV { get; set; } = 90f;
        public virtual bool FirstPersonMode { get; set; } = true;
        public virtual bool UseFPFCIfAvailable { get; set; } = true; // Use FPFC camera if installed
        public virtual float CameraX { get; set; } = 0f;
        public virtual float CameraY { get; set; } = 1.8f;
        public virtual float CameraZ { get; set; } = -2f;
        
        // Graphics Settings
        public virtual bool EnableMirrors { get; set; } = true;
        public virtual bool EnableBloom { get; set; } = true;
        public virtual int AntiAliasing { get; set; } = 4; // 0, 2, 4, 8
        
        // Render Options
        public virtual bool AllowPause { get; set; } = true;
        public virtual bool AutoConvertToVideo { get; set; } = false;
        public virtual string FFmpegPath { get; set; } = "";
        public virtual bool ShowProgressUI { get; set; } = true;
        public virtual bool HideGameUI { get; set; } = true; // Auto-hide UI when rendering starts
        public virtual bool AutoRestartOnRenderStart { get; set; } = false; // Restart level when starting render
        
        // Debug
        public virtual bool VerboseLogging { get; set; } = false;

        /// <summary>
        /// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
        /// </summary>
        public virtual void OnReload()
        {
            // Do stuff after config is read from disk.
        }

        /// <summary>
        /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
        /// </summary>
        public virtual void Changed()
        {
            // Do stuff when the config is changed.
        }

        /// <summary>
        /// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
        /// </summary>
        public virtual void CopyFrom(PluginConfig other)
        {
            // This instance's members populated from other
            RenderWidth = other.RenderWidth;
            RenderHeight = other.RenderHeight;
            TargetFrameRate = other.TargetFrameRate;
            OutputFolder = other.OutputFolder;
            FOV = other.FOV;
            FirstPersonMode = other.FirstPersonMode;
            UseFPFCIfAvailable = other.UseFPFCIfAvailable;
            CameraX = other.CameraX;
            CameraY = other.CameraY;
            CameraZ = other.CameraZ;
            EnableMirrors = other.EnableMirrors;
            EnableBloom = other.EnableBloom;
            AntiAliasing = other.AntiAliasing;
            AllowPause = other.AllowPause;
            AutoConvertToVideo = other.AutoConvertToVideo;
            FFmpegPath = other.FFmpegPath;
            ShowProgressUI = other.ShowProgressUI;
            HideGameUI = other.HideGameUI;
            AutoRestartOnRenderStart = other.AutoRestartOnRenderStart;
            VerboseLogging = other.VerboseLogging;
        }
    }
}
