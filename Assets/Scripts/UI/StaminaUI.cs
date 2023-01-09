using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class StaminaUI : MonoBehaviour
{
    public Sprite fullSprite;
    public Sprite emptySprite;

    public Image pipPrefab;

    List<Image> pips = new List<Image>();

    // Start is called before the first frame update
    void Start()
    {
        LD52GameManager.Instance.Stamina.OnChanged.AddListener(StaminaChanged);

        for (int i = this.transform.childCount; i > 0; --i)
        {
            //Note: Destroying an objects invalidates Unity's internal child array
            DestroyImmediate(this.transform.GetChild(0).gameObject);
        }

        Refresh();
    }

    void Refresh()
    { 
        int max = LD52GameManager.Instance.MaxStamina;
        int current = Application.isPlaying ? LD52GameManager.Instance.Stamina.Current : max;
        StaminaChanged(current, max);
    }

    void StaminaChanged(int current, int max)
    {
        while (pips.Count < max)
        {
            GameObject gobj = GameObject.Instantiate(pipPrefab.gameObject, this.transform);
            pips.Add(gobj.GetComponent<Image>());
        }

        for(int i = 0; i < pips.Count; i++)
        {
            if(pips[i])
            {
                pips[i].name = "Pip_" + i.ToString("00");
                pips[i].sprite = current > i ? fullSprite : emptySprite;
                pips[i].enabled = i < max;
            }
        }
    }
}
