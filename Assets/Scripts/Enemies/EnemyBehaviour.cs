using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    public SpriteRenderer body;
    public Rigidbody2D rigidbody2d;

    protected Vector2 lookDirection;
    bool isLookingRight;

    protected virtual void Start()
    {
        if (!body) body = GetComponentInChildren<SpriteRenderer>();
        if (!rigidbody2d) rigidbody2d = GetComponentInChildren<Rigidbody2D>();
    }

    protected void UpdateFacing()
    {
        if (Mathf.Abs(lookDirection.x) > 0.0001f)
        {
            isLookingRight = lookDirection.x > 0;
        }
        if (body)
        {
            body.flipX = !isLookingRight;

            var p = body.transform.localPosition;
            p.x = Mathf.Abs(p.x) * (isLookingRight ? 1 : -1);
            body.transform.localPosition = p;
        }
    }


}
