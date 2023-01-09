using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MoneyUI : MonoBehaviour
{
    TextMeshProUGUI Text;

    // Start is called before the first frame update
    void Start()
    {
        Text = GetComponent<TextMeshProUGUI>();
        Text.text = "$0";
        GameManager.Instance.Money.OnChanged.AddListener(OnMoneyChanged);
        OnMoneyChanged(GameManager.Instance.Money.Current, GameManager.Instance.Money.Max);
    }

    void OnMoneyChanged(int cur, int max)
    {
        Text.text = "$" + cur;
    }
}
