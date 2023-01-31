using UnityEngine;
using System.Collections.Generic;

public class Damage {

    public Damage(float damage, GameObject sender, FAFAudioSFXSetup hitSFXSetup = null, Vector2 knockbackVelocity = new Vector2(), float stunSeconds = 0)
    {
        value = damage;
        owner = sender;
        hitSFX = hitSFXSetup;
        this.knockbackVelocity = knockbackVelocity;
        this.stunSeconds = stunSeconds;

        hitObjects = new List<GameObject>();
    }
    public float value;
    public GameObject owner;
    public FAFAudioSFXSetup hitSFX;
    public Vector2 knockbackVelocity;
    public float stunSeconds;
    public bool consumed;

    public bool isSilent;

    public List<GameObject> hitObjects { get; private set; }
}
