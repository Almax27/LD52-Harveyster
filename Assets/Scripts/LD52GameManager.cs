using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LD52GameManager : GameManager
{
    public Vector2 MapSize = Vector2.one;


    public override Vector2 GetMapSize()
    {
        return MapSize;
    }
}
