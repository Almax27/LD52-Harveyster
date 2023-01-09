using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowBehaviour : EnemyBehaviour
{
    public float flySpeed = 2;

    bool hasMoveTarget = false;
    Vector2 moveTargetLocation;
    Coroutine logicCoroutine;

    protected override void Start()
    {
        base.Start();

        logicCoroutine = StartCoroutine(RunCrowLogic());
    }

    protected override void Update()
    {
        base.Update();

        bool isMoving = rigidbody2d.velocity.magnitude > 0.1f;

        Vector3 currentBodyPos = body.transform.localPosition;
        float targetBodyOffsetY = isMoving || hasMoveTarget ? 1.0f : 0.0f;
        currentBodyPos.y = MathExtension.FInterpTo(currentBodyPos.y, targetBodyOffsetY, Time.deltaTime, 2.0f);
        body.transform.localPosition = currentBodyPos;

        bool isFlying = currentBodyPos.y > 0.1f;
        if(animator)
        {
            animator.SetBool("isFlying", isFlying);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //hasMoveTarget = false;
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        hasMoveTarget = false;
        StopCoroutine(logicCoroutine);
    }

    IEnumerator RunCrowLogic()
    {
        yield return new WaitForSeconds(2.0f);
        while(true)
        {
            yield return CalculateNextLocationToFlyTo();
            if (hasMoveTarget)
            {
                yield return RunMove(moveTargetLocation);
            }

            yield return new WaitForSeconds(Random.Range(3.0f, 5.0f));
        }
    }

    IEnumerator CalculateNextLocationToFlyTo()
    {
        hasMoveTarget = false;
        moveTargetLocation = Vector2.zero;

        var player = GameManager.Instance.CurrentPlayer;
        if (player)
        {
            Vector2 playerPos = player.transform.position;
            Vector2 aiPos = transform.position;
            float distanceFromPlayer = Random.Range(4,5);
            float minTravelDistanceSq = 2.0f * 2.0f;

            List<Vector2> validLocations = new List<Vector2>();

            int angles = 32;

            RaycastHit2D[] hits = new RaycastHit2D[1];
            for (int i = 0; i < angles; i++)
            {
                float angle = i * (360.0f / angles);
                Vector2 pointOnCircle = playerPos + MathExtension.Vector2FromAngle(Mathf.Deg2Rad * angle) * distanceFromPlayer;

                //Skip points that are too close
                if ((aiPos - pointOnCircle).sqrMagnitude < minTravelDistanceSq)
                {
                    DebugExtension.DebugPoint(hits[0].point, Color.grey, 0.1f, 1, false);
                    continue;
                }

                ContactFilter2D filter = new ContactFilter2D();
                filter.useTriggers = false;
                filter.SetLayerMask(LayerMask.GetMask("Default"));

                if (Physics2D.Raycast(pointOnCircle, (playerPos - pointOnCircle).normalized, filter, hits, distanceFromPlayer) == 0 ||
                    (hits[0].rigidbody && hits[0].rigidbody.gameObject == player.gameObject))
                {
                    validLocations.Add(pointOnCircle);
                    Debug.DrawLine(pointOnCircle, playerPos, Color.blue, 1);
                }
                else
                {
                    Debug.DrawLine(pointOnCircle, playerPos, Color.red, 1);
                    DebugExtension.DebugPoint(hits[0].point, Color.white, 0.1f, 1, false);

                }

                yield return null;
            }

            if(validLocations.Count > 0)
            {
                Vector2 pos = transform.position;

                //sort by distance to us
                validLocations.Sort((a, b) => { return (a - pos).sqrMagnitude < (b - pos).sqrMagnitude ? -1 : 1; });
                int maxIndex = Mathf.FloorToInt(validLocations.Count * 0.5f);

                hasMoveTarget = true;
                moveTargetLocation = validLocations[Random.Range(0, maxIndex)];

                DebugExtension.DebugPoint(moveTargetLocation, Color.white, 0.1f, 1, false);
                Debug.DrawLine(pos, playerPos, Color.green, 1);
            }
        }
    }

    bool CanMove()
    {
        return hasMoveTarget && !IsStunned();
    }

    IEnumerator RunMove(Vector2 targetPosition)
    {
        yield return new WaitForSeconds(0.5f);

        float startTime = Time.time;

        while(CanMove() && Time.time - startTime < 5.0f)
        {
            Vector2 targetVector = targetPosition - (Vector2)transform.position;
            float distSq = targetVector.sqrMagnitude;
            if (distSq < 0.1f)
            {
                desiredVelocity = Vector2.zero;
                break;
            }

            if (rigidbody2d)
            {
                lookDirection = targetVector.normalized;
                desiredVelocity = lookDirection * flySpeed;
            }

            Debug.DrawLine(transform.position, targetPosition, Color.white, 0, false);

            yield return null;
        }

        hasMoveTarget = false;

        yield return new WaitForSeconds(0.5f);
    }
}
