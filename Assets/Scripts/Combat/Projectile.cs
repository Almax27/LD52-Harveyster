using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float launchSpeed = 10f;
    public float maxDistance = 10;
    public LayerMask hitMask;
    public FAFAudioSFXSetup hitSFX;
    public FAFAudioSFXSetup noDamageHitSFX;
    public GameObject[] spawnOnHit = new GameObject[0];


    GameObject owningGameObject = null;
    GameObjectPool.PooledGameObject poolEntry;

    Coroutine autoCleanupCoroutine = null;

    Vector2 velocity = Vector2.zero;
    float radius;

    public static Projectile Spawn(Projectile prefab, GameObject owner, Vector2 pos, Vector2 direction)
    {
        var pooledGO = GameObjectPool.Instance.Spawn(prefab.gameObject, pos);
        var projectile = pooledGO.Instance.GetComponent<Projectile>();
        projectile.poolEntry = pooledGO;
        projectile.owningGameObject = owner;

        projectile.velocity = direction.normalized * projectile.launchSpeed;

        return projectile;
    }

    private void OnEnable()
    {
        if (autoCleanupCoroutine != null) StopCoroutine(autoCleanupCoroutine);
        autoCleanupCoroutine = StartCoroutine(AutoCleanup());

        radius = GetComponent<CircleCollider2D>().radius;
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

    private void Update()
    {
        Vector2 pos = transform.position;
        Vector2 deltaStep = velocity * Time.deltaTime;
        float distance = deltaStep.magnitude;
        Vector2 newPos = pos + deltaStep;

        Damage damage = new Damage(1, owningGameObject, hitSFX, velocity, 0.2f);

        RaycastHit2D[] hits = Physics2D.CircleCastAll(pos, radius, deltaStep / distance, distance, hitMask.value);
        foreach(var hit in hits)
        {
            if (hit.rigidbody && hit.rigidbody.gameObject == owningGameObject)
                continue;

            if (hit.collider.gameObject.CompareTag("Plant"))
            {
                hit.collider.GetComponent<WorldPlant>()?.Harvest();
            }

            if (hit.collider.isTrigger)
                continue;

            Health health = hit.collider.GetComponentInParent<Health>();
            if (health)
            {
                health.TakeDamage(damage);
                if(damage.consumed)
                {
                    Cleanup();
                    SpawnOnHitObjects();
                    break;
                }
            }
            else //consider terminating object
            {
                noDamageHitSFX?.Play(transform.position);
                SpawnOnHitObjects();
                Cleanup();
            }
        }

        transform.position = newPos;
    }

    void SpawnOnHitObjects()
    {
        foreach(var gobj in spawnOnHit)
        {
            if(gobj)
            {
                GameObjectPool.Instance.Spawn(gobj, transform.position, transform.rotation).AutoDestruct();
            }
        }
    }
}
