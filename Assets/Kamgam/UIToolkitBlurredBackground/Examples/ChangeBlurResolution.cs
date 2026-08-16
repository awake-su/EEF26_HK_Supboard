using UnityEngine;

namespace Kamgam.UIToolkitBlurredBackground
{
    [ExecuteInEditMode]
    public class ChangeBlurResolution : MonoBehaviour
    {
        public bool ChangeResolution = false;
        public Vector2Int Resolution = new Vector2Int(512, 256);

        void Update()
        {
            if (!ChangeResolution)
                return;
            ChangeResolution = false;
            
            // This is not longer necessary as of version 1.4.0 or higher since now multiple resolutions are supported
            // and they are taken directly from the image.
            // var mgr = BlurManager.Instance;
            // if (mgr != null)
            //     mgr.Resolution = Resolution;

        }
    }
}
