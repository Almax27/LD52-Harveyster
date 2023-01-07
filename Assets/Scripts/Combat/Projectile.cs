using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float launchSpeed = 10f;


    private void Start()
    {
        //TODO: recycle
        Destroy(gameObject, 3);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {        
        if(collision.gameObject.CompareTag("Plant"))
        {
            collision.gameObject.SetActive(false);
            //Destroy(collision.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Destroy(this.gameObject);
    }

    public void Launch(Vector2 position, Vector2 direction)
    {
        transform.position = position;
        var rb = GetComponent<Rigidbody2D>();
        if(rb)
        {
            rb.velocity = direction.normalized * launchSpeed;
        }
    }

}
