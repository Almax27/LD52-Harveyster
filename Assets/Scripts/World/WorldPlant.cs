using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum PlantState
{
    Cut,
    Shrub,
    Ripe,
    Dead
}

[System.Serializable]
public struct PlantConfig
{
    public PlantConfig(float weight = 1)
    {
        this.weight = weight;
        this.cutSprite = null;
        this.shrubSprite = null;
        this.ripeSprite = null;
        this.deadSprite = null;
        this.color = Color.white;
        this.value = 1;
        this.ParticleOnRegrow = null;
        this.ParticleOnHarvest = null;
    }

    public float weight;
    public Sprite cutSprite;
    public Sprite shrubSprite;
    public Sprite ripeSprite;
    public Sprite deadSprite;
    public Color color;
    public int value;
    public ParticleSystem ParticleOnRegrow;
    public ParticleSystem ParticleOnHarvest;

    public Sprite GetSprite(PlantState state)
    {
        switch (state)
        {
            case PlantState.Cut:
                return cutSprite;
            case PlantState.Shrub:
                return shrubSprite;
            case PlantState.Ripe:
                return ripeSprite;
            case PlantState.Dead:
                return deadSprite;
        }
        return null;
    }
}

public class WorldPlant : MonoBehaviour
{
    [SerializeField]
    public PlantConfig currentConfig;

    SpriteRenderer spriteRenderer;
    Collider2D collider2d;

    bool wasHarvested = false;

    PlantState state = PlantState.Cut;

    private void Awake()
    {
        collider2d = GetComponent<Collider2D>();
        ApplyConfig(currentConfig);
    }

    public void ApplyConfig(PlantConfig config)
    {
        currentConfig = config;

        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer)
        {
            spriteRenderer.sprite = config.GetSprite(state);
            spriteRenderer.color = config.color;
        }

        if (collider2d) collider2d.enabled = state != PlantState.Cut;
    }

    public void Regrow()
    {
        state = PlantState.Shrub;
        spriteRenderer.sprite = currentConfig.GetSprite(state);
        if (collider2d) collider2d.enabled = true;
    }

    public void Harvest()
    {
        if (state != PlantState.Cut)
        {
            state = PlantState.Cut;
            spriteRenderer.sprite = currentConfig.GetSprite(state);

            SpawnParticle(currentConfig.ParticleOnHarvest);

            if (collider2d) collider2d.enabled = false;
        }
    }

    public void Ripen()
    {
        state = PlantState.Ripe;
        spriteRenderer.sprite = currentConfig.GetSprite(state);
        if (collider2d) collider2d.enabled = true;
    }

    public void Die()
    {
        state = PlantState.Dead;
        spriteRenderer.sprite = currentConfig.GetSprite(state);
        if (collider2d) collider2d.enabled = true;
    }

    void SpawnParticle(ParticleSystem prefab)
    {
        var pooledObject = GameObjectPool.Instance.Spawn(prefab.gameObject, transform.position);
        pooledObject.AutoDestruct();

        var system = pooledObject.Instance.GetComponent<ParticleSystem>();
        if (system)
        {
            var main = system.main;
            main.startColor = currentConfig.color;
        }
    }


}
