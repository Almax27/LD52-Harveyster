using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Torch : MonoBehaviour
{
    Light2D light;

    private void Start()
    {
        light = GetComponentInChildren<Light2D>();
        GetComponent<Animator>().SetFloat("Speed", Random.Range(0.8f, 1.2f));
    }

    private void Update()
    {
    
    }
}
