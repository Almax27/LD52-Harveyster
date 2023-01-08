using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class PlayerCharacter : MonoBehaviour
{
    public Health Health { get { if (!_health) { _health = GetComponent<Health>(); } return _health; } }
    Health _health;


}
