using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace AwakeComponents.WaterFloat
{
    /// <summary>
    /// Ориентирует объект на поверхности HDRP-воды (Water System) по нескольким опорным точкам.
    /// Каждая точка сэмплирует высоту воды через WaterSurface.ProjectPointOnWaterSurface,
    /// после чего по полученным точкам восстанавливается плоскость (нормаль) и позиция/поворот
    /// объекта плавно подгоняются под неё — получается наклон "по волне", а не просто болтание по высоте.
    /// </summary>
    /// <remarks>
    /// ВАЖНО: на WaterSurface и в HDRP Asset (раздел Water) должна быть включена опция
    /// "Script Interactions" — иначе ProjectPointOnWaterSurface всегда будет возвращать false.
    /// </remarks>
    [AddComponentMenu("Awake! Components/Water Floater")]
    [DisallowMultipleComponent]
    public class WaterFloater : MonoBehaviour
    {
        #region Public Settings

        [Space, Header("Поверхность воды")]
        [Tooltip("HDRP Water Surface, на который ориентируется объект. У поверхности должны быть включены Script Interactions.")]
        public WaterSurface targetSurface;

        [Space, Header("Точки ориентации (границы объекта)")]
        [Tooltip("Опорные точки (например: нос, корма, левый борт, правый борт), по которым считается высота и наклон объекта. " +
                 "Минимум 3. Если список пуст — точки сгенерируются автоматически из Bounds Size.")]
        public Transform[] orientationPoints;

        [Tooltip("Если orientationPoints пуст — считать реальные границы объекта по всем Renderer'ам в детях " +
                 "(учитывает смещение pivot от геометрического центра модели). Рекомендуется оставлять включённым.")]
        public bool autoDetectBoundsFromRenderers = true;

        [Tooltip("Размер объекта (X — ширина, Y — длина по Z), используется только если orientationPoints пуст И " +
                 "autoDetectBoundsFromRenderers выключен (или рендереры не найдены): точки ставятся симметрично вокруг pivot.")]
        public Vector2 boundsSize = new Vector2(2f, 4f);

        [Space, Header("Плавучесть")]
        [Tooltip("Смещение по высоте относительно рассчитанной поверхности воды (осадка судна).")]
        public float waterLineOffset;

        [Space, Header("Сглаживание")]
        [Tooltip("Скорость сглаживания позиции. Чем выше — тем быстрее объект догоняет волну.")]
        public float positionSmoothing = 4f;
        [Tooltip("Скорость сглаживания поворота.")]
        public float rotationSmoothing = 3f;

        [Space, Header("Поиск на поверхности воды")]
        [Tooltip("Погрешность поиска высоты. Меньше — точнее, но дороже по CPU.")]
        public float searchError = 0.01f;
        [Tooltip("Максимум итераций поиска высоты на одну точку за кадр.")]
        public int searchMaxIterations = 8;

        [Space, Header("Отладка")]
        public bool debug;
        public bool drawGizmos = true;

        #endregion

        #region Private

        private Vector3[] _localPoints;
        private WaterSearchParameters[] _searchParams;
        private WaterSearchResult[] _searchResults;
        private Vector3[] _worldHeights;
        private bool _warnedNoSurface;

        #endregion

        private void Awake()
        {
            BuildOrientationPoints();
        }

        /// <summary>
        /// Формирует набор локальных точек, по которым будет считаться ориентация,
        /// и подготавливает буферы под них.
        /// </summary>
        private void BuildOrientationPoints()
        {
            if (orientationPoints != null && orientationPoints.Length >= 3)
            {
                _localPoints = new Vector3[orientationPoints.Length];
                for (int i = 0; i < orientationPoints.Length; i++)
                {
                    _localPoints[i] = orientationPoints[i] != null
                        ? transform.InverseTransformPoint(orientationPoints[i].position)
                        : Vector3.zero;
                }
            }
            else if (autoDetectBoundsFromRenderers && TryComputeLocalBounds(out Vector3 localCenter, out Vector3 localExtents))
            {
                // Точки строятся от РЕАЛЬНОГО центра геометрии, а не от pivot — это критично,
                // если pivot смещён относительно корпуса (частый случай для импортированных моделей).
                _localPoints = new[]
                {
                    localCenter + new Vector3(0f, 0f, localExtents.z),   // нос
                    localCenter + new Vector3(0f, 0f, -localExtents.z),  // корма
                    localCenter + new Vector3(-localExtents.x, 0f, 0f),  // левый борт
                    localCenter + new Vector3(localExtents.x, 0f, 0f)    // правый борт
                };
            }
            else
            {
                float hx = boundsSize.x * 0.5f;
                float hz = boundsSize.y * 0.5f;
                _localPoints = new[]
                {
                    new Vector3(0f, 0f, hz),   // нос
                    new Vector3(0f, 0f, -hz),  // корма
                    new Vector3(-hx, 0f, 0f),  // левый борт
                    new Vector3(hx, 0f, 0f)    // правый борт
                };
            }

            int count = _localPoints.Length;
            _searchParams = new WaterSearchParameters[count];
            _searchResults = new WaterSearchResult[count];
            _worldHeights = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                _searchParams[i] = new WaterSearchParameters();
                _searchResults[i] = new WaterSearchResult();
            }

            ValidatePointsSpread();
        }

        /// <summary>
        /// Если включён debug — предупреждает, когда опорные точки почти не разнесены по X или Z.
        /// Это самая частая причина, когда объект "не чувствует" наклон вдоль одной из осей:
        /// порядок точек в массиве не важен (нормаль считается через cross product и не зависит
        /// от того, какую точку ты назвал "носом"), а вот их реальное расположение в пространстве — важно.
        /// </summary>
        private void ValidatePointsSpread()
        {
            if (!debug || _localPoints == null || _localPoints.Length < 3) return;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var p in _localPoints)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z);
            }

            float spanX = maxX - minX;
            float spanZ = maxZ - minZ;

            if (spanZ < 0.2f)
                Debug.LogWarning($"[WaterFloater] Точки ориентации почти не разнесены по Z (разброс {spanZ:F2} м) — объект не будет чувствовать наклон вдоль своей длины (тангаж). Разнеси точки от носа до кормы.", this);

            if (spanX < 0.2f)
                Debug.LogWarning($"[WaterFloater] Точки ориентации почти не разнесены по X (разброс {spanX:F2} м) — объект не будет чувствовать наклон вбок (крен). Разнеси точки от левого борта до правого.", this);
        }

        /// <summary>
        /// Считает axis-aligned bounding box объекта по всем Renderer'ам в детях и переводит его
        /// в локальные координаты pivot'а. В отличие от простого Bounds.center/extents, это учитывает
        /// смещение pivot от геометрического центра модели — иначе автоматические точки "нос/корма/борта"
        /// строятся вокруг неправильной точки и объект переворачивается неестественно.
        /// </summary>
        private bool TryComputeLocalBounds(out Vector3 localCenter, out Vector3 localExtents)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                localCenter = Vector3.zero;
                localExtents = Vector3.zero;
                return false;
            }

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                worldBounds.Encapsulate(renderers[i].bounds);

            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;

            Vector3 localMin = transform.InverseTransformPoint(new Vector3(min.x, min.y, min.z));
            Vector3 localMax = localMin;

            for (int xi = 0; xi < 2; xi++)
            for (int yi = 0; yi < 2; yi++)
            for (int zi = 0; zi < 2; zi++)
            {
                Vector3 corner = new Vector3(xi == 0 ? min.x : max.x, yi == 0 ? min.y : max.y, zi == 0 ? min.z : max.z);
                Vector3 localCorner = transform.InverseTransformPoint(corner);
                localMin = Vector3.Min(localMin, localCorner);
                localMax = Vector3.Max(localMax, localCorner);
            }

            localCenter = (localMin + localMax) * 0.5f;
            localExtents = (localMax - localMin) * 0.5f;
            return true;
        }

        private void Update()
        {
            if (targetSurface == null)
            {
                if (debug && !_warnedNoSurface)
                {
                    Debug.LogWarning("[WaterFloater] Не назначен targetSurface (WaterSurface).", this);
                    _warnedNoSurface = true;
                }
                return;
            }

            if (_localPoints == null) BuildOrientationPoints();

            if (SampleWaterPoints())
                ApplyOrientation();
        }

        /// <summary>
        /// Опрашивает поверхность воды в каждой опорной точке.
        /// Использует предыдущий результат как стартовую точку поиска — так дешевле и стабильнее (см. документацию HDRP Water System).
        /// </summary>
        private bool SampleWaterPoints()
        {
            bool anySuccess = false;

            for (int i = 0; i < _localPoints.Length; i++)
            {
                Vector3 worldPoint = transform.TransformPoint(_localPoints[i]);

                _searchParams[i].startPositionWS = _searchResults[i].candidateLocationWS;
                _searchParams[i].targetPositionWS = worldPoint;
                _searchParams[i].error = searchError;
                _searchParams[i].maxIterations = searchMaxIterations;

                if (targetSurface.ProjectPointOnWaterSurface(_searchParams[i], out _searchResults[i]))
                {
                    _worldHeights[i] = _searchResults[i].projectedPositionWS;
                    anySuccess = true;
                }
                else if (debug)
                {
                    Debug.LogWarning($"[WaterFloater] Не удалось найти высоту воды для точки {i}.", this);
                }
            }

            return anySuccess;
        }

        /// <summary>
        /// Считает центр и нормаль по опорным точкам и плавно подгоняет под них позицию/поворот объекта.
        /// </summary>
        private void ApplyOrientation()
        {
            Vector3 center = Vector3.zero;
            for (int i = 0; i < _worldHeights.Length; i++) center += _worldHeights[i];
            center /= _worldHeights.Length;
            center.y += waterLineOffset;

            Vector3 normal;
            if (_worldHeights.Length >= 4)
            {
                // Точки 0/1 — условные "нос/корма", 2/3 — "лево/право"
                Vector3 forwardAxis = _worldHeights[0] - _worldHeights[1];
                Vector3 rightAxis = _worldHeights[3] - _worldHeights[2];
                normal = Vector3.Cross(forwardAxis, rightAxis).normalized;
            }
            else
            {
                Vector3 a = _worldHeights[1] - _worldHeights[0];
                Vector3 b = _worldHeights[2] - _worldHeights[0];
                normal = Vector3.Cross(a, b).normalized;
            }

            if (normal.sqrMagnitude < 0.0001f) normal = Vector3.up;
            if (Vector3.Dot(normal, Vector3.up) < 0f) normal = -normal;

            // Сохраняем текущий "курс", просто проецируем его на новую нормаль
            Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, normal);
            if (projectedForward.sqrMagnitude < 0.0001f) projectedForward = Vector3.ProjectOnPlane(transform.up, normal);

            Quaternion targetRotation = Quaternion.LookRotation(projectedForward, normal);

            float posT = positionSmoothing <= 0f ? 1f : 1f - Mathf.Exp(-positionSmoothing * Time.deltaTime);
            float rotT = rotationSmoothing <= 0f ? 1f : 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, center, posT);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotT);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            Gizmos.color = Color.cyan;

            if (orientationPoints != null && orientationPoints.Length >= 3)
            {
                foreach (var p in orientationPoints)
                    if (p != null) Gizmos.DrawSphere(p.position, 0.1f);
            }
            else if (autoDetectBoundsFromRenderers && TryComputeLocalBounds(out Vector3 localCenter, out Vector3 localExtents))
            {
                Vector3[] pts =
                {
                    transform.TransformPoint(localCenter + new Vector3(0f, 0f, localExtents.z)),
                    transform.TransformPoint(localCenter + new Vector3(0f, 0f, -localExtents.z)),
                    transform.TransformPoint(localCenter + new Vector3(-localExtents.x, 0f, 0f)),
                    transform.TransformPoint(localCenter + new Vector3(localExtents.x, 0f, 0f))
                };
                foreach (var p in pts) Gizmos.DrawSphere(p, 0.1f);
                Gizmos.DrawLine(pts[0], pts[1]);
                Gizmos.DrawLine(pts[2], pts[3]);
            }
            else
            {
                float hx = boundsSize.x * 0.5f;
                float hz = boundsSize.y * 0.5f;
                Vector3[] pts =
                {
                    transform.TransformPoint(new Vector3(0f, 0f, hz)),
                    transform.TransformPoint(new Vector3(0f, 0f, -hz)),
                    transform.TransformPoint(new Vector3(-hx, 0f, 0f)),
                    transform.TransformPoint(new Vector3(hx, 0f, 0f))
                };
                foreach (var p in pts) Gizmos.DrawSphere(p, 0.1f);
                Gizmos.DrawLine(pts[0], pts[1]);
                Gizmos.DrawLine(pts[2], pts[3]);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (searchMaxIterations < 1) searchMaxIterations = 1;
            if (searchError < 0f) searchError = 0f;
        }
#endif
    }
}