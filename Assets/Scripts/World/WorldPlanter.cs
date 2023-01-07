using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldPlanter : MonoBehaviour
{
    public GameObject CornPrefab;
    public float spacingMin = 0.4f;
    public float spacingMax = 0.6f;
    public float overlapCheckRadius = 0.5f;
    public LayerMask overlapCheckLayerMask;

    public int NumberOfProcessedPlants;

    [EasyButtons.Button]
    void Regenerate()
    {
        for (int i = this.transform.childCount; i > 0; --i)
        {
            //Note: Destroying an objects invalidates Unity's internal child array
            DestroyImmediate(this.transform.GetChild(0).gameObject);
        }

        Rect mapBounds = GameManager.Instance.GetMapBounds();

        float yPos = mapBounds.yMin - spacingMax;
        while (yPos < mapBounds.yMax)
        {
            yPos += Random.Range(spacingMin, spacingMax);

            float xPos = mapBounds.xMin - spacingMax;
            while (xPos < mapBounds.xMax)
            {
                xPos += Random.Range(spacingMin, spacingMax);

                if (!Physics2D.OverlapCircle(new Vector2(xPos, yPos), overlapCheckRadius, overlapCheckLayerMask))
                {
                    GameObject gobj = GameObject.Instantiate(CornPrefab, new Vector3(xPos, yPos, 0), Quaternion.identity, transform);
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