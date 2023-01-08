using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class InteractableBehaviour : MonoBehaviour
{
    bool isPlayerNear = false;
    bool isShowingPrompt = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var player = GameManager.Instance.CurrentPlayer;
        if (!collision.isTrigger && collision.gameObject == player.gameObject)
        {
            isPlayerNear = true;
            Debug.Log("Player Approached: " + gameObject.name);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var player = GameManager.Instance.CurrentPlayer;
        if (!collision.isTrigger && collision.gameObject == player.gameObject)
        {
            isPlayerNear = false;
            Debug.Log("Player Left: " + gameObject.name);
        }
    }


    // Start is called before the first frame update
    protected virtual void Start()
    {
        
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        string message = "";
        if(isPlayerNear && GetInteractInfo(ref message))
        {
            ShowPrompt(message);
        }
        else
        {
            HidePrompt();
        }

        if (isShowingPrompt && Input.GetButton("Interact"))
        {
            OnInteract();
        }
    }

    protected virtual void ShowPrompt(string message)
    {
        if (!isShowingPrompt)
        {
            isShowingPrompt = true;

            Debug.Log("Show prompt: " + message);
        }
    }

    protected virtual void HidePrompt()
    {
        if (isShowingPrompt)
        {
            isShowingPrompt = false;

            Debug.Log("Hide prompt");
        }
    }

    protected virtual bool GetInteractInfo(ref string message)
    {
        if (GameManager.Instance.CurrentPlayer && GameManager.Instance.CurrentPlayer.Health.IsAlive)
        {
            message = "TEST";
            return true;
        }
        return false;
    }

    protected virtual void OnInteract()
    { 
    }
}
