using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WorldPrompt : MonoBehaviour
{
    bool isShown = false;
    TextMeshProUGUI Text;

    private void Start()
    {
        Text = GetComponentInChildren<TextMeshProUGUI>();
        gameObject.SetActive(false);
    }

    public bool Show(Vector2 location, string message)
    {
        if (isShown) return false;

        transform.position = location;

        isShown = true;
        gameObject.SetActive(true);
        Text.text = message;

        return true;
    }

    public void Hide()
    {
        if (!isShown) return;

        isShown = false;
        gameObject.SetActive(false);
    }
}
