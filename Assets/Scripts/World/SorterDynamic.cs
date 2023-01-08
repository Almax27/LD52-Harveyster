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
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = (int)(-transform.position.y * 1000);
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        for(int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = (int)(-transform.position.y * 1000) + i;
        }
    }
}
