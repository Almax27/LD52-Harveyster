using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
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
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif

        foreach(var renderer in renderers)
        {
            renderer.sortingOrder = (int)(-transform.position.y * 1000);
        }
    }
}
