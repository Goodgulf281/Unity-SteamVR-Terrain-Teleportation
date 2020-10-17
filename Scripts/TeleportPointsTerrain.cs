using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using Valve.VR.InteractionSystem;

/*
 * 
 *                                  o        o        <-  numberOfRings (#o=3)
 *                                  |       /
 *                                  o      o
 *                                  |  s  /         s = segment
 *                                  o    o
 *                                  | c /           c = cornerAngle = 2pi / #s
 *                              Player
 * 
 *  
 * 
 */


public class TeleportPointsTerrain : MonoBehaviour
{

    public int numberOfRings            = 4;        // see above diagram
    public int numberOfSegments         = 8;        // see above diagram
    public bool shiftRings              = true;     // shifts the uneven rings with 1/2 cornerangle
    public float distance               = 10.0f;    // maximum distance for the teleport
    public float yOffsetRaycast         = 10.0f;    // the top down raycast is shot from this distance up from the player's Y position
    public float maximumTerrainAngle    = 30.0f;    // when a teleport point is supposed to be placed we'll first check if the terrain is too steep
    public float yOffsetTeleportPoints  = 0.0f;     // teleporters get placed with this offset to compensate for terrain shader influences
    public Vector3 parkingSpot          = new Vector3(0, 0, 0); // park teleport points here if they are not place on the terrain

    public GameObject teleportPrefab;               // the prefab used for the teleport points

    List<Vector3> raycastPositions;                 // all teleport point positions based on above diagram (total amount = rings * segments)
    List<TeleportPoint> teleportPoints;             // all teleport point prefab instances are stored in this list

    private Player player;                          // reference to the player so we can get its position after every teleport jump

        
    void Awake()
    {
        raycastPositions    = new List<Vector3>();
        teleportPoints      = new List<TeleportPoint>();

        float shiftRing = 0f;
        
        if(shiftRings) 
            shiftRing = 0.5f;

        // Calculate the positions of the teleport points based on the diagram:
        //
        // For each ring (start at 1 since there's no need to place teleporters on the Player's position)
        for (int i=1; i<numberOfRings+1; i++)
        {
            float radius = i * (distance / numberOfRings);

            // For each segment
            for(int j=1; j < numberOfSegments + 1; j++)
            {
                float cornerAngle = 2f * Mathf.PI / (float)numberOfSegments * (j + shiftRing * i % 2); // shift every other ring by half a corner angle

                Vector3 currentVector = new Vector3(Mathf.Cos(cornerAngle)*radius,yOffsetRaycast, Mathf.Sin(cornerAngle) * radius); // calculate the position of each teleport point
                
                raycastPositions.Add(currentVector);

                // Teleport points need to be instantiated in Awake. If done later they will not be included when the (Steam VR) Teleporter collects them in its Start().
                TeleportPoint teleportPoint = Instantiate(teleportPrefab, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<TeleportPoint>();
                teleportPoints.Add(teleportPoint);
            }
        }
    }

    void Start()
    {
        player = Player.instance;

        if (player == null)
        {
            Debug.LogError("<b>TeleportPointsTerrain.Start():</b> No Player instance found in map.");
        }
    }


    // Update is called once per frame
    void Update()
    {

    }

    public void Teleported()
    {
        // This method needs to be called whenever a teleport was completed. For this some changes need to be made to the Steam VR code :-(
        // The changes are described in the Readme file of the Github repo.

        // Debug.LogWarning("Teleported!");

        if(player!=null)
        {
            // Store the player's position
            Vector3 playerPosition = player.trackingOriginTransform.position;

            // Cycle to each of the raycast positions to determine if the teleport point needs to be positioned underneath it
            for (int i=0;i<raycastPositions.Count;i++)
            {

                Vector3 currentPosition = playerPosition + raycastPositions[i];
                Vector3 down = Vector3.down;

                // Get the teleportpoint associated with this raycastPosition (by index)
                TeleportPoint teleportPoint = teleportPoints[i];

                RaycastHit hit;

                // Assume the teleportpoint needs to be parked unless all conditions are met (terrain angle low enough, raycast hits an object tagged "Terrain")
                bool parkTeleportSpot = true;

                if (Physics.Raycast(currentPosition, down, out hit, yOffsetRaycast * 2) && hit.transform.tag == "Terrain")
                {
                    // move a teleportpoint to this position when it hits the terrain

                    // step 1: check the angle, using code from http://thehiddensignal.com/unity-angle-of-sloped-ground-under-player/

                    Vector3 temp            = Vector3.Cross(hit.normal, Vector3.down);
                    Vector3 groundSlopeDir  = Vector3.Cross(temp, hit.normal);
                    float groundSlopeAngle  = Vector3.Angle(hit.normal, Vector3.up);

                    // step 2: if within maximum allowed angle then place teleport spot
                    if (groundSlopeAngle < maximumTerrainAngle)
                    {
                        // The yOffsetTeleportPoints may be needed, for example if you use Microsplat with tessellation enabled.
                        // Otherwise the teleportpoint may sink too far into the ground.
                        teleportPoint.transform.position = new Vector3(hit.point.x, hit.point.y + yOffsetTeleportPoints, hit.point.z);
                        parkTeleportSpot = false;
                    }
                }
                
                if(parkTeleportSpot)
                {
                    // move teleport point [i] to parking spot which defaults to (0,0,0). Move it somewhere else if (0,0,0) is actually used in the scene.
                    teleportPoint.transform.position = parkingSpot; 
                }
            }
        }
        else Debug.LogError("<b>TeleportPointsTerrain.Teleported():</b> player == null");
    }


}
