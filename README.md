# Unity-SteamVR-Terrain-Teleportation
Dynamically place SteamVR TeleportationPoints on Unity terrain.

## Youtube
This video shows the end result when using this code.

# Prerequisites
- Unity 2019
- SteamVR plugin for Unity (https://assetstore.unity.com/packages/tools/integration/steamvr-plugin-32647)

## Changes to SteamVR teleportation code to include teleportation events
Unfortunately the SteamVR code does not support extending the code easily so I had to change a few lines. I tried to keep it as simple as possible to make it easy to update their code after a version change. These are the code changes needed to make this work:

TeleportMarkerbase.cs, add:
''' 
using UnityEngine.Events;
'''
In the public abstract class TeleportMarkerBase add:
'''
public UnityEvent onTeleportToHere;
'''

Teleport.cs, add:
''' 
using UnityEngine.Events;
'''
In the public class Teleport add:
'''
public UnityEvent onTeleportSucceeded;
'''
In the method private void TeleportPlayer() add:
'''
            onTeleportSucceeded.Invoke();                       // Invoke the event attached to the global teleporting object
            teleportingToMarker.onTeleportToHere.Invoke();      // Invoke the event attached to the teleporting object (teleporting point, teleporting area)
'''
(Add this just before the code: Teleport.Player.Send( pointedAtTeleportMarker ); )

# How does it work?
- Tag your terrain with the "Terrain" label (which you may need to create first).
- Make the changes to the SteamVR code.
- Add this script to the Teleporter object in your scene hierarchy.
- In the Teleporter object link the event to the Teleported() event in this script.

## Terrain shader
If you have a terrain shader active on your terrain you may want to increase the value of the **yOffsetTeleportPoints** property to prevent the TeleportPoints sinking into the ground.  
