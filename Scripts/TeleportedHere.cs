using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;

// Add this script to specific TeleportPoints to be able to trigger an event whenever the Player teleports to it.

public class TeleportedHere : MonoBehaviour
{
    public UnityEvent onTeleportedHere;

    void Start()
    {
        Teleport.Player.AddListener(TeleportDone);
    }


    void TeleportDone(TeleportMarkerBase teleportMarkerBase)
    {
        onTeleportedHere.Invoke();
    }

}
