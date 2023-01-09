using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Money : MonoBehaviour
{
    public float startingSpeed = 3;
    public float acceleration = 10;

    GameObject prefab;
    float tick;
    Vector3 velocity;

    public void AutoPool(GameObject prefab)
    {
        this.prefab = prefab;
    }

    private void OnEnable()
    {
        tick = Random.Range(0.4f, 0.8f);
        velocity = Vector2.one.Rotate(Random.Range(0, 360)) * startingSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        tick -= Time.deltaTime;
        if(tick > 0f)
        {
            velocity.y -= 1 * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
        }
        else
        {
            Vector3 targetPos = GameManager.Instance.CurrentPlayer.transform.position + new Vector3(0, 0.5f);
            Vector3 targetDir = targetPos - transform.position;
            float targetDist = targetDir.magnitude;
            targetDir /= targetDist;

            //maintain speed, but force direction
            float speed = velocity.magnitude;
            speed = Mathf.Min(speed + acceleration * Time.deltaTime, targetDist / Time.deltaTime); //don't overshoot
            velocity = targetDir * speed;

            transform.position += velocity * Time.deltaTime;

            if (targetDist < 0.4f)
            {
                Collect();
            }
        }
    }

    void Collect()
    {
        GameManager.Instance.Money.Current++;
        GameManager.Instance.MoneyCollected(this);
        if (prefab)
        {
            GameObjectPool.Instance.Pool(new GameObjectPool.PooledGameObject(prefab, this.gameObject));
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
