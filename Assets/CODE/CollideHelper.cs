using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.PlayerLoop;

public class CollideHelper : MonoBehaviour
{
    public UnityEvent OnEnter;

    private void OnTriggerEnter(Collider other)
    {
        
        OnEnter?.Invoke();
    }

}
