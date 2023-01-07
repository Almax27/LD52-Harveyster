using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowBehaviour : EnemyBehaviour
{
    public float flySpeed = 2;

    bool hasMoveTarget = false;
    Vector2 moveTargetLocation;

    protected override void Start()
    {
        base.Start();

        StartCoroutine(RunCrowLogic());
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

            yield return new WaitForSeconds(1.0f);
        }
    }

    IEnumerator CalculateNextLocationToFlyTo()
    {
        hasMoveTarget = false;
        moveTargetLocation = Vector2.zero;

        var player = GameManager.Instance.CurrentPlayer;
        if (player)
        {
            moveTargetLocation = player.transform.position;

            float distanceFromPlayer = 5;

            for(int i = 0; i < 16; i++)
            {
                float angle = i * (360.0f / 16);
                Vector2 pointOnCircle = MathExtension.Vector2FromAngle(Mathf.Deg2Rad * angle);
                pointOnCircle = moveTargetLocation + pointOnCircle * distanceFromPlayer;

                Debug.DrawLine(pointOnCircle, moveTargetLocation, Color.blue, 1);

                if (!Physics2D.Raycast(pointOnCircle, (moveTargetLocation - pointOnCircle).normalized, distanceFromPlayer, LayerMask.GetMask("Default")))
                {
                    hasMoveTarget = true;
                    Debug.DrawLine(pointOnCircle, moveTargetLocation, Color.blue, 1);
                    break;
                }
                else
                {
                    Debug.DrawLine(pointOnCircle, moveTargetLocation, Color.red, 1);
                }

                yield return null;
            }
        }
    }


    IEnumerator RunMove(Vector2 targetPosition)
    {
        while(true)
        {
            Vector2 targetVector = targetPosition - (Vector2)transform.position;
            float distSq = targetVector.sqrMagnitude;
            if (distSq < 0.1f)
            {
                break;
            }

            if (rigidbody2d)
            {
                lookDirection = targetVector.normalized;
                rigidbody2d.velocity = lookDirection * flySpeed;
            }
            UpdateFacing();

            Debug.DrawLine(transform.position, targetPosition, Color.white, 0, false);

            yield return null;
        }
        rigidbody2d.velocity = Vector2.zero;
    }
}
