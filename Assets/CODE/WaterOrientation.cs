using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class WaterOrientationFollower : MonoBehaviour
{
    public WaterSurface targetSurface;
    public Transform source; // тележка

    [Tooltip("Ниже этого порога forward тележки считается ненадёжным (смотрит почти вертикально) — направление не обновляется.")]
    public float minHorizontalMagnitude = 0.15f;

    [Tooltip("Скорость доворота волны к целевому направлению, градусов/сек.")]
    public float rotationSpeed = 60f;

    private float _currentAngle;
    private bool _initialized;

    void Update()
    {
        if (targetSurface == null || source == null) return;

        Vector3 fwd = source.forward;
        fwd.y = 0f;

        if (fwd.sqrMagnitude >= minHorizontalMagnitude * minHorizontalMagnitude)
        {
            // Orientation воды отсчитывается от оси X (0°=+X, 90°=+Z)
            float targetAngle = Mathf.Atan2(fwd.z, fwd.x) * Mathf.Rad2Deg;

            if (!_initialized)
            {
                _currentAngle = targetAngle;
                _initialized = true;
            }
            else
            {
                _currentAngle = Mathf.MoveTowardsAngle(_currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
            }
        }
        // иначе камера смотрит почти вертикально — держим последний валидный угол

        targetSurface.largeOrientationValue = _currentAngle;
    }
}