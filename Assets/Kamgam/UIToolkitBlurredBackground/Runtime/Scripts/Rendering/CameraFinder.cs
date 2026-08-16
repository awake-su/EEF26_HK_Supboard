using UnityEngine;
#if KAMGAM_RENDER_PIPELINE_URP
using UnityEngine.Rendering.Universal;
#endif

namespace Kamgam.UIToolkitBlurredBackground
{
    public static class CameraFinder
    {
        static Camera[] _tmpAllCameras = new Camera[10];
        
        public static Camera FindPlayModeCamera()
        {
            // All of this is only to support multiple-camera setups with render textures.
            // The blur only needs to be done on one camera (usually the main camera). That's
            // why the stop on all other cameras.
            Camera cam = Camera.main;
            if (cam == null)
            {
                // No main camera -> let's check if there are cameras that
                // are NOT rendering into render textures.
                Camera firstCamWithoutRenderTexture = null;
                int camCount = Camera.allCamerasCount;
                int maxCamCount = _tmpAllCameras.Length;
                // alloc new array if needed
                if(camCount > maxCamCount)
                {
                    _tmpAllCameras = new Camera[camCount + 5];
                }
                Camera.GetAllCameras(_tmpAllCameras);
                for (int i = 0; i < maxCamCount; i++)
                {
                    // Null out old references
                    if(i >= camCount)
                    {
                        _tmpAllCameras[i] = null;
                        continue;
                    }

                    var cCam = _tmpAllCameras[i];

                    if (cCam == null || !cCam.isActiveAndEnabled)
                        continue;

                    // Skip overlay cameras.
                    if (IsOverlayCamera(cCam))
                        continue;

                    if (cCam != null && cCam.targetTexture == null)
                    {
                        firstCamWithoutRenderTexture = cCam;
                        break;
                    }
                }

                // If there are some then use the first we found.
                if (firstCamWithoutRenderTexture != null)
                    return firstCamWithoutRenderTexture;

                // If there are only cameras with render textures then we do nothing.
            }

            return cam;
        }
        
        public static bool IsOverlayCamera(Camera cam)
        {
#if KAMGAM_RENDER_PIPELINE_URP
            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            if (data != null && data.renderType == CameraRenderType.Overlay)
            {
                return true;
            }
#endif
            
            // If not URP then always return false.
            return false;
        }
    }

}