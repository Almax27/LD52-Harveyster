using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using UnityEngine.SceneManagement;

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

[System.Serializable]
public class RoundConfig
{
    public int NumEnemies = 1;
    public List<EnemyBehaviour> EnemyTypes = new List<EnemyBehaviour>();
}


public class LD52GameManager : GameManager<LD52GameManager>
{
    public enum GameState
    { 
        Intro,
        Passive,
        Defend,
        Harvest,
        GameOver
    }

    public Vector2 MapSize = Vector2.one;
    public Image blackoutImage;
    public TextMeshProUGUI objectiveText;
    public StaminaUI staminaUI;
    public WorldPlanter planter;
    public WorldPrompt worldPrompt;

    [Header("Player Stats")]
    public PlayerStat Lives = new PlayerStat(3);
    public PlayerStat Stamina = new PlayerStat(3);
    public int[] staminaLevels = { 3, 4, 5 };
    public float staminaRegenPerSecond = 0.5f;

    [Header("Enemies")]
    public List<RoundConfig> Rounds = new List<RoundConfig>();
    int roundIndex = 0;
    List<EnemyBehaviour> activeEnemies = new List<EnemyBehaviour>();
    List<Transform> enemySpawnLocations = new List<Transform>();

    int currentStaminaLevel = 0;
    float staminaRegenTick = 0;

    public void StopStaminaRegen(float duration = 0)
    {
        staminaRegenTick = -Mathf.Max(0, duration);
    }

    public int MaxStamina { get { return currentStaminaLevel < staminaLevels.Length ? staminaLevels[currentStaminaLevel] : 1; } }

    Coroutine gameLogicCoroutine;
    Coroutine spawnPlayerCoroutine;
    GameState _state = GameState.Intro;

    public GameState State 
    { 
        get { return _state; } 
        private set
        {
            if(_state != value)
            {
                _state = value;
                Debug.Log("GameState = " + _state);
                StateChangedEvent.Invoke(_state);
            }
        }
    }

    public UnityEvent<GameState> StateChangedEvent;

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
        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            CurrentPlayer.SendMessage("OnDamage", new Damage(100, gameObject));
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
        blackoutImage.enabled = true;

        objectiveText.text = "ObjectiveText";

        Stamina.Max = currentStaminaLevel < staminaLevels.Length ? staminaLevels[currentStaminaLevel] : 1;

        if (!staminaUI) staminaUI = GetComponentInChildren<StaminaUI>();

        foreach(var spawn in FindObjectsOfType<EnemySpawn>())
        {
            enemySpawnLocations.Add(spawn.transform);
        }        
    }

    void RebuildStaminaPips()
    {
        int numPips = MaxStamina;
    }

    public void OnBellRung()
    {
        if(State <= GameState.Passive)
        {
            State = GameState.Defend;
        }
    }

    IEnumerator RunSpawnEnemiesForRound(RoundConfig round)
    {
        if (round.EnemyTypes.Count > 0)
        {
            int spawned = 0;
            Vector3 location = Vector3.zero;

            while (spawned < round.NumEnemies)
            {
                var enemyPrefab = round.EnemyTypes[Random.Range(0, round.EnemyTypes.Count)];
                if(FindEnemySpawnLocation(ref location))
                {
                    SpawnEnemy(enemyPrefab, location);
                    spawned++;
                }
                yield return new WaitForSeconds(Random.Range(1.0f, 2.0f));
            }
        }
    }

    bool FindEnemySpawnLocation(ref Vector3 location)
    {
        if (enemySpawnLocations.Count > 0)
        {
            enemySpawnLocations.Shuffle();

            ContactFilter2D filter2D = new ContactFilter2D();
            filter2D.useTriggers = false;
            filter2D.SetLayerMask(LayerMask.GetMask("Default"));
            List<Collider2D> results = new List<Collider2D>();

            for (int i = 0;  i < enemySpawnLocations.Count; i++)
            {
                location = enemySpawnLocations[i].position;
                results.Clear();
                if (Physics2D.OverlapCircle(location, 1, filter2D, results) == 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    EnemyBehaviour SpawnEnemy(EnemyBehaviour prefab, Vector2 location)
    {
        if(prefab)
        {
            GameObject gobj = GameObject.Instantiate(prefab.gameObject, location, Quaternion.identity);
            EnemyBehaviour enemy = gobj.GetComponent<EnemyBehaviour>();
            activeEnemies.Add(enemy);
            enemy.health.DeathEvent.AddListener(() => { OnEnemyDeath(enemy); });
            return enemy;
        }
        return null;
    }

    void OnEnemyDeath(EnemyBehaviour enemy)
    {
        activeEnemies.Remove(enemy);
    }

    IEnumerator RunGameLogic()
    {
        while(true)
        {
            //Spawn player at shine?
            if (CurrentPlayer && !CurrentPlayer.Health.IsAlive && State != GameState.GameOver)
            {
                if (Lives.Current > 0)
                {
                    Lives.Current--;
                    Stamina.Current = Stamina.Max;
                    yield return RunSpawnPlayer();
                }
                else
                {
                    State = GameState.GameOver;
                }
            }

            RoundConfig roundConfig = roundIndex < Rounds.Count ? Rounds[roundIndex] : null;

            switch (State)
            {
                case GameState.Intro:
                    yield return FadeBlackout(new Color(0, 0, 0, 0), 3.0f);
                    break;
                case GameState.Passive:
                    //Wait for player to ring the bell
                    break;
                case GameState.Defend:
                    yield return StartCoroutine(RunSpawnEnemiesForRound(roundConfig));
                    while(activeEnemies.Count > 0)
                    {
                        yield return null;
                    }
                    State = GameState.Harvest;
                    break;
                case GameState.Harvest:
                    break;
                case GameState.GameOver:
                    yield return new WaitForSeconds(1.0f);
                    yield return FadeBlackout(new Color(0, 0, 0, 1), 2.0f);
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    break;
            }
            

            yield return null;
        }
    }

    IEnumerator FadeBlackout(Color target, float duration)
    {
        if(blackoutImage != null)
        {
            blackoutImage.enabled = true;
            float tick = 0;
            Color startingColor = blackoutImage.color;
            while (tick < duration)
            {
                tick += Time.unscaledDeltaTime;
                blackoutImage.color = Color.Lerp(startingColor, target, Mathf.Clamp01(tick / duration));
                yield return null;
            }
            blackoutImage.enabled = target.a > 0;
        }
        yield break;
    }
}

//Helper for abstract static access
class GameManager : GameManager<LD52GameManager> { }