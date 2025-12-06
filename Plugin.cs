using IPA;
using IPA.Config;
using IPA.Config.Stores;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;

namespace ReplayRenderer
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }
        internal static PluginConfig Config { get; private set; }

        [Init]
        public void Init(IPALogger logger, Config conf)
        {
            Instance = this;
            Log = logger;
            Config = conf.Generated<PluginConfig>();
            Log.Info("ReplayRenderer initialized.");
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Log.Info("ReplayRenderer starting...");
            
            // Register our manager component that will persist across scenes
            new GameObject("ReplayRendererManager").AddComponent<RenderManager>();
            GameObject.DontDestroyOnLoad(GameObject.Find("ReplayRendererManager"));
            
            // Register BeatLeader UI integration
            new GameObject("BeatLeaderUIIntegration").AddComponent<BeatLeaderUIIntegration>();
            GameObject.DontDestroyOnLoad(GameObject.Find("BeatLeaderUIIntegration"));
            
            Log.Info("ReplayRenderer started successfully.");
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Log.Info("ReplayRenderer shutting down.");
        }
    }
}