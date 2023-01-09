using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    public SpriteRenderer body;
    public Rigidbody2D rigidbody2d;
    public Animator animator;
    public Health health;

    public float animMaxSpeed = 10;

    protected Vector2 desiredVelocity;
    protected Vector2 lookDirection;
    protected bool isLookingRight;
    float stunnedUntilTime;

    protected virtual void Awake()
    {
        if (!body) body = GetComponentInChildren<SpriteRenderer>();
        if (!rigidbody2d) rigidbody2d = GetComponentInChildren<Rigidbody2D>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!health) health = GetComponentInChildren<Health>();
    }

    protected virtual void Start()
    {

    }

    protected virtual void Update()
    {
        if(animator)
        {
            float speed = rigidbody2d.velocity.magnitude;
            animator.SetBool("isMoving", speed > 0.1f);
            animator.SetFloat("moveSpeed", speed / animMaxSpeed);
        }

        var mapBounds = GameManager.Instance.GetMapBounds(0.5f, 0.5f, 0.5f, 0.0f);
        transform.position = mapBounds.Clamp(transform.position);

        if (IsStunned()) desiredVelocity = Vector2.zero;

        rigidbody2d.velocity = MathExtension.VInterpTo(rigidbody2d.velocity, desiredVelocity, Time.deltaTime, GetVelocityInterpSpeed());

        UpdateFacing();
    }

    protected float GetVelocityInterpSpeed()
    {
        return IsStunned() ? 15.0f : 20.0f;
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

    protected virtual void OnDamage(Damage damage)
    {
        if (rigidbody2d)
        {
            desiredVelocity = rigidbody2d.velocity = damage.knockbackVelocity;
        }

        StunFor(damage.stunSeconds);
    }

    protected virtual void OnDeath()
    {
        //enabled = false;
        animator?.SetTrigger("OnDeath");
    }

    public bool IsStunned()
    {
        return Time.time < stunnedUntilTime;
    }

    public void StunFor(float duration)
    {
        stunnedUntilTime = Time.time + duration;
    }

}
