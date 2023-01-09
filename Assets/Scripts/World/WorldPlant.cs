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
        this.cutSprites = new Sprite[0];
        this.shrubSprites = new Sprite[0];
        this.ripeSprites = new Sprite[0];
        this.deadSprites = new Sprite[0];
        this.color = Color.white;
        this.value = 1;
        this.ParticleOnRegrow = null;
        this.ParticleOnHarvest = null;
    }

    public float weight;
    public Sprite[] cutSprites;
    public Sprite[] shrubSprites;
    public Sprite[] ripeSprites;
    public Sprite[] deadSprites;
    public Color color;
    public int value;
    public ParticleSystem ParticleOnRegrow;
    public ParticleSystem ParticleOnHarvest;

    public Sprite GetSprite(PlantState state)
    {
        Sprite[] sprites = null;
        switch (state)
        {
            case PlantState.Cut:
                sprites = cutSprites; break;
            case PlantState.Shrub:
                sprites = shrubSprites; break;
            case PlantState.Ripe:
                sprites = ripeSprites; break;
            case PlantState.Dead:
                sprites = deadSprites; break;
        }
        if(sprites != null && sprites.Length > 0)
        {
            return sprites[Random.Range(0, sprites.Length)];
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

        if(Random.value < 0.1f) SpawnParticle(currentConfig.ParticleOnRegrow);
    }

    public void Harvest()
    {
        if (state != PlantState.Cut)
        {
            state = PlantState.Cut;
            spriteRenderer.sprite = currentConfig.GetSprite(state);

            SpawnParticle(currentConfig.ParticleOnHarvest, true);

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
        if (state != PlantState.Cut)
        { 
            state = PlantState.Dead;
            spriteRenderer.sprite = currentConfig.GetSprite(state);
            if (collider2d) collider2d.enabled = true;
        }
    }

    void SpawnParticle(ParticleSystem prefab, bool overrideColor = false)
    {
        var pooledObject = GameObjectPool.Instance.Spawn(prefab.gameObject, transform.position);
        pooledObject.AutoDestruct();

        var system = pooledObject.Instance.GetComponent<ParticleSystem>();
        if (system)
        {
            var main = system.main;
            if(overrideColor) main.startColor = currentConfig.color;
        }
    }


}
