using System;
using System.Collections;
using UnityEngine;

public class AnimDelay : MonoBehaviour
{

    public float preDelay;
    public Animator anim;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(preDelay);
        anim.enabled = true;
    }
}
