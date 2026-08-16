using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class OceanHelper : MonoBehaviour
{
    public WaterSurface OCEAN;
    public float duration;
    
    public void ChangeWaterFlow(float newAnge)
    {
        StartCoroutine(slowlyChangedFlow(newAnge));
        
    }
    public void ChangeWaterSpeed(float newSpeed)
    {
        StartCoroutine(slowlyChangedSpeed(newSpeed));
    }

    IEnumerator slowlyChangedFlow(float newAngle)
    {
        
        float oldAngle = OCEAN.largeOrientationValue;
        float time = 0;
        while (time < duration)
        {
            yield return null;
            time += Time.deltaTime;
            float t = time / duration;
            OCEAN.largeOrientationValue = Mathf.LerpAngle(oldAngle,newAngle,t);
        }

        OCEAN.largeOrientationValue = newAngle;
    }
    IEnumerator slowlyChangedSpeed(float newSpeed)
    {
        float oldSpeed = OCEAN.largeCurrentSpeedValue;
        
        float time = 0;
        while (time < duration)
        {
            yield return new WaitForSeconds(Time.deltaTime);
            time += Time.deltaTime;
            float t = time / duration;
            OCEAN.largeCurrentSpeedValue = Mathf.SmoothStep(oldSpeed, newSpeed, t);
        }

        OCEAN.largeCurrentSpeedValue = newSpeed;
    }
}
