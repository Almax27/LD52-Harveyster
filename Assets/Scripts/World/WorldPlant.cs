using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldPlant : MonoBehaviour
{
    [SerializeField]
    public PlantConfig currentConfig;

    SpriteRenderer spriteRenderer;
    bool wasHarvested = false;

    private void Awake()
    {
        ApplyConfig(currentConfig);
    }

    public void ApplyConfig(PlantConfig config)
    {
        currentConfig = config;

        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer)
        {
            spriteRenderer.sprite = config.sprite;
            spriteRenderer.color = config.color;
        }
    }

    public void Regrow()
    {
        if (wasHarvested)
        {
            wasHarvested = false;
            SpawnParticle(currentConfig.ParticleOnRegrow);
            gameObject.SetActive(true);
        }
    }


    public void TryHarvest()
    {
        if (!wasHarvested)
        {
            wasHarvested = true;
            SpawnParticle(currentConfig.ParticleOnHarvest);
            gameObject.SetActive(false);
        }
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
