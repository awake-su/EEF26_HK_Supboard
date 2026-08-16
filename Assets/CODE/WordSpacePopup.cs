using System.Collections;
using UnityEngine;

public class WorldSpacePopup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Renderer[] renderers;

    [Header("Animation")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float showTime = 2f;
    [SerializeField] private float moveOffset = 0.5f;

    [Header("3D Material")]
    [Range(0f, 1f)]
    [SerializeField] private float maxMetallic = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float maxSmoothness = 1f;

    private Vector3 defaultPosition;
    private Vector3 startPosition;

    private MaterialPropertyBlock propertyBlock;

    private static readonly int MetallicID =
        Shader.PropertyToID("_Metallic");

    private static readonly int SmoothnessID =
        Shader.PropertyToID("_Smoothness");

    private void Awake()
    {
        defaultPosition = transform.localPosition;
        startPosition = defaultPosition + Vector3.down * moveOffset;

        propertyBlock = new MaterialPropertyBlock();

        SetHiddenState();
    }

    public void Show()
    {
        StopAllCoroutines();
        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        SetHiddenState();

        // Включаем 3D объект перед началом появления
        SetRenderersEnabled(true);

        // =========================
        // SHOW
        // =========================

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            float ease = EaseOut(t);

            ApplyShowState(ease);

            yield return null;
        }

        ApplyShowState(1f);

        // =========================
        // SHOW TIME
        // =========================

        yield return new WaitForSeconds(showTime);

        // =========================
        // HIDE
        // =========================

        time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            float ease = EaseOut(t);

            ApplyHideState(ease);

            yield return null;
        }

        SetHiddenState();
    }

    private void ApplyShowState(float t)
    {
        // При появлении двигаемся снизу вверх
        transform.localPosition = Vector3.Lerp(
            startPosition,
            defaultPosition,
            t
        );

        canvasGroup.alpha = t;

        SetMaterialValues(t);
    }

    private void ApplyHideState(float t)
    {
        // При исчезновении позицию НЕ меняем
        canvasGroup.alpha = 1f - t;

        SetMaterialValues(1f - t);
    }

    private void SetHiddenState()
    {
        transform.localPosition = startPosition;

        canvasGroup.alpha = 0f;

        SetMaterialValues(0f);

        SetRenderersEnabled(false);
    }

    private void SetMaterialValues(float t)
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetFloat(
                MetallicID,
                Mathf.Lerp(0f, maxMetallic, t)
            );

            propertyBlock.SetFloat(
                SmoothnessID,
                Mathf.Lerp(0f, maxSmoothness, t)
            );

            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = enabled;
        }
    }

    private float EaseOut(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}