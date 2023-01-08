using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float launchSpeed = 10f;
    public FAFAudioSFXSetup hitSFX;

    GameObject owningGameObject = null;

    private void Start()
    {
        //TODO: recycle
        Destroy(gameObject, 3);
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
        if (collision.gameObject)
        {
            Vector2 knockbackVelocity = Vector2.zero;
            var rb = GetComponent<Rigidbody2D>();
            if (rb)
            {
                knockbackVelocity = rb.velocity.normalized * 10;
            }
            Damage damage = new Damage(1, owningGameObject, hitSFX, knockbackVelocity, 0.2f);
            collision.gameObject.SendMessageUpwards("OnDamage", damage, SendMessageOptions.DontRequireReceiver);
        }


        Destroy(this.gameObject);
    }

    public void Launch(GameObject owner, Vector2 position, Vector2 direction)
    {
        owningGameObject = owner;
        transform.position = position;
        var rb = GetComponent<Rigidbody2D>();
        if(rb)
        {
            rb.velocity = direction.normalized * launchSpeed;
        }
    }

}
