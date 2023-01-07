using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class GameManager : SingletonBehaviour<GameManager>
{
    public bool isPaused = false;

    public Transform playerSpawnPoint;

    public PlayerCharacter playerPrefabToSpawn;

    public MusicSetup gameMusic;

    public PlayerCharacter CurrentPlayer { get; set; }

    protected override void Start()
    {
        base.Start();

        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        var playerGO = GameObject.Instantiate(playerPrefabToSpawn, GetPlayerSpawnLocation(), Quaternion.identity);
        CurrentPlayer = playerGO.GetComponent<PlayerCharacter>();

        var followCamera = GetComponentInChildren<FollowCamera>();
        if (followCamera)
        {
            followCamera.target = playerGO.transform;
            followCamera.SnapToTarget();
        }

        if (gameMusic != null)
        {
            FAFAudio.Instance.TryPlayMusic(gameMusic);
        }
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

    Vector3 GetPlayerSpawnLocation()
    {
        Vector2 loc = Vector2.zero;
        if (playerSpawnPoint)
        {
            loc = playerSpawnPoint.position;
        }
        return loc;
    }
}
