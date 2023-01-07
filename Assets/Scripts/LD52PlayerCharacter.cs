using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LD52PlayerCharacter : PlayerCharacter
{
    //Components
    [Header("Components")]
    public SpriteRenderer body;
    public new Rigidbody2D rigidbody2D;
    public Collider2D playerCollider2D;
    public Animator bodyAnimator;

    //Input
    Vector2 inputVector;
    float attackInputTime = 0;
    Vector2 attackVector;
    bool waitingOnAttackRelease = false;

    [Header("Movement")]
    public float MaxSpeed = 1;
    public float MoveAccelerationTime = 0.1f;
    public float MoveStopTime = 0.05f;
    Vector2 lastPosition;
    Vector2 desiredVelocity;
    Vector2 acceleration;

    [Header("Attack")]
    public Projectile ProjectilePrefab;

    [Header("State")]
    bool isMovingRight;
    bool isLookingRight;

    private void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        playerCollider2D = GetComponent<Collider2D>();
        if(!bodyAnimator) bodyAnimator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (GameManager.Instance.isPaused)
            return;

        desiredVelocity = rigidbody2D.velocity;

        UpdateInput();
        UpdateMovement();
        UpdateAttacking();

        UpdateFacing();

        //Update anims before physics to capture physic stops (move to func)
        if (bodyAnimator)
        {
            bodyAnimator.SetBool("isMoving", rigidbody2D.velocity.magnitude > 0.1);
        }


        var mapBounds = GameManager.Instance.GetMapBounds(0.5f, 0.5f, 0.5f, 0.0f);
        if ((desiredVelocity.x < 0 && transform.position.x < mapBounds.xMin) || (desiredVelocity.x > 0 && transform.position.x > mapBounds.xMax))
        {
            desiredVelocity.x = 0;
        }
        if ((desiredVelocity.y < 0 && transform.position.y < mapBounds.yMin) || (desiredVelocity.y > 0 && transform.position.y > mapBounds.yMax))
        {
            desiredVelocity.y = 0;
        }
        rigidbody2D.velocity = desiredVelocity;

        

    }

    private void LateUpdate()
    {
        
    }

    void UpdateInput()
    {
        inputVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if(inputVector != Vector2.zero)
        {
            float magnitude = inputVector.magnitude;
            float deadZone = 0.3f;
            if (magnitude > deadZone)
            {
                inputVector = inputVector.normalized * Mathf.Min(inputVector.magnitude, 1);
            }
            else
            {
                inputVector = Vector2.zero;
            }
        }

        if (Input.GetButtonDown("Attack") || Input.GetAxis("Attack") > 0.0f)
        {
            if (!waitingOnAttackRelease)
            {
                attackInputTime = Time.time;
                if (inputVector != Vector2.zero)
                {
                    attackVector = inputVector;
                }

                Vector2 aimVector = new Vector2(Input.GetAxisRaw("AimX"), Input.GetAxisRaw("AimY"));
                if (aimVector.sqrMagnitude > 0.2f)
                {
                    attackVector = aimVector;
                }
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            attackInputTime = Time.time;

            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            attackVector = (worldPosition - transform.position).normalized;
        }
        else
        {
            waitingOnAttackRelease = false;
        }
    }

    void ClearInput()
    {
        inputVector = Vector2.zero;
        attackInputTime = 0;
    }

    bool CanMove()
    {
        return true;
    }

    void UpdateMovement()
    {
        if (CanMove())
        {
            float desiredSpeed = MaxSpeed;

            if (inputVector != Vector2.zero) //Accelerate with input
            {
                desiredVelocity = Vector2.SmoothDamp(desiredVelocity, inputVector * desiredSpeed, ref acceleration, MoveAccelerationTime, float.MaxValue, Time.deltaTime);
            }
            else //Decelerate to rest
            {
                desiredVelocity = Vector2.SmoothDamp(desiredVelocity, inputVector * desiredSpeed, ref acceleration, 0.05f, float.MaxValue, Time.deltaTime);
            }
        }
        else //Decelerate to reset (soft)
        {
            inputVector = Vector2.zero;
            desiredVelocity = Vector2.SmoothDamp(desiredVelocity, Vector2.zero, ref acceleration, 0.2f, float.MaxValue, Time.deltaTime);
        }
    }

    bool CanAttack()
    {
        return true;
    }

    void UpdateAttacking()
    {
        bool wantsAttack = attackInputTime > 0 && Time.time - attackInputTime < 0.3f;
        if (CanAttack() && wantsAttack)
        {
            attackInputTime = 0;
            waitingOnAttackRelease = true;
            //TODO: do attack

            GameObject gobj = GameObject.Instantiate(ProjectilePrefab.gameObject);
            gobj.GetComponent<Projectile>().Launch(transform.position + new Vector3(0, 0.5f, 0), attackVector);
            Physics2D.IgnoreCollision(playerCollider2D, gobj.GetComponent<Collider2D>(), true);
        }
    }

    bool CanLook()
    {
        return true;
    }

    void UpdateFacing()
    {
        if (Mathf.Abs(inputVector.x) > 0.0001f)
        {
            isMovingRight = inputVector.x > 0;
        }
        if (CanLook() && Mathf.Abs(inputVector.x) > 0.0001f)
        {
            isLookingRight = inputVector.x > 0;
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
