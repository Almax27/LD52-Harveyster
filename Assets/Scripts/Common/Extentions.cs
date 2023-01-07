using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector2Extension
{
    public static Vector2 Rotate(this Vector2 v, float degrees)
    {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

        float tx = v.x;
        float ty = v.y;
        v.x = (cos * tx) - (sin * ty);
        v.y = (sin * tx) + (cos * ty);
        return v;
    }
}

public static class RectExtension
{
    public static Vector2 Clamp(this Rect r, Vector2 pos)
    {
        return Vector2.Min(Vector2.Max(pos, r.min), r.max);
    }

    public static Vector3 Clamp(this Rect r, Vector3 pos)
    {
        Vector2 clamped2D = Clamp(r, (Vector2)pos);
        return new Vector3(clamped2D.x, clamped2D.y, pos.z);
    }
}

public static class MathExtension
{
    public static float FInterpTo(float Current, float Target, float DeltaTime, float InterpSpeed)
    {
        // If no interp speed, jump to target value
        if (InterpSpeed <= 0.0f)
        {
            return Target;
        }

        // Distance to reach
        float Dist = Target - Current;

        // If distance is too small, just set the desired location
        if (Dist*Dist < float.Epsilon)
        {
            return Target;
        }

        // Delta Move, Clamp so we do not over shoot.
        float DeltaMove = Dist * Mathf.Clamp(DeltaTime * InterpSpeed, 0.0f, 1.0f);

        return Current + DeltaMove;
    }

    public static Vector2 Vector2FromAngle(float radian)
    {
        return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
    }
}


