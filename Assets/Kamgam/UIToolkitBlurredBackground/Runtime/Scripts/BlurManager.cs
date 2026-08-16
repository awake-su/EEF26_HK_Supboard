using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitBlurredBackground
{
    /// <summary>
    /// This manager keeps track of whether or not the blur is needed and disables the
    /// rendering if not. This is done to save performance when no blurred UI is shown.
    /// </summary>
    public class BlurManager
    {
        static BlurManager _instance;

        public static BlurManager Instance // This is triggered by the UI Toolkit Elements
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BlurManager();
#if !UNITY_EDITOR
                    _instance.registerToUpdate();
#endif
                }

                return _instance;
            }
        }

        // Reset static variables on play mode enter to support disabling domain reload.
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlayModeEnter()
        {
            if (_instance != null)
            {
                defragRenderers();
                foreach (var renderer in Renderers)
                {
                    renderer.Active = false;
                }
                _instance = null;
            }
            Instance.registerToUpdate();
        }
        
        [DidReloadScripts]
        static void ResetOnScriptReload()
        {
            EditorApplication.playModeStateChanged -= onPlayModeChanged;
            EditorApplication.playModeStateChanged += onPlayModeChanged;
            
            // Only do it in editor mode after reload, not in play mode.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            
            if (_instance != null)
            {
                defragRenderers();
                foreach (var renderer in Renderers)
                {
                    renderer.Active = false;
                }

                _instance = null;
            }
            Instance.registerToUpdate();
        }

        private static void onPlayModeChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.EnteredEditMode)
            {
                if (_instance != null)
                {
                    defragRenderers();
                    foreach (var renderer in Renderers)
                    {
                        renderer.Active = false;
                    }
                    _instance = null;
                }

                Instance.registerToUpdate();
            }
        }


#endif

        void registerToUpdate()
        {
            // Register this classes Update() method in the Unity update loop for runtime and editor.
            BlurManagerUpdater.Init(Update); 
        }
        
        // -------------------


        private void logDeprecatedMsg() => Debug.Log("Deprecated since multiple blur settings at the same time are supported since v1.4.0. Please use the image attributes instead.");
        
        public int Iterations
        {
            get { logDeprecatedMsg(); return 1; }
            set { logDeprecatedMsg(); }
        }

        public float Offset
        {
            get { logDeprecatedMsg(); return 10f; }
            set { logDeprecatedMsg(); }
        }

        public Vector2Int Resolution
        {
            get { logDeprecatedMsg(); return new Vector2Int(512, 512); }
            set { logDeprecatedMsg(); Debug.LogWarning("If you want to set a non-square resolution use BlurResolutionSize with IgnoreSquareResolution set to TRUE on the image."); }
        }

        public ShaderQuality Quality
        {
            get { logDeprecatedMsg(); return ShaderQuality.Medium; }
            set { logDeprecatedMsg(); }
        }
        
        
        [System.NonSerialized]
        public static List<IBlurRenderer> Renderers = new ();

        static void defragRenderers()
        {
            for (int i = Renderers.Count-1; i >= 0; i--)
            {
                if (Renderers[i] == null)
                {
                    Renderers.RemoveAt(i);
                }
            }
        }
        
        
        public static bool IsImageMatchingRenderer(BlurredBackground image, IBlurRenderer renderer)
        {
            return image.BlurIterations == renderer.Iterations
                   && image.BlurQuality == renderer.Quality
                   && image.BlurResolutionSize == renderer.Resolution
                   && Mathf.Abs(image.BlurStrength - renderer.Offset) < 0.01f;

        }
        
        public static bool IsImageBlurRenderingRequired(BlurredBackground image)
        {
            return image.BlurIterations > 0 && image.BlurStrength > 0.001f && IsImageVisibleInUI(image);
        }

        public static bool IsImageVisibleInUI(BlurredBackground image)
        {
            return image.visible && image.resolvedStyle.display != DisplayStyle.None;
        }

        public Texture GetBlurredTexture(BlurredBackground image)
        {
            return GetOrCreateRenderer(image).GetBlurredTexture();
        }
        
        public Texture GetBlurredTextureWorld(BlurredBackground image)
        {
            return GetOrCreateRenderer(image).GetBlurredTextureWorld();
        }

        IBlurRenderer getRendererMatchingImage(BlurredBackground image)
        {
            foreach (var renderer in Renderers)
            {
                if (IsImageMatchingRenderer(image, renderer))
                {
                    return renderer;
                }
            }

            return null;
        }
        
        IBlurRenderer getRendererNotMatchingAnyImage()
        {
            foreach (var renderer in Renderers)
            {
                // Check if the renderer has any images that match it. If not then we can return it.
                bool foundImage = false;
                foreach (var image in _blurredBackgroundElements)
                {
                    if (IsImageMatchingRenderer(image, renderer))
                    {
                        foundImage = true;
                        break;
                    }
                }

                if (!foundImage)
                    return renderer;
            }

            return null;
        }

        /// <summary>
        /// If there are any renderers that match the image blur settings the nothing will happen.
        /// If not then an old renderer is re-used or a new one is created.
        /// </summary>
        public IBlurRenderer GetOrCreateRenderer(BlurredBackground image)
        {
            defragRenderers();
            
            // Find
            var renderer = getRendererMatchingImage(image);
            if (renderer == null)
            {
                renderer = getRendererNotMatchingAnyImage();
                if (renderer != null)
                {
                    // Re-use
                    // Debug.Log("Reusing " + renderer.Offset);
                    // Debug.Log("Total number of renderers: " + Renderers.Count);
                    renderer.Iterations = image.BlurIterations;
                    renderer.Offset = image.BlurStrength;
                    renderer.Quality = image.BlurQuality;
                    renderer.Resolution = image.BlurResolutionSize;
                }
                else
                {
                    // Create new
                    renderer = createRenderer(image);
                    // Debug.Log("Creating new renderer " + renderer.Offset);
                    Renderers.Add(renderer);
                    // Debug.Log("Total number of renderers: " + Renderers.Count);
                }
            }
            
            return renderer;
        }
        
        IBlurRenderer createRenderer(BlurredBackground ve)
        {
#if !KAMGAM_RENDER_PIPELINE_URP && !KAMGAM_RENDER_PIPELINE_HDRP
            var renderer = new BlurRendererBuiltIn(); // BuiltIn
#elif KAMGAM_RENDER_PIPELINE_URP
            var renderer = new BlurRendererURP(); // URP
#else
            var renderer = new BlurRendererHDRP(); // HDRP
#endif

            if (ve != null)
            {
                renderer.Iterations = ve.BlurIterations;
                renderer.Quality = ve.BlurQuality;
                renderer.Offset = ve.BlurStrength;
                renderer.Resolution = ve.BlurResolutionSize;
            }
            
            return renderer;
        }

        /// <summary>
        /// Keeps track of how many elements use the blurred texture. If none are using it then
        /// the rendering will be paused to save performance.
        /// </summary>
        protected List<BlurredBackground> _blurredBackgroundElements = new List<BlurredBackground>();

        public List<BlurredBackground> BlurredBackgroundElements => _blurredBackgroundElements;

        public void AttachElement(BlurredBackground ele)
        {
            if (!_blurredBackgroundElements.Contains(ele))
            {
                _blurredBackgroundElements.Add(ele);
                ele.MarkDirtyRepaint();
            }

            updateRenderersActiveState();
        }

        public void DetachElement(BlurredBackground ele)
        {
            if (_blurredBackgroundElements.Contains(ele))  
            {
                _blurredBackgroundElements.Remove(ele);
            }

            updateRenderersActiveState();
        }

        protected void updateRenderersActiveState()
        {
            // Disable rendering if no elements with blurred background are visible.
            foreach (var renderer in Renderers)
            {
                int usageCount = countVisibleImagesThatBlurAndMatchTheRenderer(renderer);
                renderer.Active = usageCount > 0;
            }
        }
        
        protected int countVisibleImagesThatBlurAndMatchTheRenderer(IBlurRenderer renderer)
        {
            int count = 0;
            foreach (var blurredBackground in _blurredBackgroundElements)
            {
                if (   IsImageMatchingRenderer(blurredBackground, renderer) 
                    && IsImageBlurRenderingRequired(blurredBackground)
                   )
                {
                    count++;
                }
            }

            return count;
        }
        
        public void Update()
        {
            updateRenderersActiveState();
            
            // Keep the renderers in sync with the current main camera.
            foreach (var renderer in Renderers)
            {
                renderer.Update();
            }

            // Update image if needed (usually only needed for world space panels) 
            foreach (var blurredBackground in _blurredBackgroundElements)
            {
                blurredBackground.UpdateIfNecessary();
            }
        }
    }
}