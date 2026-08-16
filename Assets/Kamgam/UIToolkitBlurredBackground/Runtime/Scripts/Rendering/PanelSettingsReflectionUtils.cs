using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitBlurredBackground
{
    public static class PanelSettingsReflectionUtils
    {
        private static PropertyInfo _renderModeProperty;
        private static object _worldSpaceEnumValue;

        static PanelSettingsReflectionUtils()
        {
            // Cache reflection results
            var panelSettingsType = typeof(PanelSettings);
            _renderModeProperty = panelSettingsType.GetProperty("renderMode", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            if (_renderModeProperty != null)
            {
                var renderModeType = _renderModeProperty.PropertyType;
                _worldSpaceEnumValue = Enum.Parse(renderModeType, "WorldSpace");
            }
        }

        public static bool IsRenderModeWorldSpace(PanelSettings panelSettings)
        {
            if (panelSettings == null)
                return false;

            // fall back to screen space if reflection lookup failed.
            if (_renderModeProperty == null || _worldSpaceEnumValue == null)
                return false;

            var renderMode = _renderModeProperty.GetValue(panelSettings, null);
            return Equals(renderMode, _worldSpaceEnumValue);
        }
    }

}