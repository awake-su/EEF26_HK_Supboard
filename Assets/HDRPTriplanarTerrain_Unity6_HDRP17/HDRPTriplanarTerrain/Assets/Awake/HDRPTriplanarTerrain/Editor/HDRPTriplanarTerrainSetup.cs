using UnityEditor;
using UnityEngine;

namespace Awake.HDRPTriplanarTerrain.Editor
{
    public static class HDRPTriplanarTerrainSetup
    {
        private const string GraphName = "Awake/HDRP Triplanar Terrain";

        [MenuItem("Tools/Awake/HDRP Triplanar Terrain/Validate Selection")]
        private static void ValidateSelection()
        {
            var material = Selection.activeObject as Material;
            if (material == null)
            {
                EditorUtility.DisplayDialog("HDRP Triplanar Terrain",
                    "Select a material that uses the triplanar Shader Graph.", "OK");
                return;
            }

            var missing = "";
            string[] textureProperties =
            {
                "_SandAlbedo", "_SandNormal", "_SandMask",
                "_GrassAlbedo", "_GrassNormal", "_GrassMask",
                "_RockAlbedo", "_RockNormal", "_RockMask"
            };

            foreach (var property in textureProperties)
                if (!material.HasProperty(property) || material.GetTexture(property) == null)
                    missing += "\n• " + property;

            EditorUtility.DisplayDialog("HDRP Triplanar Terrain",
                missing.Length == 0
                    ? "All nine texture slots are assigned."
                    : "These texture slots are empty:" + missing,
                "OK");
        }
    }
}
