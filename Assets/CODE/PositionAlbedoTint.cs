using UnityEngine;

namespace AwakeComponents.Coloring
{
    /// <summary>
    /// Разово красит Albedo всех Renderer'ов в детях, смешивая два цвета по Perlin noise —
    /// получаются естественные "пятна" вместо равномерного цвета. Красит через MaterialPropertyBlock —
    /// не создаёт инстансы материалов и не трогает shared material.
    /// </summary>
    [AddComponentMenu("Awake! Components/Position Albedo Tint")]
    [DisallowMultipleComponent]
    public class PositionAlbedoTint : MonoBehaviour
    {
        [Header("Цвета")]
        public Color colorA = Color.white;
        public Color colorB = Color.white;

        [Header("Perlin Noise (пятна)")]
        [Tooltip("Размер пятен в метрах⁻¹: меньше — крупнее и плавнее пятна, больше — мельче и чаще.")]
        public float noiseScale = 0.05f;
        public float noiseSeed;

        [Range(0f, 1f)]
        [Tooltip("0 — мягкий плавный шум, 1 — чёткие контрастные пятна вместо плавного смешивания.")]
        public float patchSharpness = 0f;

        [Tooltip("Имя свойства цвета в шейдере. Для HDRP Lit/LayeredLit — _BaseColor.")]
        public string colorProperty = "_BaseColor";

        public bool applyOnStart = true;

        private void Start()
        {
            if (applyOnStart) Apply();
        }

        [ContextMenu("Применить")]
        public void Apply()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            var block = new MaterialPropertyBlock();

            float edge = 0.5f * (1f - patchSharpness);

            foreach (var renderer in renderers)
            {
                Vector3 pos = renderer.transform.position;

                float t = Mathf.PerlinNoise(pos.x * noiseScale + noiseSeed, pos.z * noiseScale + noiseSeed);
                t = Mathf.SmoothStep(0.5f - edge, 0.5f + edge, t);

                Color final = Color.Lerp(colorA, colorB, t);

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    renderer.GetPropertyBlock(block, i);
                    block.SetColor(colorProperty, final);
                    renderer.SetPropertyBlock(block, i);
                }
            }
        }
    }
}