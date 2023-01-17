using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    public Sprite fullSprite;
    public Sprite emptySprite;
    public Sprite emptyFlashSprite;

    public Image pipPrefab;

    List<Image> pips = new List<Image>();
    Coroutine flashCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        LD52GameManager.Instance.Stamina.OnChanged.AddListener((cur,max)=>
        {
            if (flashCoroutine != null) { StopCoroutine(flashCoroutine); flashCoroutine = null; }
            StaminaChanged(cur, max);
        });

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

    public void Flash()
    {
        if (flashCoroutine == null)
        {
            flashCoroutine = StartCoroutine(RunFlash());
        }
    }

    IEnumerator RunFlash()
    {
        for (int i = 0; i < 2; i++)
        {
            foreach(var pip in pips)
            {
                pip.sprite = emptyFlashSprite;
            }
            yield return new WaitForSeconds(0.2f);
            Refresh();
            yield return new WaitForSeconds(0.2f);
        }
        flashCoroutine = null;
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
