# Unity-SteamVR-Terrain-Teleportation
Dynamically place SteamVR TeleportationPoints on Unity terrain or in Dungeons.

## YouTube
This video shows the end result when using this code: https://youtu.be/b6n6LhHSIFw

# Prerequisites
- Unity 2019
- SteamVR plugin for Unity (https://assetstore.unity.com/packages/tools/integration/steamvr-plugin-32647)

## Changes to SteamVR teleportation code to include teleportation events
Unfortunately the SteamVR code does not support extending the code easily so I had to change a few lines. I tried to keep it as simple as possible to make it easy to update their code after a version change. These are the code changes needed to make this work:

TeleportMarkerbase.cs, add:
```
using UnityEngine.Events;
```
In the public abstract class TeleportMarkerBase add:
```
public UnityEvent onTeleportToHere;
```

Teleport.cs, add:
``` 
using UnityEngine.Events;
```
In the public class Teleport add:
```
public UnityEvent onTeleportSucceeded;
```
In the method private void TeleportPlayer() add:
```
            onTeleportSucceeded.Invoke();                       // Invoke the event attached to the global teleporting object
            teleportingToMarker.onTeleportToHere.Invoke();      // Invoke the event attached to the teleporting object (teleporting point, teleporting area)
```
(Add this just before the code: Teleport.Player.Send( pointedAtTeleportMarker ); )

# How does it work?
 - Tag your terrain with the "Terrain" label or your dungeon with the "Floor" label (which you may need to create first).
 - Make the changes to the SteamVR code as mentioned above.
 - Add this script to the Teleporter object (SteamVR>InteractionSystem>Teleport>Prefabs>Teleporting) in your scene hierarchy.
 - In the Teleporter object link the OnTeleportSucceeded event to the Teleported() event in this script.
 - Place one TeleportPoint in the terrain or dungeon close to the Player. Jumping to this point will first trigger the script.

## Terrain shader
If you have a terrain shader active on your terrain you may want to increase the value of the **yOffsetTeleportPoints** property to prevent the TeleportPoints sinking into the ground.  

## Added to latest version
The major feature added to the latest version is support for (indoor) dungeons. It uses a square pattern for the teleport points instead of the concentric rings.
Next to this it also includes:
 - Line of sight placement of teleport points (so they will not hide behind obstacles)
 - Proximity check for teleport points so the script does not place teleport points near by manually places teleport points
