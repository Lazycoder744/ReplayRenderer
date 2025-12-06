using System;

namespace ReplayRenderer
{
    /// <summary>
    /// Holds information about the replay being rendered.
    /// This will be populated by BeatLeader/ScoreSaber replay data.
    /// For now, it's a placeholder with basic info.
    /// </summary>
    public class ReplayData
    {
        public string SongName { get; set; }
        public string SongAuthor { get; set; }
        public string LevelAuthor { get; set; }
        public string Difficulty { get; set; }
        public float SongDuration { get; set; } // In seconds
        public string PlayerName { get; set; }
        public int Score { get; set; }
        public float Accuracy { get; set; }
        
        // Replay file path
        public string ReplayFilePath { get; set; }
        
        // Timestamps and positions (will be filled from actual replay file)
        // This is simplified - real implementation would have frame-by-frame data
        public ReplayFrame[] Frames { get; set; }
        
        public ReplayData()
        {
            SongName = "Unknown";
            SongAuthor = "Unknown";
            LevelAuthor = "Unknown";
            Difficulty = "Unknown";
            SongDuration = 180f; // Default 3 minutes
            PlayerName = "Player";
            Score = 0;
            Accuracy = 0f;
        }
    }
    
    /// <summary>
    /// Represents a single frame of replay data.
    /// In a full implementation, this would come from the BSOR format.
    /// </summary>
    [Serializable]
    public class ReplayFrame
    {
        public float Time { get; set; }
        
        // Head position and rotation
        public Vector3Data HeadPosition { get; set; }
        public QuaternionData HeadRotation { get; set; }
        
        // Left saber
        public Vector3Data LeftHandPosition { get; set; }
        public QuaternionData LeftHandRotation { get; set; }
        
        // Right saber
        public Vector3Data RightHandPosition { get; set; }
        public QuaternionData RightHandRotation { get; set; }
        
        public ReplayFrame()
        {
            HeadPosition = new Vector3Data();
            HeadRotation = new QuaternionData();
            LeftHandPosition = new Vector3Data();
            LeftHandRotation = new QuaternionData();
            RightHandPosition = new Vector3Data();
            RightHandRotation = new QuaternionData();
        }
    }
    
    /// <summary>
    /// Serializable Vector3
    /// </summary>
    [Serializable]
    public class Vector3Data
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        
        public Vector3Data() { }
        
        public Vector3Data(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public UnityEngine.Vector3 ToUnityVector3()
        {
            return new UnityEngine.Vector3(X, Y, Z);
        }
    }
    
    /// <summary>
    /// Serializable Quaternion
    /// </summary>
    [Serializable]
    public class QuaternionData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }
        
        public QuaternionData() { }
        
        public QuaternionData(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
        
        public UnityEngine.Quaternion ToUnityQuaternion()
        {
            return new UnityEngine.Quaternion(X, Y, Z, W);
        }
    }
}
