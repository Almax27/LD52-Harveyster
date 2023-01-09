using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float launchSpeed = 10f;
    public float maxDistance = 10;
    public FAFAudioSFXSetup hitSFX;

    GameObject owningGameObject = null;
    GameObjectPool.PooledGameObject poolEntry;

    Coroutine autoCleanupCoroutine = null;

    public static Projectile Spawn(Projectile prefab, GameObject owner, Vector2 pos, Vector2 direction)
    {
        var pooledGO = GameObjectPool.Instance.Spawn(prefab.gameObject, pos);
        var projectile = pooledGO.Instance.GetComponent<Projectile>();
        projectile.poolEntry = pooledGO;
        projectile.owningGameObject = owner;

        var rb = projectile.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.velocity = direction.normalized * projectile.launchSpeed;
        }

        return projectile;
    }

    private void OnEnable()
    {
        if (autoCleanupCoroutine != null) StopCoroutine(autoCleanupCoroutine);
        autoCleanupCoroutine = StartCoroutine(AutoCleanup());
    }

    IEnumerator AutoCleanup()
    {
        yield return new WaitForSeconds(maxDistance / launchSpeed);
        Cleanup();
    }

    void Cleanup()
    {
        if (!GameObjectPool.Instance.Pool(poolEntry))
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {        
        if(collision.gameObject.CompareTag("Plant"))
        {
            collision.GetComponent<WorldPlant>()?.Harvest();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //ignore owner
        if (collision.gameObject == owningGameObject)
            return;

        if (collision.gameObject)
        {
            Vector2 knockbackVelocity = Vector2.zero;
            var rb = GetComponent<Rigidbody2D>();
            if (rb)
            {
                knockbackVelocity = rb.velocity.normalized * 5;
            }
            Damage damage = new Damage(1, owningGameObject, hitSFX, knockbackVelocity, 0.2f);
            collision.gameObject.SendMessageUpwards("OnDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
        Cleanup();
    }
}
