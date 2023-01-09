using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BellBehaviour : InteractableBehaviour
{
    protected override void OnInteract()
    {
        base.OnInteract();

        GameManager.Instance.OnBellRung(transform.position);
    }

    protected override bool GetInteractInfo(ref string message)
    {
        if(base.GetInteractInfo(ref message) && GameManager.Instance.State <= LD52GameManager.GameState.Passive)
        {
            message = "Ring Bell (E)";
            return true;
        }
        return false;
    }
}
