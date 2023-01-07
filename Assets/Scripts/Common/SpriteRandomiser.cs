using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RandomSpriteConfig
{
    public float weight = 1;
    public Sprite sprite;
    public Color color = Color.white;
}

[ExecuteInEditMode]
public class SpriteRandomiser : MonoBehaviour
{
    public List<RandomSpriteConfig> Sprites;

    // Start is called before the first frame update
    void Start()
    {
        float totalWeight = 0;
        foreach(var config in Sprites)
        {
            totalWeight += config.weight;
        }

        var spriteRenderer = GetComponent<SpriteRenderer>();
        if(spriteRenderer)
        {
            float val = Random.value;
            float accWeight = 0;
            foreach (var config in Sprites)
            {
                accWeight += config.weight;
                if (totalWeight <= 0 || val < accWeight / totalWeight)
                {
                    spriteRenderer.sprite = config.sprite;
                    spriteRenderer.color = config.color;
                    break;
                }
            }            
        }
    }
}
