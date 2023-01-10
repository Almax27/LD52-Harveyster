using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HighScoreUI : MonoBehaviour
{
    TextMeshProUGUI Text;

    // Start is called before the first frame update
    void Start()
    {
        Text = GetComponent<TextMeshProUGUI>();
        Text.text = "";
        GameManager.Instance.HighScore.OnChanged.AddListener(OnScoreChanged);
        OnScoreChanged(GameManager.Instance.HighScore.Current, GameManager.Instance.HighScore.Max);
    }

    void OnScoreChanged(int cur, int max)
    {
        Text.alpha = cur > 0 ? 1 : 0;
        Text.text = "(" + cur + ")";
    }
}
