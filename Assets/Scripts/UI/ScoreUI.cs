using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    TextMeshProUGUI Text;

    // Start is called before the first frame update
    void Start()
    {
        Text = GetComponent<TextMeshProUGUI>();
        Text.text = "Score\n0";
        GameManager.Instance.Score.OnChanged.AddListener(OnScoreChanged);
        OnScoreChanged(GameManager.Instance.Score.Current, GameManager.Instance.Score.Max);
    }

    void OnScoreChanged(int cur, int max)
    {
        Text.text = "Score\n"+cur;
    }
}
