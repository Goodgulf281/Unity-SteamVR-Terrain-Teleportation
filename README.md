# Unity-SteamVR-Terrain-Teleportation
Dynamically place SteamVR TeleportationPoints on Unity terrain or in Dungeons.

## YouTube
This video shows the end result when using this code: https://youtu.be/b6n6LhHSIFw

# Prerequisites
- Unity 2019
- SteamVR plugin for Unity (https://assetstore.unity.com/packages/tools/integration/steamvr-plugin-32647)

## Changes to SteamVR teleportation code to include teleportation events
In the latest version this is not needed anymore. I completely missed out on the `Teleport.Player.AddListener(Event< TeleportMarkerBase >)` function which allows you to hook into the Steam events. This is a major improvement since you don't need to update the code everytime a new version of the SteamVR plugin is released.

# How does it work?
 - Tag your terrain with the "Terrain" label or your dungeon with the "Floor" label (which you may need to create first).
 - Add the `TeleportPointsTerrain.cs` script to the Teleporter object (`SteamVR>InteractionSystem>Teleport>Prefabs>Teleporting`) or an empty object in your scene hierarchy.
 - Place one TeleportPoint in the terrain or dungeon close to the Player. Jumping to this point will first trigger the script.
 - If you want to have specific events for certain TeleportPoints then add the `TeleportedHere.cs` script to them.
 - Don't forget to add the `TeleportPoint` prefab to the teleportPrefab property of the 'TeleportPointsTerrain' object in your project hierarchy.

## Terrain shader
If you have a terrain shader active on your terrain you may want to increase the value of the **yOffsetTeleportPoints** property to prevent the TeleportPoints sinking into the ground.  

## Added to latest version
The big change is removing the necessity of modifying the SteamVR plugin code.

The major feature added to the previous version is support for (indoor) dungeons. It uses a square pattern for the teleport points instead of the concentric rings.
Next to this it also included:
 - Line of sight placement of teleport points (so they will not hide behind obstacles)
 - Proximity check for teleport points so the script does not place teleport points near by manually places teleport points
