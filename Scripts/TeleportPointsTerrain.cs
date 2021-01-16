using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using Valve.VR.InteractionSystem;

/*  
 *  https://github.com/Goodgulf281/Unity-SteamVR-Terrain-Teleportation
 *  
 *  Basic instructions:
 *   - Tag your terrain with the "Terrain" label or your dungeon with the "Floor" label (which you may need to create first).
 *   - Make the changes to the SteamVR code.
 *   - Add this script to the Teleporter object (SteamVR>InteractionSystem>Teleport>Prefabs>Teleporting) in your scene hierarchy.
 *   - In the Teleporter object link the OnTeleportSucceeded event to the Teleported() event in this script.
 *   - Place one TeleportPoint in the terrain or dungeon close to the Player. Jumping to this point will first trigger the script.
 *  
 *  
 *  On terrain use rings:
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
 *  (use the Terrain tag for raycast hits)
 *  
 *  In a dungeon use square setup:
 *  
 *                     o - o - o -  O  - o - o - o  <- numberOfSquares (#o=3)
 *                                  |
 *                                  O  - o - o - o
 *                                  |
 *                                  O  - o - o - o
 *                                  |
 *                              Player - o - o - o
 * 
 * 
 * Other functionality added in this version:
 * 
 * - Line of sight placement of teleport points (so they will not hide behind obstacles)
 * - Proximity check for teleport points so the script does not place teleport points near by manually places teleport points
 * 
 */


public enum TeleportingShape { ShapeRings, ShapeSquares};

public class TeleportPointsTerrain : MonoBehaviour
{
    [Header("Ring Settings")]

    public string ringTag                       = "Terrain";// tag used for Raycast
    public int numberOfRings                    = 4;        // see above diagram
    public int numberOfSegments                 = 8;        // see above diagram
    public bool shiftRings                      = true;     // shifts the uneven rings with 1/2 cornerangle
    public float maximumTerrainAngle            = 30.0f;    // when a teleport point is supposed to be placed we'll first check if the terrain is too steep

    [Header("Squares Settings")]
    public string squareTag                     = "Floor";  // tag used for Raycast
    public int numberOfSquares                  = 3;        // see above diagram
    public bool shiftSquares                    = false;    // shifts the uneven square layers with 1/2 square side


    [Header("Teleporting Basic Settings")]
    public TeleportingShape teleportingShape    = TeleportingShape.ShapeRings;

    public float distance                       = 10.0f;    // maximum distance for the teleport
    public float yOffsetRaycast                 = 10.0f;    // the top down raycast is shot from this distance up from the player's Y position
    public float yOffsetTeleportPoints          = 0.0f;     // teleporters get placed with this offset to compensate for (terrain) shader influences
    public Vector3 parkingSpot                  = new Vector3(0, 0, 0); // park teleport points here if they are not place on the terrain
    public GameObject teleportPrefab;                       // the prefab used for the teleport points, typically this contains teh SteamVR TeleportPoint prefab

    public bool lineOfSightPlacement            = true;     // only places TeleportPoints if they are visible from the player's point of view (so not behind corners or obstacles)
    public bool checkProximity                  = true;     // only places TeleportPoints if they are not within proximity (distance) of a fixed TeleportPoint (manually placed in the level)
    public float proximity                      = 2.0f;     // proximity distance

    private List<Vector3> raycastPositions;         // all teleport point positions based on above diagram (total amount = rings * segments)
    private List<TeleportPoint> teleportPoints;     // all teleport point prefab instances are stored in this list
    private Player player;                          // reference to the player so we can get its position after every teleport jump
    private TeleportMarkerBase[] teleportMarkers;   // collect all fixed TeleportPoints for proximity check.

    private Teleport teleport;                      // reference to the teleport object in the scene hierarchy

    void Awake()
    {
        if (teleportPrefab == null)
        {
            Debug.LogError("<b>TeleportPointsTerrain.Awake():</b> No teleportPrefab has been set.");
        }

        raycastPositions    = new List<Vector3>();
        teleportPoints      = new List<TeleportPoint>();

        // Collect the manually placed teleport points before we create our own set of teleport points.
        if (checkProximity)
        {
            teleportMarkers = GameObject.FindObjectsOfType<TeleportMarkerBase>();
        }

        float shift = 0f;
        
        // Shift offset is used when shift property = true and corresponding shape us selected:
        if((shiftRings && teleportingShape==TeleportingShape.ShapeRings) || (shiftSquares && teleportingShape==TeleportingShape.ShapeSquares))
            shift = 0.5f;

        if (teleportingShape == TeleportingShape.ShapeRings)
        {
            // This is where we place the teleport points for the ring shape.
            //
            // Calculate the positions of the teleport points based on the diagram:
            //
            // For each ring (start at 1 since there's no need to place teleporters on the Player's position)
            for (int i = 1; i < numberOfRings + 1; i++)
            {
                float radius = i * (distance / numberOfRings);

                // For each segment
                for (int j = 1; j < numberOfSegments + 1; j++)
                {
                    float cornerAngle = 2f * Mathf.PI / (float)numberOfSegments * (j + shift * i % 2); // shift every other ring by half a corner angle

                    Vector3 currentVector = new Vector3(Mathf.Cos(cornerAngle) * radius, yOffsetRaycast, Mathf.Sin(cornerAngle) * radius); // calculate the position of each teleport point

                    raycastPositions.Add(currentVector);

                    // Teleport points need to be instantiated in Awake. If done later they will not be included when the (Steam VR) Teleporter collects them in its Start().
                    TeleportPoint teleportPoint = Instantiate(teleportPrefab, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<TeleportPoint>();
                    teleportPoints.Add(teleportPoint);
                }
            }
        }
        else
        {
            // This is where we place the teleport points for the square shape.

            float distanceBetweenPoints = distance / numberOfSquares;

            for (int x = 0; x < (numberOfSquares*2) + 1; x++)
            {
                for (int y = 0; y < (numberOfSquares * 2) + 1; y++)
                { 
                    if((x==numberOfSquares) && (y==numberOfSquares))
                    {
                        // Don't create a TeleportPoint where the Player stands
                        // Debug.Log("Skip center square at ("+x+","+y+")");
                    }
                    else
                    {
                        float localShift = shift * x % 2;

                        if ((localShift)>0 && y == numberOfSquares * 2)
                        {
                            // Debug.Log("Shift remove last position at ("+x+","+y+")");
                            // Because of the shift to right the rightmost position is skipped
                            //
                            // Effectively we'll get this pattern if shiftSquares = true:
                            //
                            //      o - o - o - o - o
                            //        o - o - o - o - x  (x = removed with this code)
                            //      o - o - o - o - o
                        }
                        else
                        {
                            Vector3 currentVector = new Vector3((x - numberOfSquares) * distanceBetweenPoints, yOffsetRaycast, (y- numberOfSquares + localShift) * distanceBetweenPoints);
                            raycastPositions.Add(currentVector);

                            // Teleport points need to be instantiated in Awake. If done later they will not be included when the (Steam VR) Teleporter collects them in its Start().
                            TeleportPoint teleportPoint = Instantiate(teleportPrefab, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<TeleportPoint>();
                            teleportPoints.Add(teleportPoint);
                        }
                    }
                }
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

        teleport = Teleport.instance;
        if (teleport == null)
        {
            Debug.LogError("<b>TeleportPointsTerrain.Start():</b> No Teleport instance found in the hierarchy.");
        }

        // Instead of changing the SteamVR code we can just add a listener using the Teleport.Player SteamVR Event.
        Teleport.Player.AddListener(Teleported);
    }


    // Update is called once per frame
    void Update()
    {

    }

    public void Teleported(TeleportMarkerBase teleportMarkerBase)
    {
        // This method needs to be called whenever a teleport was completed. For this some changes need to be made to the Steam VR code :-(
        // The changes are described in the Readme file of the Github repo.

        // Debug.LogWarning("Teleported!");

        if(player!=null)
        {
            // Assign the appropriate tag for raycasting:

            string tagForRaycast;

            if (teleportingShape == TeleportingShape.ShapeRings)
            {
                tagForRaycast = ringTag;
            }
            else
            {
                tagForRaycast = squareTag;
            }


            // Calculate the POV for line of site origin
            Vector3 PointOfView = player.feetPositionGuess;
            PointOfView.y += player.eyeHeight;

            
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

                if (Physics.Raycast(currentPosition, down, out hit, yOffsetRaycast * 4) && hit.transform.tag == tagForRaycast)
                {
                    // Check for fixed Teleportpoints here
                    //
                    // TODO

                    // move a teleportpoint to this position when it hits the terrain / floor

                    if (teleportingShape == TeleportingShape.ShapeRings)
                    {
                        // step 1: check the angle, using code from http://thehiddensignal.com/unity-angle-of-sloped-ground-under-player/

                        Vector3 temp = Vector3.Cross(hit.normal, Vector3.down);
                        Vector3 groundSlopeDir = Vector3.Cross(temp, hit.normal);
                        float groundSlopeAngle = Vector3.Angle(hit.normal, Vector3.up);

                        // step 2: if within maximum allowed angle then place teleport spot
                        if (groundSlopeAngle < maximumTerrainAngle)
                        {
                            // The yOffsetTeleportPoints may be needed, for example if you use Microsplat with tessellation enabled.
                            // Otherwise the teleportpoint may sink too far into the ground.
                            teleportPoint.transform.position = new Vector3(hit.point.x, hit.point.y + yOffsetTeleportPoints, hit.point.z);
                            parkTeleportSpot = false;
                        }
                    }
                    else
                    {
                        // For squares we don't don the angle check.

                        // The yOffsetTeleportPoints may be needed, for example if you use Microsplat with tessellation enabled.
                        // Otherwise the teleportpoint may sink too far into the ground.
                        teleportPoint.transform.position = new Vector3(hit.point.x, hit.point.y + yOffsetTeleportPoints, hit.point.z);
                        parkTeleportSpot = false;
                    }
                }
                
                if(lineOfSightPlacement && parkTeleportSpot == false)
                {
                    // This is where we check if the teleport point we are going to place hides behind an obstacle. If so the park it instead.
                    RaycastHit hitPOV;

                    // Get a direction from the Player (eyes) to the teleport point to be placed:
                    Vector3 direction = (teleportPoint.transform.position - PointOfView).normalized; 

                    // Cast a ray from the Player (eyes) to the teleport point and see if we hit something:
                    if (Physics.Raycast(PointOfView,direction,out hitPOV,distance*1,5) && hitPOV.transform.tag != tagForRaycast)
                    {
                        // Looks like we hit something else than Floor or Terrain so park thsi teleport point instead
                        parkTeleportSpot = true;
                    }
                }

                if(checkProximity && parkTeleportSpot == false)
                {
                    // Here we'll check if the teleport point to be placed is near any of the manually placed teleport points.
                    // This can be an expensive operation however we're only doing it on each teleport action. If you want you can optimize it using the examples here:
                    // https://forum.unity.com/threads/different-ways-to-find-distance.285226/

                    // We collected the manually placed teleport points in the Awake function and now we'll check each of them for proximity.
                    // This can also become an expensive operation if a lot of teleport points have been placed in the level manually.
                    foreach (TeleportMarkerBase tmb in teleportMarkers)
                    {
                        float distance = Vector3.Distance(tmb.transform.position, teleportPoint.transform.position);

                        if (distance < proximity)
                        {
                            // Debug.Log("Skipping teleport point due to proximity of "+distance);
                            parkTeleportSpot = true;
                        }
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
