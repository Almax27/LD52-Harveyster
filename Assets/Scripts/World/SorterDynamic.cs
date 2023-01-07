using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SorterDynamic : MonoBehaviour
{
    new SpriteRenderer[] renderers;

    private void Start()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.sortingOrder = (int)(-transform.position.y * 1000);
        }
        enabled = Application.isPlaying; //disable in editor mode
    }

    void Update()
    {
        foreach(var renderer in renderers)
        {
            renderer.sortingOrder = (int)(-transform.position.y * 1000);
        }
    }
}
