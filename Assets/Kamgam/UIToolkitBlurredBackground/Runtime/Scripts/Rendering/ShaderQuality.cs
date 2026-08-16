using System;

namespace Kamgam.UIToolkitBlurredBackground
{
    public enum ShaderQuality { Low, Medium, High };

    public static class ShaderQualityTools
    {
        public static ShaderQuality FromString(string str)
        {
            if(Enum.TryParse(str, out ShaderQuality result))
            {
                return result;
            }

            return default;
        }
    }
}