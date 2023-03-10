using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldText : MonoBehaviour
{
    public LD52GameManager.GameState VisibleInState;
    public int RoundIndex = 0;

    private void Start()
    {
        GameManager.Instance.StateChangedEvent.AddListener(GameStateChanged);
        GameStateChanged(GameManager.Instance.State);
    }

    void GameStateChanged(LD52GameManager.GameState state)
    {
        gameObject.SetActive(state == VisibleInState && GameManager.Instance.RoundIndex == RoundIndex);
    }


}
