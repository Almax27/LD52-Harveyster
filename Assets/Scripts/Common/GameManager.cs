using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class GameManager<T> : SingletonBehaviour<T> where T : MonoBehaviour
{
    private bool _isPaused = false;
    public bool IsPaused 
    { 
        get { return _isPaused; } 
        set { if (_isPaused != value) _isPaused = value; OnPaused(_isPaused); } 
    }

    public Transform playerSpawnPoint;

    public GameObject playerPrefabToSpawn;

    public MusicSetup gameMusic;

    public PlayerCharacter CurrentPlayer { get; set; }

    protected override void Start()
    {
        base.Start();

        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        StartCoroutine(RunSpawnPlayer(0.0f));

        var followCamera = GetComponentInChildren<FollowCamera>();
        if (CurrentPlayer && followCamera)
        {
            followCamera.SnapToTarget();
        }

        if (gameMusic != null)
        {
            FAFAudio.Instance.TryPlayMusic(gameMusic);
        }
    }

    protected virtual void OnPaused(bool paused)
    {
        
    }

    protected virtual IEnumerator RunSpawnPlayer(float delay = 1.0f)
    {
        var followCamera = GetComponentInChildren<FollowCamera>();

        if (CurrentPlayer)
        {
            Destroy(CurrentPlayer.gameObject);
        }

        if (followCamera)
        {
            followCamera.target = playerSpawnPoint;
            followCamera.constrainToTarget = false;
        }

        yield return new WaitForSeconds(delay);

        var playerGO = GameObject.Instantiate(playerPrefabToSpawn, GetPlayerSpawnLocation(), Quaternion.identity);
        CurrentPlayer = playerGO.GetComponent<PlayerCharacter>();

        if (followCamera)
        {
            followCamera.target = CurrentPlayer.transform;
            followCamera.constrainToTarget = true;
        }

        yield break;
    }

    virtual public Vector2 GetMapSize()
    {
        return Vector2.zero;
    }

    virtual public Vector2 GetMapPivot()
    {
        return new Vector2(0.5f, 0.5f);
    }

    public Rect GetMapBounds(float insetLeft, float insetRight, float insetTop, float insetBottom)
    {
        var mapSize = GetMapSize();
        var pivotOffset = GetMapPivot() * mapSize;
        return new Rect(insetLeft - pivotOffset.x, insetBottom - mapSize.y + pivotOffset.y, mapSize.x - insetRight * 2, mapSize.y - insetTop * 2);
    }

    public Rect GetMapBounds(Vector2 inset = default)
    {
        return GetMapBounds(inset.x, inset.x, inset.y, inset.y);
    }

    protected virtual Vector3 GetPlayerSpawnLocation()
    {
        Vector2 loc = Vector2.zero;
        if (playerSpawnPoint)
        {
            loc = playerSpawnPoint.position;
        }
        return loc;
    }
}