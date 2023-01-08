using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldParalaxer : MonoBehaviour
{
    public float paralaxScale = 1.0f;

    // Update is called once per frame
    void Update()
    {
        Vector3 position = transform.position;
        Vector3 cameraPos = Camera.main.transform.position;
        position.x = cameraPos.x * paralaxScale;
        position.y = cameraPos.y * paralaxScale;
        transform.position = position;
    }
}
