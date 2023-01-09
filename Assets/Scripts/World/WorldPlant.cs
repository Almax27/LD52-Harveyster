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

    public Money moneyPrefab;

    SpriteRenderer spriteRenderer;
    Collider2D collider2d;

    bool wasHarvested = false;

    public PlantState State { get; private set; }

    private void Awake()
    {
        State = PlantState.Cut;
        collider2d = GetComponent<Collider2D>();
        ApplyConfig(currentConfig);
    }

    public void ApplyConfig(PlantConfig config)
    {
        currentConfig = config;

        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer)
        {
            spriteRenderer.sprite = config.GetSprite(State);
            spriteRenderer.color = config.color;
        }

        if (collider2d) collider2d.enabled = State != PlantState.Cut;
    }

    public void Regrow()
    {
        State = PlantState.Shrub;
        spriteRenderer.sprite = currentConfig.GetSprite(State);
        if (collider2d) collider2d.enabled = true;
        transform.rotation = Quaternion.identity;

        if(Random.value < 0.1f) SpawnParticle(currentConfig.ParticleOnRegrow);
    }

    public void Harvest()
    {
        if (State != PlantState.Cut)
        {
            if(State == PlantState.Ripe)
            {
                for (int i = 0; i < currentConfig.value; i++)
                {
                    var pooledObj = GameObjectPool.Instance.Spawn(moneyPrefab.gameObject, transform.position);
                    pooledObj.Instance.GetComponent<Money>()?.AutoPool(moneyPrefab.gameObject);
                }
                GameManager.Instance.Score.Current += currentConfig.value * 5;
            }

            State = PlantState.Cut;
            spriteRenderer.sprite = currentConfig.GetSprite(State);

            SpawnParticle(currentConfig.ParticleOnHarvest, true);

            if (collider2d) collider2d.enabled = false;
            transform.rotation = Quaternion.Euler(0,0,Random.Range(-30,30));
        }
    }

    public void Ripen()
    {
        State = PlantState.Ripe;
        spriteRenderer.sprite = currentConfig.GetSprite(State);
        if (collider2d) collider2d.enabled = true;
    }

    public void Die()
    {
        if (State != PlantState.Cut)
        { 
            State = PlantState.Dead;
            spriteRenderer.sprite = currentConfig.GetSprite(State);
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
