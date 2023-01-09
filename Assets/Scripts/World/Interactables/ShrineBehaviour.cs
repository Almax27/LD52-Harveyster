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

    protected override bool GetInteractInfo(ref string message)
    {
        if(base.GetInteractInfo(ref message))
        {
            if (GetPlayerStat().IsFull)
            {
                message = "Purchase " + type + " (E)";
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
            GetPlayerStat().Current++;
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
