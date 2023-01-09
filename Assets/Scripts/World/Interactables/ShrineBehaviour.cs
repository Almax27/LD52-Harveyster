using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShrineLevels
{
    public int cost;
    public Sprite sprite;
}

public enum ShrineType
{
    Attack,
    Lives,
    Stamina
}


public class ShrineBehaviour : InteractableBehaviour
{
    public ShrineType type;
    public List<ShrineLevels> levels = new List<ShrineLevels>();
    public SpriteRenderer spriteRenderer;

    public FAFAudioSFXSetup purchaseSFX;
    public FAFAudioSFXSetup noMoneySFX;

    protected override void Start()
    {
        base.Start();

        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();

        GetPlayerStat().OnChanged.AddListener(LevelChanged);
        LevelChanged(GetPlayerStat().Current, GetPlayerStat().Max);
    }

    void LevelChanged(int current, int max)
    {
        if(current < levels.Count)
        {
            spriteRenderer.sprite = levels[current].sprite;
        }
    }

    int GetUpgradeCost()
    {
        int next = GetPlayerStat().Current + 1;
        return next < levels.Count ? levels[next].cost : 0;
    }

    protected override bool GetInteractInfo(ref string message)
    {
        if(base.GetInteractInfo(ref message))
        {
            if (!GetPlayerStat().IsFull)
            {
                int cost = GetUpgradeCost();
                message = "Purchase " + type + " \n<color=#E0D454>$" + cost + "</color>\n(E)";
            }
            else
            {
                message = "Max " + type;
            }
            return true;
        }
        return false;
    }

    protected override void OnInteract()
    {
        base.OnInteract();

        if (!GetPlayerStat().IsFull)
        {
            int cost = GetUpgradeCost();
            if (GameManager.Instance.Money.Current >= cost || (Application.isEditor && Input.GetKey(KeyCode.LeftShift)))
            {
                GameManager.Instance.Money.Current -= cost;
                GetPlayerStat().Current++;
                purchaseSFX?.Play(transform.position);
            }
            else
            {
                noMoneySFX?.Play(transform.position);
            }
        }
    }

    PlayerStat GetPlayerStat()
    {
        if (type == ShrineType.Attack)
        {
            return GameManager.Instance.AttackLevel;
        }
        else if (type == ShrineType.Lives)
        {
            return GameManager.Instance.Lives;
        }
        else if (type == ShrineType.Stamina)
        {
            return GameManager.Instance.StaminaLevel;
        }
        return null;
    }
}
