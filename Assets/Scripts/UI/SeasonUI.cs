using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SeasonUI : MonoBehaviour
{
    TextMeshProUGUI Text;

    // Start is called before the first frame update
    void Start()
    {
        Text = GetComponent<TextMeshProUGUI>();
        GameManager.Instance.StateChangedEvent.AddListener(GameStateChanged);
        GameStateChanged(GameManager.Instance.State);
    }

    void GameStateChanged(LD52GameManager.GameState state)
    {
        Text.text = "Season " + (GameManager.Instance.RoundIndex + 1);
    }
}
