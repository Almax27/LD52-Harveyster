using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
    Light2D light;
    float baseLightIntensity;

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
    public float HeavyAttackInputTime = 0.4f;
    public float SpinAttackInputTime = 1.0f;
    public float AttackSpinStaminaPerSecond = 1.0f;
    public float AttackStamiaRegenDelay = 1.0f;
    float attackSpinTick = 0;

    [Header("Audio")]
    public FAFAudioSFXSetup footstepSFX;
    public float footstepDistance = 0.5f;
    public FAFAudioSFXSetup lightAttackSFX;
    public FAFAudioSFXSetup heavyAttackSFX;
    public FAFAudioSFXSetup evadeSFX;
    public AudioSource spinAudio;

    [Header("State")]
    bool isMovingRight;
    bool isLookingRight;
    bool isEvading;
    bool isAttacking;
    int attackIndex;
    int attackCount;
    float distanceWalked;

    private void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        playerCollider2D = GetComponent<Collider2D>();
        if(!bodyAnimator) bodyAnimator = GetComponentInChildren<Animator>();
        //if (!attackAnimator) attackAnimator = GetComponentInChildren<Animator>();
        if (!meleeAttackArea) meleeAttackArea = GetComponentInChildren<AttackDamageArea>();
        if (!plantPusher) plantPusher = GetComponentInChildren<WorldPlantPusher>();

        light = GetComponentInChildren<Light2D>();
        if (light) light.intensity = 0;

        if (attack) attack.enabled = false;

        Health.IgnoreDamageFor(1.0f);

        facingVector = Vector2.left;
    }

    private void OnDisable()
    {
        rigidbody2D.velocity = Vector2.zero;
    }

    private void Update()
    {
        if (GameManager.Instance.IsPaused)
            return;

        desiredVelocity = rigidbody2D.velocity;

        if(Health.IsAlive) UpdateInput();

        UpdateMovement();

        if (Health.IsAlive)
        {
            if (light)
            {
                light.intensity = Mathf.Min(light.intensity + Time.deltaTime, 0.5f);
            }

            UpdateEvading();
            UpdateAttacking();

            UpdateFacing();

            //Update anims before physics to capture physic stops (move to func)
            if (bodyAnimator)
            {
                bodyAnimator.SetBool("isMoving", CanMove() && rigidbody2D.velocity.magnitude > 0.1);
                bodyAnimator.SetFloat("moveSpeed", Mathf.Max(0.3f, rigidbody2D.velocity.magnitude / MaxSpeed));

                bodyAnimator.SetBool("isEvading", isEvading);
            }
        }
        else
        {
            if(light)
            {
                light.intensity = Mathf.Max(light.intensity - Time.deltaTime * 2, 0);
            }
        }

        var mapBounds = GameManager.Instance.GetMapBounds(0.5f, 0.5f, 0.5f, 0.0f);
        if ((desiredVelocity.x < 0 && transform.position.x < mapBounds.xMin) || (desiredVelocity.x > 0 && transform.position.x > mapBounds.xMax))
        {
            if (isAttacking && attackIndex == 2) desiredVelocity.x = -desiredVelocity.x;
        }
        if ((desiredVelocity.y < 0 && transform.position.y < mapBounds.yMin) || (desiredVelocity.y > 0 && transform.position.y > mapBounds.yMax))
        {
            if (isAttacking && attackIndex == 2) desiredVelocity.y = -desiredVelocity.y;
        }
        transform.position = mapBounds.Clamp(transform.position);

        rigidbody2D.velocity = desiredVelocity;

        if (plantPusher)
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

    protected virtual void OnDamage(Damage damage)
    {
        if (rigidbody2D)
        {
            desiredVelocity = rigidbody2D.velocity = damage.knockbackVelocity;
        }
    }

    void OnDeath()
    {
        bodyAnimator.SetTrigger("onDeath");
        attack.enabled = false;
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
            //facingVector = (worldPosition - (Vector2)transform.position).normalized;
        }

        bool wantsStaminaUse = false;

        if(Input.GetButtonDown("Evade"))
        {
            evadeInputTime = Time.time;
            wantsStaminaUse = true;
        }

        if (Input.GetButton("Attack") || Input.GetAxis("Attack") > 0.0f)
        {
            if (!isAttacking && !waitingOnAttackRelease)
            {
                attackInputTime = Time.time;
                waitingOnAttackRelease = true;
                wantsStaminaUse = true;

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
                wantsStaminaUse = true;
            }
        }
        else
        {
            waitingOnAttackRelease = false;
        }

        if(wantsStaminaUse && GameManager.Instance.Stamina.Current == 0)
        {
            GameManager.Instance.staminaUI.Flash();
            LD52GameManager.Instance.StopStaminaRegen(0.5f);
        }
    }

    void ClearInput()
    {
        inputVector = Vector2.zero;
        attackInputTime = 0;
    }

    bool CanMove()
    {
        return Health.IsAlive && !isEvading && (!isAttacking || attackIndex == 2);
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
                distanceWalked += rigidbody2D.velocity.magnitude * Time.deltaTime;
                if(distanceWalked > footstepDistance)
                {
                    distanceWalked = 0;
                    footstepSFX?.Play(transform.position);
                }
                desiredVelocity = Vector2.SmoothDamp(desiredVelocity, moveVector * desiredSpeed, ref acceleration, MoveAccelerationTime, float.MaxValue, Time.deltaTime);
            }
            else //Decelerate to rest
            {
                desiredVelocity = Vector2.SmoothDamp(desiredVelocity, moveVector * desiredSpeed, ref acceleration, 0.05f, float.MaxValue, Time.deltaTime);
            }
        }
        else //Decelerate to rest (soft)
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
        return Time.time - lastEvadeTime > (EvadeDuration + 0.1f) && LD52GameManager.Instance.Stamina.Current > 0;
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
                Health?.IgnoreDamageFor(EvadeDuration);

                LD52GameManager.Instance.Stamina.Current--;
                LD52GameManager.Instance.StopStaminaRegen(AttackStamiaRegenDelay);

                evadeSFX?.Play(transform.position);
            }

            evadeVector = Vector3.RotateTowards(evadeVector, facingVector, EvadeMaxTurnSpeed * Mathf.Deg2Rad * Time.deltaTime, 0.5f);

            float evadeSpeed = EvadeDistance / EvadeDuration;
            desiredVelocity = evadeVector * evadeSpeed;
            acceleration = Vector2.zero;

            evadeTimeRemaining -= Time.deltaTime;
            if (evadeTimeRemaining <= 0.1f) //0.1s pause at the end of an evade
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
        return !isEvading && Time.time - lastAttackTime > 0.3f && LD52GameManager.Instance.Stamina.Current > 0;
    }

    void UpdateAttacking()
    {
        bool wantsAttack = waitingOnAttackRelease && attackInputTime > 0 && Time.time - attackInputTime < 0.3f;
        if ((isAttacking && !isEvading)|| (wantsAttack && CanAttack()))
        {
            bool canMove = false;
            bool canAim = false;

            if (!isAttacking)
            {
                lastAttackTime = Time.time;
                attackCount++;
                attackIndex = 0;
                isAttacking = true;
                attackSpinTick = 0;
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
                LD52GameManager.Instance.StopStaminaRegen();

                canAim = true;
                int attackLevel = GameManager.Instance.AttackLevel.Current;

                if (attackIndex == 0 && Time.time - attackInputTime > HeavyAttackInputTime && attackLevel > 1)
                {
                    attackAnimator.SetTrigger("powerUpAttack");
                    attackIndex++;
                }
                else if (attackIndex == 1 && Time.time - attackInputTime > SpinAttackInputTime && attackLevel > 2)
                {
                    attackAnimator.SetTrigger("powerUpAttack");
                    attackIndex++;

                    LD52GameManager.Instance.Stamina.Current--;
                }
                else if (attackIndex == 2) //spin attack
                {
                    if (spinAudio && !spinAudio.isPlaying)
                    {
                        spinAudio.Play();
                    }
                    canMove = true;
                    canAim = false;

                    attack.flipY = false;
                    attackAnimator.transform.rotation = Quaternion.identity;

                    spinAttackArea.Attack(this.gameObject, 1, 0.2f);

                    attackSpinTick += Time.deltaTime;
                    if (attackSpinTick > AttackSpinStaminaPerSecond)
                    {
                        if (LD52GameManager.Instance.Stamina.Current == 0)
                        {
                            waitingOnAttackRelease = false;
                        }

                        attackSpinTick -= AttackSpinStaminaPerSecond;
                        LD52GameManager.Instance.Stamina.Current--;
                    }
                }
            }
            else
            {
                if (attackIndex >= 0)
                {
                    LD52GameManager.Instance.StopStaminaRegen(AttackStamiaRegenDelay);

                    if (attackIndex == 0) //do light attack
                    {
                        lightAttackSFX?.Play(transform.position);
                        attackAnimator.SetTrigger("execAttack");
                        desiredVelocity = attackVector * 5.0f;
                        meleeAttackArea.Attack(this.gameObject, 1, 0.2f);
                        LD52GameManager.Instance.Stamina.Current--;
                    }
                    else if (attackIndex == 1) //do heavy attack
                    {
                        heavyAttackSFX?.Play(transform.position);
                        attackAnimator.SetTrigger("execAttack");
                        desiredVelocity = attackVector * 35.0f;
                        meleeAttackArea.Attack(this.gameObject, 3, 0.2f);
                        LD52GameManager.Instance.Stamina.Current -= 2;
                    }
                    else if (attackIndex == 2)//end spin attack
                    {
                        if (spinAudio && spinAudio.isPlaying)
                        {
                            spinAudio.Stop();
                        }
                        attackAnimator.SetTrigger("finishAttack");
                    }

                    attackIndex = -1;
                }

                if (attackAnimator.GetCurrentAnimatorStateInfo(0).IsName("NONE"))
                {
                    isAttacking = false;
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
            if (spinAudio && spinAudio.isPlaying)
            {
                spinAudio.Stop();
            }
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
