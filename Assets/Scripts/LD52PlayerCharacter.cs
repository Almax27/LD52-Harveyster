using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LD52PlayerCharacter : PlayerCharacter
{
    //Components
    [Header("Components")]
    public SpriteRenderer body;
    public SpriteRenderer attack;
    public new Rigidbody2D rigidbody2D;
    public Collider2D playerCollider2D;
    public Animator bodyAnimator;
    public Animator attackAnimator;
    public AttackDamageArea meleeAttackArea;
    public AttackDamageArea spinAttackArea;
    public WorldPlantPusher plantPusher;

    //Input
    Vector2 inputVector;
    Vector2 facingVector;
    float attackInputTime = 0;
    float evadeInputTime = 0;
    Vector2 attackVector;
    Vector2 evadeVector;
    bool waitingOnAttackRelease = false;

    [Header("Movement")]
    public float MaxSpeed = 5;
    public float MoveAccelerationTime = 0.1f;
    public float MoveStopTime = 0.05f;
    public float EvadeDistance = 5;
    public float EvadeDuration = 0.3f;
    public float EvadeMaxTurnSpeed = 180;
    public float EvadeCooldown = 1.2f;
    Vector2 lastPosition;
    Vector2 desiredVelocity;
    Vector2 acceleration;
    float evadeTimeRemaining = 0;
    float lastEvadeTime = 0;
    float lastAttackTime = 0;

    [Header("Attack")]
    public Projectile ProjectilePrefab;

    [Header("State")]
    bool isMovingRight;
    bool isLookingRight;
    bool isEvading;
    bool isAttacking;
    int attackIndex;
    int attackCount;

    private void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        playerCollider2D = GetComponent<Collider2D>();
        if(!bodyAnimator) bodyAnimator = GetComponentInChildren<Animator>();
        //if (!attackAnimator) attackAnimator = GetComponentInChildren<Animator>();
        if (!meleeAttackArea) meleeAttackArea = GetComponentInChildren<AttackDamageArea>();
        if (!plantPusher) plantPusher = GetComponentInChildren<WorldPlantPusher>();
    }

    private void Update()
    {
        if (GameManager.Instance.isPaused)
            return;

        desiredVelocity = rigidbody2D.velocity;

        UpdateInput();
        UpdateMovement();
        UpdateEvading();
        UpdateAttacking();

        UpdateFacing();

        //Update anims before physics to capture physic stops (move to func)
        if (bodyAnimator)
        {
            bodyAnimator.SetBool("isMoving", CanMove() && rigidbody2D.velocity.magnitude > 0.1);
            bodyAnimator.SetFloat("moveSpeed", rigidbody2D.velocity.magnitude / MaxSpeed);

            bodyAnimator.SetBool("isEvading", isEvading);
        }


        var mapBounds = GameManager.Instance.GetMapBounds(0.5f, 0.5f, 0.5f, 0.0f);
        if ((desiredVelocity.x < 0 && transform.position.x < mapBounds.xMin) || (desiredVelocity.x > 0 && transform.position.x > mapBounds.xMax))
        {
            if (isAttacking && attackIndex == 2) desiredVelocity.x = -desiredVelocity.x;
            else desiredVelocity.x = 0;
        }
        if ((desiredVelocity.y < 0 && transform.position.y < mapBounds.yMin) || (desiredVelocity.y > 0 && transform.position.y > mapBounds.yMax))
        {
            if (isAttacking && attackIndex == 2) desiredVelocity.y = -desiredVelocity.y;
            else desiredVelocity.y = 0;
        }
        rigidbody2D.velocity = desiredVelocity;

        if(plantPusher)
        {
            if(isAttacking && attackIndex == 2)
            {
                plantPusher.Radius = 3.0f;
            }
            else
            {
                plantPusher.Radius = 1.0f;
            }
        }

    }

    private void LateUpdate()
    {
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(isAttacking && attackIndex == 2)
        {
            rigidbody2D.velocity = Vector2.Reflect(rigidbody2D.velocity, collision.GetContact(0).normal);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isAttacking && attackIndex == 2)
        {
            rigidbody2D.velocity += collision.GetContact(0).normal * 5;
        }
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
                facingVector = inputVector.normalized;
                inputVector = facingVector * Mathf.Min(inputVector.magnitude, 1);
            }
            else
            {
                inputVector = Vector2.zero;
            }
        }

        if(Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            Vector2 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            facingVector = (worldPosition - (Vector2)transform.position).normalized;
        }

        if(Input.GetButtonDown("Evade"))
        {
            evadeInputTime = Time.time;
        }

        if (Input.GetButton("Attack") || Input.GetAxis("Attack") > 0.0f)
        {
            if (!isAttacking && !waitingOnAttackRelease)
            {
                attackInputTime = Time.time;
                waitingOnAttackRelease = true;

                Vector2 aimVector = new Vector2(Input.GetAxisRaw("AimX"), Input.GetAxisRaw("AimY"));
                if (aimVector.sqrMagnitude > 0.2f)
                {
                    attackVector = aimVector;
                }
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (!isAttacking && !waitingOnAttackRelease)
            {
                attackInputTime = Time.time;
                waitingOnAttackRelease = true;
            }
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
        return !isEvading && (!isAttacking || attackIndex == 2);
    }

    void UpdateMovement()
    {
        if (CanMove())
        {
            float desiredSpeed = MaxSpeed;
            Vector2 moveVector = inputVector;

            if(isAttacking && attackIndex == 2) //spin attack
            {
                desiredSpeed *= 2.0f;
                moveVector = Vector3.RotateTowards(desiredVelocity.normalized, facingVector, 90 * Mathf.Deg2Rad * Time.deltaTime, 0.5f);
                desiredVelocity = Vector2.SmoothDamp(desiredVelocity, moveVector * desiredSpeed, ref acceleration, MoveAccelerationTime, float.MaxValue, Time.deltaTime);
            }
            else if (inputVector != Vector2.zero) //Accelerate with input
            {
                desiredVelocity = Vector2.SmoothDamp(desiredVelocity, moveVector * desiredSpeed, ref acceleration, MoveAccelerationTime, float.MaxValue, Time.deltaTime);
            }
            else //Decelerate to rest
            {
                desiredVelocity = Vector2.SmoothDamp(desiredVelocity, moveVector * desiredSpeed, ref acceleration, 0.05f, float.MaxValue, Time.deltaTime);
            }
        }
        else //Decelerate to reset (soft)
        {
            inputVector = Vector2.zero;
            desiredVelocity = Vector2.SmoothDamp(desiredVelocity, Vector2.zero, ref acceleration, 0.2f, float.MaxValue, Time.deltaTime);
        }
    }

    bool CanEvade()
    {
        if(isAttacking && waitingOnAttackRelease)
        {
            return false;
        }
        return Time.time - lastEvadeTime > EvadeCooldown;
    }

    void UpdateEvading()
    {
        bool wantsEvade = evadeInputTime > 0 && Time.time - evadeInputTime < 0.3f;
        if (isEvading || (wantsEvade && CanEvade()))
        {
            if(!isEvading)
            {
                isEvading = true;
                lastEvadeTime = Time.time;
                evadeVector = facingVector;
                evadeTimeRemaining = EvadeDuration;
            }

            evadeVector = Vector3.RotateTowards(evadeVector, facingVector, EvadeMaxTurnSpeed * Mathf.Deg2Rad * Time.deltaTime, 0.5f);

            float evadeSpeed = EvadeDistance / EvadeDuration;
            desiredVelocity = evadeVector * evadeSpeed;
            acceleration = Vector2.zero;

            evadeTimeRemaining -= Time.deltaTime;
            if (evadeTimeRemaining <= 0)
            {
                isEvading = false;
            }
        }
        else
        {
            isEvading = false;
        }
    }

    bool CanAttack()
    {
        return !isEvading && Time.time - lastAttackTime > 0.3f;
    }

    void UpdateAttacking()
    {
        bool wantsAttack = attackInputTime > 0 && Time.time - attackInputTime < 0.3f;
        if (isAttacking || (wantsAttack && CanAttack()))
        {
            bool canMove = false;
            bool canAim = false;

            if (!isAttacking)
            {
                lastAttackTime = Time.time;
                attackCount++;
                attackIndex = 0;
                isAttacking = true;
                attackAnimator.ResetTrigger("powerUpAttack");
                attackAnimator.ResetTrigger("finishAttack");
                attackAnimator.ResetTrigger("execAttack");
                attackAnimator.SetTrigger("startAttack");

                attackVector = facingVector;

                if (attack)
                {
                    attack.flipY = !attack.flipY;// attackCount % 2 == 0;//attackVector.x < 0;
                }
            }

            if (waitingOnAttackRelease)
            {
                canAim = true;
                if (attackIndex == 0 && Time.time - attackInputTime > 0.4f)
                {
                    attackAnimator.SetTrigger("powerUpAttack");
                    attackIndex++;
                }
                else if (attackIndex == 1 && Time.time - attackInputTime > 1.0f)
                {
                    attackAnimator.SetTrigger("powerUpAttack");
                    attackIndex++;
                }
                else if (attackIndex == 2) //spin attack
                {
                    canMove = true;
                    canAim = false;
                    spinAttackArea.Attack(this.gameObject, 1, 0.2f);
                }
            }
            else
            {
                if (attackIndex == 0) //do light attack
                {
                    attackAnimator.SetTrigger("execAttack");
                    desiredVelocity = attackVector * 5.0f;
                    meleeAttackArea.Attack(this.gameObject, 1, 0.2f);
                }
                else if (attackIndex == 1) //do heavy attack
                {
                    attackAnimator.SetTrigger("execAttack");
                    desiredVelocity = attackVector * 30.0f;
                    meleeAttackArea.Attack(this.gameObject, 2, 0.2f);
                }
                else if (attackIndex == 2)//end spin attack
                {
                    attackAnimator.SetTrigger("finishAttack");
                }

                attackIndex = -1;

                if (attackAnimator.GetCurrentAnimatorStateInfo(0).IsName("NONE"))
                {
                    isAttacking = false;
                    attackInputTime = 0;
                }
            }

            if (canAim)
            {
                attackVector = Vector3.RotateTowards(attackVector, facingVector, 720 * Mathf.Deg2Rad * Time.deltaTime, 0.5f);
                attackAnimator.transform.rotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.right, attackVector));                
            }

            if (!canMove)
            {
                desiredVelocity = MathExtension.VInterpTo(desiredVelocity, Vector2.zero, Time.deltaTime, 10.0f);
            }

            bodyAnimator.SetBool("isSpinning", isAttacking && attackIndex == 2 && waitingOnAttackRelease);

            //GameObject gobj = GameObject.Instantiate(ProjectilePrefab.gameObject);
            // gobj.GetComponent<Projectile>().Launch(this.gameObject, transform.position + new Vector3(0, 0.5f, 0), attackVector);
            //Physics2D.IgnoreCollision(playerCollider2D, gobj.GetComponent<Collider2D>(), true);
        }
        else if (isAttacking)
        {
            attackAnimator.SetTrigger("finishAttack");
            isAttacking = false;
        }

        //Hide sprite when not in use
        if(attack) attack.enabled = isAttacking;

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
        if (CanLook() && Mathf.Abs(facingVector.x) > 0.0001f)
        {
            isLookingRight = facingVector.x > 0;
        }
        if (body)
        {
            body.flipX = !isLookingRight;

            var p = body.transform.localPosition;
            p.x = Mathf.Abs(p.x) * (isLookingRight ? 1 : -1);
            body.transform.localPosition = p;
        }
    }

    void DealMeleeDamage()
    {

    }
}
