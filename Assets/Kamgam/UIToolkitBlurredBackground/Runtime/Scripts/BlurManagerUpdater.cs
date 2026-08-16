using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Kamgam.UIToolkitBlurredBackground
{
    /// <summary>
    /// This class ensure update is called in play and in edit mode.
    /// </summary>
    [HelpURL("https://kamgam.com/unity/UIToolkitBlurredBackgroundManual.pdf")]
    public class BlurManagerUpdater : MonoBehaviour
    {
        // The instance is only used at runtime. During edit time EditorApplication.update is used.
        static BlurManagerUpdater _instance;
        static BlurManagerUpdater instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Utils.FindRootObjectByType<BlurManagerUpdater>(includeInactive: true);
                    if (_instance == null)
                    {
                        var go = new GameObject("UIToolkit BlurredBackground Updater");
                        _instance = go.AddComponent<BlurManagerUpdater>();
                        _instance.hideFlags = HideFlags.DontSave;
                        Utils.SmartDontDestroyOnLoad(_instance.gameObject);
                    }
                }
                return _instance;
            }
        }
        
        public Action OnUpdate;

        public void Update()
        {
            OnUpdate?.Invoke();
        }

        public static void Init(Action updateFunc)
        {
#if !UNITY_EDITOR
            // Runtime
            instance.OnUpdate += updateFunc;
#else
            // Editor
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                instance.OnUpdate += updateFunc;
            }
            
            _action = updateFunc;
            EditorApplication.update -= updateInEditor;
            EditorApplication.update += updateInEditor;
#endif
        }

#if UNITY_EDITOR
        static Action _action;
        static void updateInEditor()
        {
            // Use editor update only if not in play mode.
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                _action?.Invoke();
        }
#endif
    }
}

