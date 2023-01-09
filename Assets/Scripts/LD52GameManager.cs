using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;


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
            int newValue = Max > 0 ? Mathf.Clamp(value, 0, Max) : value;
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

    public bool IsFull { get { return Current == Max; } }
}

[System.Serializable]
public class RoundConfig
{
    public int NumEnemies = 1;
    public List<EnemyBehaviour> EnemyTypes = new List<EnemyBehaviour>();
}

[System.Serializable]
public class LightingConfig
{
    public float intensity = 0.2f;
    public Color color = Color.white;
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
    public TextMeshProUGUI gameOverText;
    public StaminaUI staminaUI;
    public WorldPlanter planter;
    public WorldPrompt worldPrompt;
    public Light2D globalLight;

    [Header("Player Stats")]
    public PlayerStat Score = new PlayerStat(0);
    public PlayerStat Money = new PlayerStat(0);
    public PlayerStat AttackLevel = new PlayerStat(3);
    public PlayerStat Lives = new PlayerStat(3);
    public PlayerStat Stamina = new PlayerStat(3);
    public PlayerStat StaminaLevel = new PlayerStat(3);
    public int[] staminaLevels = { 3, 4, 5 };
    public float staminaRegenPerSecond = 0.5f;

    [Header("Enemies")]
    public List<RoundConfig> Rounds = new List<RoundConfig>();
    int roundIndex = 0;
    List<EnemyBehaviour> activeEnemies = new List<EnemyBehaviour>();
    List<Transform> enemySpawnLocations = new List<Transform>();

    [Header("Music")]
    public MusicSetup PassiveMusic;
    public MusicSetup DefendMusic;
    public MusicSetup HarvestMusic;

    [Header("Lighting")]
    public LightingConfig passiveLighting;
    public LightingConfig defendLighting;
    public LightingConfig harvestLighting;

    float staminaRegenTick = 0;

    public void StopStaminaRegen(float duration = 0)
    {
        staminaRegenTick = -Mathf.Max(0, duration);
    }

    public int MaxStamina { get { return StaminaLevel.Current < staminaLevels.Length ? staminaLevels[StaminaLevel.Current] : 1; } }

    Coroutine gameLogicCoroutine;
    Coroutine spawnPlayerCoroutine;
    Coroutine planterCoroutine;
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

                //Clear up any enemies
                foreach (var enemy in activeEnemies)
                {
                    Destroy(enemy.gameObject);
                }
                activeEnemies.Clear();

                //restart the game logic coroutine
                if (gameLogicCoroutine != null)
                {
                    StopCoroutine(gameLogicCoroutine);
                    gameLogicCoroutine = StartCoroutine(RunGameLogic());
                }
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
            StartCoroutine(RunCombatLogic());
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

        if(globalLight)
        {
            LightingConfig config = null;
            if (State <= GameState.Passive) config = passiveLighting;
            if (State == GameState.Defend) config = defendLighting;
            if (State == GameState.Harvest) config = harvestLighting;
            if (config != null)
            {
                globalLight.intensity = Mathf.Lerp(globalLight.intensity, config.intensity, Time.deltaTime);
                globalLight.color = Color.Lerp(globalLight.color, config.color, Time.deltaTime);
            }
        }
    }

    [EasyButtons.Button]
    public void Initialise()
    {
        blackoutImage.color = Color.black;
        blackoutImage.enabled = true;

        gameOverText.CrossFadeAlpha(0, 0 , true);

        objectiveText.text = "ObjectiveText";

        Money.Current = 0;
        AttackLevel.Current = 1;
        StaminaLevel.Current = 0;
        Stamina.Max = MaxStamina;

        StaminaLevel.OnChanged.AddListener((cur, max) => { Stamina.Max = MaxStamina; });

        if (!staminaUI) staminaUI = GetComponentInChildren<StaminaUI>();
        if (!planter) planter = FindObjectOfType<WorldPlanter>();

        foreach (var spawn in FindObjectsOfType<EnemySpawn>())
        {
            enemySpawnLocations.Add(spawn.transform);
        }        
    }

    public void OnBellRung(Vector2 pos)
    {
        if(State <= GameState.Passive)
        {
            if (planterCoroutine != null) StopCoroutine(planterCoroutine);
            planterCoroutine = StartCoroutine(planter.GrowOutFrom(pos));
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
        Score.Current += 100;
    }

    IEnumerator RunGameLogic()
    {
        while(true)
        {
            RoundConfig roundConfig = roundIndex < Rounds.Count ? Rounds[roundIndex] : null;

            switch (State)
            {
                case GameState.Intro:
                    objectiveText.text = "";
                    yield return FadeBlackout(new Color(0, 0, 0, 0), 3.0f);
                    State = GameState.Passive;
                    break;
                case GameState.Passive:

                    FAFAudio.Instance.TryPlayMusic(PassiveMusic, false);

                    //Wait for player to ring the bell
                    if (roundIndex == 0)
                    {
                        objectiveText.text = "A mysterious bell calls in the harvest...";
                    }
                    else
                    {
                        objectiveText.text = "Purchase upgrades, then ring the bell...";
                    }
                    
                    break;
                case GameState.Defend:

                    FAFAudio.Instance.TryPlayMusic(DefendMusic, false);

                    StartCoroutine(RunSpawnEnemiesForRound(roundConfig));

                    while(activeEnemies.Count > 0)
                    {
                        objectiveText.text = "Defend the Harvest! " + activeEnemies.Count + "/" + roundConfig.NumEnemies;
                        yield return null;
                    }
                    State = GameState.Harvest;
                    break;
                case GameState.Harvest:

                    FAFAudio.Instance.TryPlayMusic(HarvestMusic, false);

                    //Ripen all the plants
                    if (planterCoroutine != null) StopCoroutine(planterCoroutine);
                    planterCoroutine = StartCoroutine(planter.RipenAllPlants());

                    //Give the player time to harvest
                    float harvestTick = 0;
                    float harvestingTime = 10;
                    while(harvestTick < harvestingTime)
                    {
                        harvestTick += Time.deltaTime;
                        objectiveText.text = "Harvest time! " + (int)Mathf.Max(0,harvestingTime - harvestTick);
                        yield return null;
                    }

                    //kill all the plants
                    if (planterCoroutine != null) StopCoroutine(planterCoroutine);
                    planterCoroutine = StartCoroutine(planter.KillAllPlants());

                    State = GameState.Passive;

                    roundIndex = Mathf.Clamp(roundIndex + 1, 0, Rounds.Count - 1);

                    /*
                    //Reward player for uncut plants
                    int scoreAward = 0;
                    var allPlants = planter.AllPlants;
                    for (int i = 0; i < allPlants.Length; i++)
                    {
                        if(allPlants[i].State != PlantState.Cut)
                        {
                            scoreAward++;
                        }
                    }
                    Score.Current += scoreAward;
                    */

                    break;
                case GameState.GameOver:
                    objectiveText.text = "You ran out of lives :(";
                    gameOverText.CrossFadeAlpha(1.0f, 1.0f, true);
                    yield return new WaitForSeconds(1.0f);
                    while (true)
                    {
                        if (Input.anyKeyDown)
                        {
                            yield return FadeBlackout(new Color(0, 0, 0, 1), 2.0f);
                            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                        }
                        yield return null;
                    }
                    break;
            }
            

            yield return null;
        }
    }

    IEnumerator RunCombatLogic()
    {
        while (true)
        {
            //Spawn player at shine?
            if (CurrentPlayer && !CurrentPlayer.Health.IsAlive && State != GameState.GameOver)
            {
                yield return new WaitForSeconds(1.0f);
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