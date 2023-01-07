using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlantConfigConfig
{
    public float weight = 1;
    public GameObject prefab = null;
}

public class WorldPlanter : MonoBehaviour
{
    public int randomSeed = 0;
    public PlantConfigConfig[] plantConfigs;
    public float spacingMin = 0.4f;
    public float spacingMax = 0.6f;
    public float overlapCheckRadius = 0.5f;
    public LayerMask overlapCheckLayerMask;

    public int NumberOfProcessedPlants;

    [EasyButtons.Button]
    void Regenerate()
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
                    var plantConfig = plantConfigs[Random.Range(0, plantConfigs.Length - 1)];
                    GameObject gobj = GameObject.Instantiate(plantConfig.prefab, new Vector3(xPos, yTempPos, 0), Quaternion.identity, transform);
                }
            }
        }
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
