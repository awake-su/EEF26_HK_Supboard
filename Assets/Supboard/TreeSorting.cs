using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class TreeSorting : MonoBehaviour
{
    void OnEnable() => Apply();
    void OnValidate() => Apply();

    void Apply()
    {
        foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
            renderer.sortingOrder = Mathf.RoundToInt(-transform.position.z * 100);
    }
}