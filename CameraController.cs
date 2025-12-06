using System.Linq;
using UnityEngine;

namespace ReplayRenderer
{
    public class CameraController : MonoBehaviour
    {
        private Camera targetCamera;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private float originalFOV;
        
        private bool isFirstPerson;
        private Transform headTransform;
        private GameObject fpfcCamera; // Reference to FPFC camera if available
        
        void Start()
        {
            Plugin.Log.Info("CameraController initialized");
            
            // Find the main camera
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Plugin.Log.Error("No main camera found!");
                enabled = false;
                return;
            }
            
            // Store original camera settings
            originalPosition = targetCamera.transform.position;
            originalRotation = targetCamera.transform.rotation;
            originalFOV = targetCamera.fieldOfView;
            
            // Apply custom camera settings
            ApplyCameraSettings();
            
            Plugin.Log.Info($"Camera setup complete - FOV: {targetCamera.fieldOfView}, Position: {targetCamera.transform.position}");
        }
        
        void Update()
        {
            // Allow runtime camera adjustments with keyboard (for testing)
            if (Input.GetKey(KeyCode.LeftControl))
            {
                HandleCameraControls();
            }
            
            // Update camera position if in first person mode
            if (isFirstPerson && headTransform != null)
            {
                // In a real implementation, this would follow the replay head position
                // For now, we just keep the camera at the configured position
            }
        }
        
        private void ApplyCameraSettings()
        {
            // Set Field of View
            targetCamera.fieldOfView = Plugin.Config.FOV;
            
            // Set camera mode
            isFirstPerson = Plugin.Config.FirstPersonMode;
            
            // Check for FPFC (First Person Flying Camera) mod
            if (Plugin.Config.UseFPFCIfAvailable && TryUseFPFCCamera())
            {
                Plugin.Log.Info("Using FPFC camera mode");
                return;
            }
            
            if (isFirstPerson)
            {
                // First person - try to find the player's head
                GameObject playerAvatar = GameObject.Find("PlayerAvatar");
                if (playerAvatar != null)
                {
                    headTransform = playerAvatar.transform.Find("Head");
                }
                
                if (headTransform != null)
                {
                    Plugin.Log.Info("First person mode - tracking head");
                }
                else
                {
                    Plugin.Log.Warn("Could not find player head, using configured position");
                    SetThirdPersonCamera();
                }
            }
            else
            {
                // Third person - use configured position
                SetThirdPersonCamera();
            }
        }
        
        private bool TryUseFPFCCamera()
        {
            // Try to find FPFC camera
            // FPFC typically creates a camera object with specific naming
            GameObject[] allCameras = GameObject.FindObjectsOfType<GameObject>();
            
            foreach (var obj in allCameras)
            {
                // Common FPFC camera names
                if (obj.name.Contains("FPFC") || 
                    obj.name.Contains("FlyingCamera") || 
                    obj.name.Contains("FirstPersonFlying"))
                {
                    Camera fpfcCam = obj.GetComponent<Camera>();
                    if (fpfcCam != null)
                    {
                        fpfcCamera = obj;
                        targetCamera = fpfcCam;
                        Plugin.Log.Info($"Found FPFC camera: {obj.name}");
                        
                        // Apply FOV to FPFC camera
                        targetCamera.fieldOfView = Plugin.Config.FOV;
                        
                        return true;
                    }
                }
            }
            
            // Alternative: Check if Camera2 mod is present
            // Camera2 is another popular camera mod that might be used
            var camera2Objects = allCameras.Where(c => 
                c.name.ToLower().Contains("camera2") || 
                c.name.ToLower().Contains("cameraplus"));
            
            foreach (var obj in camera2Objects)
            {
                Camera cam = obj.GetComponent<Camera>();
                if (cam != null)
                {
                    fpfcCamera = obj;
                    targetCamera = cam;
                    Plugin.Log.Info($"Found Camera2/CameraPlus camera: {obj.name}");
                    targetCamera.fieldOfView = Plugin.Config.FOV;
                    return true;
                }
            }
            
            Plugin.Log.Info("No FPFC or compatible camera mod found");
            return false;
        }
        
        private void SetThirdPersonCamera()
        {
            Vector3 position = new Vector3(
                Plugin.Config.CameraX,
                Plugin.Config.CameraY,
                Plugin.Config.CameraZ
            );
            
            targetCamera.transform.position = position;
            
            // Look at the center of the play area
            targetCamera.transform.LookAt(new Vector3(0, 1.5f, 0));
            
            Plugin.Log.Info($"Third person camera set to: {position}");
        }
        
        private void HandleCameraControls()
        {
            // Camera position adjustment (CTRL + Arrow Keys)
            float moveSpeed = 0.1f * Time.deltaTime / Time.captureFramerate;
            
            if (Input.GetKey(KeyCode.UpArrow))
            {
                targetCamera.transform.Translate(Vector3.forward * moveSpeed);
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                targetCamera.transform.Translate(Vector3.back * moveSpeed);
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                targetCamera.transform.Translate(Vector3.left * moveSpeed);
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                targetCamera.transform.Translate(Vector3.right * moveSpeed);
            }
            if (Input.GetKey(KeyCode.PageUp))
            {
                targetCamera.transform.Translate(Vector3.up * moveSpeed);
            }
            if (Input.GetKey(KeyCode.PageDown))
            {
                targetCamera.transform.Translate(Vector3.down * moveSpeed);
            }
            
            // FOV adjustment (CTRL + Plus/Minus)
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                targetCamera.fieldOfView = Mathf.Clamp(targetCamera.fieldOfView + 5f, 30f, 120f);
                Plugin.Log.Info($"FOV: {targetCamera.fieldOfView}");
            }
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                targetCamera.fieldOfView = Mathf.Clamp(targetCamera.fieldOfView - 5f, 30f, 120f);
                Plugin.Log.Info($"FOV: {targetCamera.fieldOfView}");
            }
            
            // Reset camera (CTRL + R)
            if (Input.GetKeyDown(KeyCode.R))
            {
                ApplyCameraSettings();
                Plugin.Log.Info("Camera reset to config settings");
            }
            
            // Print current position (CTRL + P)
            if (Input.GetKeyDown(KeyCode.P))
            {
                Vector3 pos = targetCamera.transform.position;
                Vector3 rot = targetCamera.transform.eulerAngles;
                Plugin.Log.Info($"Camera Position: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})");
                Plugin.Log.Info($"Camera Rotation: ({rot.x:F2}, {rot.y:F2}, {rot.z:F2})");
                Plugin.Log.Info($"FOV: {targetCamera.fieldOfView:F1}");
            }
        }
        
        void OnDestroy()
        {
            // Restore original camera settings
            if (targetCamera != null)
            {
                targetCamera.transform.position = originalPosition;
                targetCamera.transform.rotation = originalRotation;
                targetCamera.fieldOfView = originalFOV;
                
                Plugin.Log.Info("Camera settings restored");
            }
        }
        
        /// <summary>
        /// Call this to update camera position from replay data (future implementation)
        /// </summary>
        public void UpdateFromReplayFrame(ReplayFrame frame)
        {
            if (targetCamera == null || frame == null) return;
            
            if (isFirstPerson)
            {
                // Position camera at head position
                targetCamera.transform.position = frame.HeadPosition.ToUnityVector3();
                targetCamera.transform.rotation = frame.HeadRotation.ToUnityQuaternion();
            }
            // Third person camera stays fixed
        }
    }
}