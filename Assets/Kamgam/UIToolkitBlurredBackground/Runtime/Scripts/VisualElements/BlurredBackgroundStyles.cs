using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitBlurredBackground
{
    public static class BlurredBackgroundStyles
    {
        public static CustomStyleProperty<float> Strength = new CustomStyleProperty<float>("--blur-strength");
        public static CustomStyleProperty<int> Iterations = new CustomStyleProperty<int>("--blur-iterations");
        /// <summary>
        /// low, medium, high
        /// </summary>
        public static CustomStyleProperty<string> Quality = new CustomStyleProperty<string>("--blur-quality");
        public static CustomStyleProperty<int> Resolution = new CustomStyleProperty<int>("--blur-resolution");
        public static CustomStyleProperty<Color> Tint = new CustomStyleProperty<Color>("--blur-tint");
        public static CustomStyleProperty<float> MeshCornerOverlap = new CustomStyleProperty<float>("--blur-mesh-corner-overlap");
        public static CustomStyleProperty<int> MeshCornerSegments = new CustomStyleProperty<int>("--blur-mesh-corner-segments");
        public static CustomStyleProperty<Color> BackgroundColor = new CustomStyleProperty<Color>("--blur-background-color");

        public static float ResolveStyle(CustomStyleProperty<float> property, VisualElement element, float defaultValue)
        {
            if (element.customStyle.TryGetValue(property, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public static int ResolveStyle(CustomStyleProperty<int> property, VisualElement element, int defaultValue)
        {
            if (element.customStyle.TryGetValue(property, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public static bool ResolveStyle(CustomStyleProperty<bool> property, VisualElement element, bool defaultValue)
        {
            if (element.customStyle.TryGetValue(property, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public static Color ResolveStyle(CustomStyleProperty<Color> property, VisualElement element, Color defaultValue)
        {
            if (element.customStyle.TryGetValue(property, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public static string ResolveStyle(CustomStyleProperty<string> property, VisualElement element, string defaultValue)
        {
            if (element.customStyle.TryGetValue(property, out var value))
            {
                return value;
            }

            return defaultValue;
        }
    }
}