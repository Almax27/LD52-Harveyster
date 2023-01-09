using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Money : MonoBehaviour
{
    GameObject prefab;
    float tick;
    Vector2 velocity;

    public void AutoPool(GameObject prefab)
    {
        this.prefab = prefab;
    }

    private void OnEnable()
    {
        tick = Random.Range(0.4f, 0.8f);
        velocity = Vector2.one.Rotate(Random.Range(0, 360)) * 3.0f;
    }

    // Update is called once per frame
    void Update()
    {
        tick -= Time.deltaTime;
        if(tick > 0f)
        {
            velocity.y -= 1 * Time.deltaTime;
            transform.position += (Vector3)velocity * Time.deltaTime;
        }
        else
        {
            Vector2 targetPos = GameManager.Instance.CurrentPlayer.transform.position + new Vector3(0, 0.5f);
            transform.position = MathExtension.VInterpTo(transform.position, targetPos, Time.deltaTime, 10.0f);
            if(Vector2.Distance(transform.position, targetPos) < 0.4f)
            {
                Collect();
            }
        }
    }

    void Collect()
    {
        GameManager.Instance.Money.Current++;
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
