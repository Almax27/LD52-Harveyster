using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PlantConfig
{
    public PlantConfig(float weight = 1)
    {
        this.weight = weight;
        this.sprite = null;
        this.color = Color.white;
        this.value = 1;
        this.ParticleOnRegrow = null;
        this.ParticleOnHarvest = null;
    }

    public float weight ;
    public Sprite sprite;
    public Color color;
    public int value;
    public ParticleSystem ParticleOnRegrow;
    public ParticleSystem ParticleOnHarvest;
}


public class WorldPlanter : MonoBehaviour
{
    public int randomSeed = 0;
    public WorldPlant worldPlantPrefab;
    public List<PlantConfig> plantConfigs;
    public float spacingMin = 0.4f;
    public float spacingMax = 0.6f;
    public float overlapCheckRadius = 0.5f;
    public LayerMask overlapCheckLayerMask;

    public int NumberOfProcessedPlants;

    [EasyButtons.Button]
    public void Regenerate()
    {
        Random.InitState(randomSeed);

        for (int i = this.transform.childCount; i > 0; --i)
        {
            //Note: Destroying an objects invalidates Unity's internal child array
            DestroyImmediate(this.transform.GetChild(0).gameObject);
        }

        Rect mapBounds = GameManager.Instance.GetMapBounds();

        float yPos = mapBounds.yMin - spacingMax;
        float yHalfRange = (spacingMax - spacingMin) * 0.5f;
        while (yPos < mapBounds.yMax)
        {
            yPos += spacingMin + yHalfRange;

            float xPos = mapBounds.xMin - spacingMax;
            while (xPos < mapBounds.xMax)
            {
                xPos += Random.Range(spacingMin, spacingMax);
                float yTempPos = yPos + Random.Range(-yHalfRange, yHalfRange);

                if (!Physics2D.OverlapCircle(new Vector2(xPos, yTempPos), overlapCheckRadius, overlapCheckLayerMask))
                {
                    GameObject gobj = GameObject.Instantiate(worldPlantPrefab.gameObject, new Vector3(xPos, yTempPos, 0), Quaternion.identity, transform);
                    gobj.GetComponent<WorldPlant>()?.ApplyConfig(GetRandomPlantConfig());
                }
            }
        }
    }

    PlantConfig GetRandomPlantConfig()
    {
        float totalWeight = 0;
        foreach (var config in plantConfigs)
        {
            totalWeight += config.weight;
        }

        float val = Random.value;
        float accWeight = 0;
        foreach (var config in plantConfigs)
        {
            accWeight += config.weight;
            if (totalWeight <= 0 || val < accWeight / totalWeight)
            {
                return config;
            }
        }

        return new PlantConfig(1);
    }

    private void LateUpdate()
    {
        NumberOfProcessedPlants = 0;
        for (int i = 0; i < WorldPlantPusher.ActiveInstances.Count; ++i)
        {
            WorldPlantPusher pusher = WorldPlantPusher.ActiveInstances[i];

            Collider2D[] plants = Physics2D.OverlapCircleAll(pusher.transform.position, pusher.Radius, LayerMask.GetMask("Plants"));

            foreach (var plant in plants)
            {
                float dist = Vector2.Distance(pusher.transform.position, plant.transform.position) / pusher.Radius;

                Quaternion currentRot = plant.transform.localRotation;
                Quaternion targetRot = Quaternion.Euler(0, 0, Mathf.Lerp(0, pusher.transform.position.x > plant.transform.position.x ? 30 : -30, 1.0f - dist));
                plant.transform.localRotation = Quaternion.RotateTowards(currentRot, targetRot, 180.0f * Time.deltaTime);

                NumberOfProcessedPlants++;
            }
        }
    }
}
