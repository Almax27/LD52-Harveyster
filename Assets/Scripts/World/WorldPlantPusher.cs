using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldPlantPusher : MonoBehaviour
{
    public float Radius = 1.5f;

    [System.NonSerialized]
    public static List<WorldPlantPusher> ActiveInstances = new List<WorldPlantPusher>();
    
    private void OnEnable()
    {
        ActiveInstances.Add(this);
    }

    private void OnDisable()
    {
        ActiveInstances.Remove(this);
    }
}
