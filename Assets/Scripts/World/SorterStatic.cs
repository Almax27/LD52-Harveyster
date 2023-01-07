using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SorterStatic : MonoBehaviour
{
    private void Start()
    {
        foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
        {
            renderer.sortingOrder = (int)(-transform.position.y * 1000);
        }
    }
}
