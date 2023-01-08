using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class Health : MonoBehaviour {

    public float maxHealth = 10;
    public float startingHealth = 10;
    float currentHealth = 10;

    public GameObject[] spawnOnDamage = new GameObject[0];
    public FAFAudioSFXSetup damageSFX;
    public GameObject[] spawnOnDeath = new GameObject[0];
    public GameObject[] spawnOnDestroy = new GameObject[0];

    public Color damageTintColor = Color.red;
    public float damageTintDuration = 0.2f;

    public float destroyOnDeath = -1;

    [Header("Events")]
    public UnityEvent DeathEvent = new UnityEvent();

    bool isDead = false;

    float lastDamageTime = 0;
    bool isDamageTinted = false;

    float ignoreDamageTime = 0;

    public bool IsAlive { get { return !isDead; } }

    public float GetHealth() { return currentHealth; }

    public float TimeSinceLastDamage() { return lastDamageTime > 0 ? Time.time - lastDamageTime : -1; }

    public void IgnoreDamageFor(float duration)
    {
        ignoreDamageTime = Time.time + duration;
    }

    void Start()
    {
        Reset();
        currentHealth = Mathf.Clamp(startingHealth, 1, maxHealth);
    }

    void Reset()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    public void OnDamage(Damage damage)
    {
        OnDamage(damage, false);
    }

    public void OnDamage(Damage damage, bool isSilent)
    {
        if (damage.consumed) return;

        if (Time.time < ignoreDamageTime) return;

        currentHealth -= damage.value;

        if (!isSilent)
        {
            foreach (GameObject gobj in spawnOnDamage)
            {
                Instantiate(gobj, this.transform.position, this.transform.rotation);
            }

            damageSFX?.Play(transform.position);
        }

        if (!isDead && currentHealth <= 0)
        {
            currentHealth = 0;
            isDead = true;
            SendMessage("OnDeath");
        }

        lastDamageTime = Time.time;
    }

    void OnDeath()
    {
        foreach (GameObject gobj in spawnOnDeath)
        {
            Instantiate(gobj, this.transform.position, this.transform.rotation);
        }

        if (destroyOnDeath >= 0)
        {
            Destroy(gameObject, destroyOnDeath);
        }

        DeathEvent.Invoke();
    }

    private void OnDestroy()
    {
        if (isDead)
        {
            foreach (GameObject gobj in spawnOnDestroy)
            {
                Instantiate(gobj, this.transform.position, this.transform.rotation);
            }
        }
    }

    void Update()
    {
        bool doTint = lastDamageTime > 0 && Time.time - lastDamageTime < damageTintDuration;
        if(doTint != isDamageTinted)
        {
            isDamageTinted = doTint;
            foreach (var sprite in GetComponentsInChildren<SpriteRenderer>())
            {
                sprite.color = isDamageTinted ? damageTintColor : Color.white;
            }
        }
    }

    public void Heal(float value)
    {
        currentHealth = Mathf.Max(currentHealth + value, maxHealth);
    }

    public void Kill(bool silent = false, bool instant = false)
    {
        if(instant)
        {
            destroyOnDeath = 0;
        }

        if (silent)
        {
            currentHealth = 0;
            isDead = true;
            SendMessage("OnDeath");
        }
        else
        {
            OnDamage(new Damage(maxHealth, null));
        }
    }
}