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
        Passive,
        Defend,
        Harvest,
        GameOver,
        Win
    }

    public Vector2 MapSize = Vector2.one;
    public Image blackoutImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI toastText;
    public TextMeshProUGUI winText;
    public StaminaUI staminaUI;
    public WorldPlanter planter;
    public WorldPrompt worldPrompt;
    public Light2D globalLight;

    [Header("Player Stats")]
    public PlayerStat Score = new PlayerStat(0);
    public PlayerStat HighScore = new PlayerStat(0);
    public PlayerStat Money = new PlayerStat(0);
    public PlayerStat AttackLevel = new PlayerStat(3);
    public PlayerStat Lives = new PlayerStat(3);
    public PlayerStat Stamina = new PlayerStat(3);
    public PlayerStat StaminaLevel = new PlayerStat(3);
    public int[] staminaLevels = { 3, 4, 5 };
    public float staminaRegenPerSecond = 0.5f;

    [Header("Enemies")]
    public List<RoundConfig> Rounds = new List<RoundConfig>();
    public int RoundIndex { get; private set; }
    List<EnemyBehaviour> activeEnemies = new List<EnemyBehaviour>();
    List<Transform> enemySpawnLocations = new List<Transform>();

    [Header("Music")]
    public MusicSetup PassiveMusic;
    public MusicSetup DefendMusic;
    public MusicSetup HarvestMusic;
    public FAFAudioSFXSetup playerDeathSFX;
    public FAFAudioSFXSetup gameOverSFX;
    public FAFAudioSFXSetup CollectSFX;


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
    Coroutine spawnEnemiesCoroutine;
    Coroutine planterCoroutine;
    GameState _state = GameState.Passive;

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

                UpdateHighScore();

                //Clear up any enemies
                foreach (var enemy in activeEnemies)
                {
                    Destroy(enemy.gameObject);
                }
                activeEnemies.Clear();
                if (spawnEnemiesCoroutine != null) StopCoroutine(spawnEnemiesCoroutine);

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
        if (Application.isEditor)
        {
            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                CurrentPlayer?.SendMessage("OnDamage", new Damage(100, gameObject));
            }
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                if (State == GameState.Passive) State = GameState.Defend;
                else if (State == GameState.Defend) State = GameState.Harvest;
                else if (State == GameState.Harvest)
                {
                    if (RoundIndex < Rounds.Count - 1)
                    {
                        RoundIndex++;
                        State = GameState.Passive;
                    }
                    else
                    {
                        State = GameState.Win;
                    }
                }
            }
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
        toastText.CrossFadeAlpha(0, 0, true);
        winText.CrossFadeAlpha(0, 0, true);

        objectiveText.text = "ObjectiveText";

        HighScore.Current = PlayerPrefs.GetInt("HighScore");
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
            State = GameState.Defend;
        }
    }

    IEnumerator RunSpawnEnemiesForRound(RoundConfig round)
    {
        if (round.EnemyTypes.Count > 0 && enemySpawnLocations.Count > 0)
        {
            int spawned = 0;

            enemySpawnLocations.Shuffle();

            while (spawned < round.NumEnemies)
            {
                var enemyPrefab = round.EnemyTypes[Random.Range(0, round.EnemyTypes.Count)];
                SpawnEnemy(enemyPrefab, enemySpawnLocations[spawned % enemySpawnLocations.Count].position);
                spawned++;
                yield return new WaitForSeconds(Random.Range(1.0f, 2.0f));
            }
        }
    }

    bool FindEnemySpawnLocation(ref Vector3 location)
    {
        if (enemySpawnLocations.Count > 0)
        {
            ContactFilter2D filter2D = new ContactFilter2D();
            filter2D.useTriggers = false;
            filter2D.SetLayerMask(LayerMask.GetMask(new string[]{"Default","Character"}));
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
        yield return null; //wait one frame

        while (true)
        {
            RoundConfig roundConfig = RoundIndex < Rounds.Count ? Rounds[RoundIndex] : null;

            switch (State)
            {
                case GameState.Passive:

                    FAFAudio.Instance.TryPlayMusic(PassiveMusic, false);

                    //kill all the plants
                    if (planterCoroutine != null) StopCoroutine(planterCoroutine);
                    planterCoroutine = StartCoroutine(planter.KillAllPlants());

                    //Wait for player to ring the bell
                    if (RoundIndex == 0)
                    {
                        objectiveText.text = "A mysterious bell calls in the harvest...";
                        yield return FadeBlackout(new Color(0, 0, 0, 0), 5.0f);
                        titleText.CrossFadeAlpha(0, 2, true);
                    }
                    else
                    {
                        objectiveText.text = "Purchase upgrades, then ring the bell...";
                    }

                    while(true) { yield return null; }
                    
                    break;
                case GameState.Defend:

                    if (planterCoroutine != null) StopCoroutine(planterCoroutine);
                    planterCoroutine = StartCoroutine(planter.GrowOut());

                    FAFAudio.Instance.TryPlayMusic(DefendMusic, false);

                    spawnEnemiesCoroutine = StartCoroutine(RunSpawnEnemiesForRound(roundConfig));

                    if(RoundIndex == 0) ShowToast("Attack with 'J' or left-click\nEvade with 'K' or right-click\nInteract with 'E'");

                    while(activeEnemies.Count > 0)
                    {
                        objectiveText.text = "Defend the Harvest! " + activeEnemies.Count + "/" + roundConfig.NumEnemies;
                        yield return null;
                    }
                    State = GameState.Harvest;
                    break;
                case GameState.Harvest:

                    if (RoundIndex == 0) ShowToast("Reap the harvest and earn money to spend on upgrades!");

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

                    if(RoundIndex < Rounds.Count - 1)
                    {
                        RoundIndex++;
                        State = GameState.Passive;
                    }
                    else
                    {
                        State = GameState.Win;
                    }

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
                            if(CurrentPlayer.gameObject) Destroy(CurrentPlayer.gameObject);
                            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                        }
                        yield return null;
                    }
                    break;
                case GameState.Win:
                    CurrentPlayer.enabled = false;
                    objectiveText.text = "Harvey is a happy scarecrow :)";
                    winText.CrossFadeAlpha(1, 2, true);
                    yield return new WaitForSeconds(1.0f);
                    while (true)
                    {
                        if (Input.anyKeyDown)
                        {
                            yield return FadeBlackout(new Color(0, 0, 0, 1), 2.0f);
                            if (CurrentPlayer.gameObject) Destroy(CurrentPlayer.gameObject);
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
                    playerDeathSFX?.Play(CurrentPlayer.transform.position);
                    yield return RunSpawnPlayer();
                }
                else
                {
                    gameOverSFX?.Play(CurrentPlayer.transform.position);
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

    Coroutine showToastRoutine;
    public void ShowToast(string message, float delay = 0)
    {
        if (showToastRoutine != null) StopCoroutine(showToastRoutine);
        showToastRoutine = StartCoroutine(RunShowToast(message, delay));
    }

    IEnumerator RunShowToast(string message, float delay)
    {
        if (toastText)
        {
            toastText.text = message;
            yield return new WaitForSeconds(delay);
            toastText.CrossFadeAlpha(1, 0.5f, true);
            yield return new WaitForSeconds(10);
            toastText.CrossFadeAlpha(0, 0.5f, true);
        }
    }

    float lastMoneyCollectedTime = 0;
    float moneyPitch = 1;
    public void MoneyCollected(Money money)
    {
        float timeSinceLastCollected = Time.time - lastMoneyCollectedTime;
        if (timeSinceLastCollected < 1.0f)
        {
            moneyPitch = Mathf.Min(2.0f, moneyPitch + 0.01f);
        }
        else
        {
            moneyPitch = 1;
        }
        if (timeSinceLastCollected > 0.05f)
        {
            lastMoneyCollectedTime = Time.time;
            CollectSFX?.Play(money.transform.position, 1.0f, moneyPitch);
        }
    }

    void UpdateHighScore()
    {
        if (Score.Current > HighScore.Current)
        {
            PlayerPrefs.SetInt("HighScore", Score.Current);
            PlayerPrefs.Save();
        }
    }
}

//Helper for abstract static access
class GameManager : GameManager<LD52GameManager> { }