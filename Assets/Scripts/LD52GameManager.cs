using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

[System.Serializable]
public class PlayerStat
{
    int _current = 1;

    [SerializeField]
    int _max = 1;

    public UnityEvent<int, int> OnChanged;

    PlayerStat() { }
    public PlayerStat(int max = 1) { _current = _max = max; }

    public int Current
    {
        get { return _current; }
        set
        {
            int newValue = Mathf.Clamp(value, 0, Max);
            if (_current != newValue)
            {
                _current = newValue;
                OnChanged.Invoke(_current, Max);
            }
        }
    }

    public int Max
    {
        get { return _max; }
        set
        {
            if (_max != value)
            {
                _max = value;
                _current = Mathf.Min(_current, _max);
                OnChanged.Invoke(_current, _max);
            }
        }
    }
}

public class LD52GameManager : GameManager<LD52GameManager>
{
    public Vector2 MapSize = Vector2.one;
    public Image blackoutImage;
    public TextMeshProUGUI objectiveText;
    public StaminaUI staminaUI;

    [Header("Player Stats")]
    public PlayerStat Lives = new PlayerStat(3);
    public PlayerStat Stamina = new PlayerStat(3);
    public int[] staminaLevels = { 3, 4, 5 };
    public float staminaRegenPerSecond = 0.5f;

    int currentStaminaLevel = 0;
    float staminaRegenTick = 0;

    public void StopStaminaRegen(float duration = 0)
    {
        staminaRegenTick = -Mathf.Max(0, duration);
    }

    public int MaxStamina { get { return currentStaminaLevel < staminaLevels.Length ? staminaLevels[currentStaminaLevel] : 1; } }

    Coroutine gameLogicCoroutine;

    public override Vector2 GetMapSize()
    {
        return MapSize;
    }

    protected override void Start()
    {
        Initialise();
        
        base.Start();

        if (Application.isPlaying)
        {
            gameLogicCoroutine = StartCoroutine(RunGameLogic());
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            Stamina.Current++;
        }
        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            Stamina.Current--;
        }

        staminaRegenTick += Time.deltaTime;
        while(staminaRegenTick > staminaRegenPerSecond)
        {
            staminaRegenTick -= staminaRegenPerSecond;
            Stamina.Current++;
        }

    }

    [EasyButtons.Button]
    public void Initialise()
    {
        blackoutImage.color = Color.black;
        objectiveText.text = "ObjectiveText";

        Stamina.Max = currentStaminaLevel < staminaLevels.Length ? staminaLevels[currentStaminaLevel] : 1;

        if (!staminaUI) staminaUI = GetComponentInChildren<StaminaUI>();
    }

    void RebuildStaminaPips()
    {
        int numPips = MaxStamina;
        
    }

    IEnumerator RunGameLogic()
    {
        while(true)
        {
            yield return FadeBlackout(0, 2.0f);

            yield return null;
        }
    }

    IEnumerator FadeBlackout(float opacity, float duration)
    {
        if(blackoutImage != null)
        {
            float tick = 0;
            Color startingColor = blackoutImage.color;
            while (tick < duration)
            {
                tick += Time.unscaledDeltaTime;
                Color color = blackoutImage.color;
                color.a = Mathf.Lerp(startingColor.a, opacity, Mathf.Clamp01(tick / duration));
                blackoutImage.color = color;
                yield return null;
            }
        }
        yield break;
    }
}

//Helper for abstract static access
class GameManager : GameManager<LD52GameManager> { }