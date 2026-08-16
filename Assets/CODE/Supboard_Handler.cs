using System;
using AwakeComponents.DebugUI;
using UnityEngine;
using UnityEngine.Splines;

namespace AwakeComponents.Veslo
{
    public class Supboard_handler : MonoBehaviour, IDebuggableComponent
    {
        [Header("Speed")] [SerializeField] private float currentSpeed = 0f;
        [SerializeField] private float targetSpeed = 0f;

        [Tooltip("1 = maximum speed, equal to SplineAnimate Duration.")] [SerializeField]
        private float maxSpeed = 1f;
        
        [SerializeField] [DebugUIField]  private float speedPerPress = 0.1f;
        [SerializeField] [DebugUIField] private float deceleration = 0.1f;
        [SerializeField] [DebugUIField]  private float smoothSpeed = 1f;

        [Header("Spline")] [SerializeField] private SplineAnimate splineAnimate;

        private bool reachedEnd;
        
        public void RenderDebugUI()
        {
            GUILayout.Label("VESLOOOO");
        }

        private void Update()
        {
            
            // Если уже достигли конца — полностью прекращаем движение
            if (reachedEnd)
                return;

            // Left and Right both perform the same stroke
            if (Input.GetKeyDown(KeyCode.LeftArrow) ||
                Input.GetKeyDown(KeyCode.RightArrow))
            {
                targetSpeed += speedPerPress;
            }

            // Gradually lose speed
            targetSpeed -= deceleration * Time.deltaTime;
            targetSpeed = Mathf.Clamp(targetSpeed, 0f, maxSpeed);

            // Smoothly approach target speed
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                smoothSpeed * Time.deltaTime
            );

            if (currentSpeed <= 0f)
                return;

            float duration = splineAnimate.Duration;

            if (duration <= 0f)
                return;

            float normalizedSpeed = currentSpeed / duration;

            splineAnimate.NormalizedTime +=
                normalizedSpeed * Time.deltaTime;

            // Проверяем конец spline
            if (splineAnimate.NormalizedTime >= 1f)
            {
                splineAnimate.NormalizedTime = 1f;

                currentSpeed = 0f;
                targetSpeed = 0f;

                reachedEnd = true;
            }
        }
    }
}