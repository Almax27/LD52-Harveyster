using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackDamageArea : MonoBehaviour
{
    GameObject attacker;
    float damage;
    float duration;

    HashSet<GameObject> objectsToIgnore = new HashSet<GameObject>();

    bool isAttacking = false;

    private void Start()
    {
        //gameObject.SetActive(false);
    }

    public void Attack(GameObject attacker, float damage, float duration)
    {
        if (isAttacking) return;

        //gameObject.SetActive(true);
        isAttacking = true;

        objectsToIgnore.Clear();
        objectsToIgnore.Add(attacker);

        this.attacker = attacker;
        this.damage = damage;
        this.duration = duration;
        
        StartCoroutine(DoAttack());
    }

    IEnumerator DoAttack()
    {
        yield return new WaitForSeconds(duration);
        //gameObject.SetActive(false);
        isAttacking = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ProcessCollision(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        ProcessCollision(collision);
    }

    void ProcessCollision(Collider2D collision)
    {
        if (!isAttacking)
            return;

        if (objectsToIgnore.Contains(collision.gameObject))
            return;

        if (collision.gameObject.CompareTag("Plant"))
        {
            collision.GetComponent<WorldPlant>()?.Harvest();
            return;
        }

        objectsToIgnore.Add(collision.gameObject);

        Vector2 knockback = collision.transform.position - this.transform.position;
        knockback.Normalize();
        knockback *= 10.0f;

        float stunDuration = 0.5f;

        collision.gameObject.SendMessageUpwards("OnDamage", new Damage(damage, attacker, null, knockback, stunDuration), SendMessageOptions.DontRequireReceiver);

    }
}
