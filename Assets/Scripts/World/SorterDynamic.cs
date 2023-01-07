using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SorterDynamic : MonoBehaviour
{
    new SpriteRenderer[] renderers;

    private void Start()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        foreach(var renderer in renderers)
        {
            renderer.sortingOrder = (int)(-transform.position.y * 1000);
        }
    }
}
