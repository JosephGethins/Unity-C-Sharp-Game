using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    public Transform cameraTransform;
    public PlayerMovement player;
    public float cameraMoveTime = 0.5f;

    private bool transitioning = false;
    private Vector2Int currentRoom = Vector2Int.zero;

     // Store room GameObjects by their grid coordinate this way I can keep track of the X and Y position using whole integers
    private Dictionary<Vector2Int, GameObject> rooms = new Dictionary<Vector2Int, GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Find all room GameObjects in the scene and add them to the dictionary that was declared above
        GameObject[] roomObjects = GameObject.FindGameObjectsWithTag("Room");
        foreach (GameObject roomObject in roomObjects)
        {
            // Parse the room coordinate from the GameObject name since they are named "Room_0_0", "Room_1_0", etc. (The X and Y position)
            Vector2Int coord = ParseRoomCoord(roomObject.name);
            rooms[coord] = roomObject;

            // Deactivate all rooms except the starting one:
            roomObject.SetActive(coord == currentRoom);
        }
    }

    private Vector2Int ParseRoomCoord(string name)
    {
        // Taking the second Room for example
        // Room_1_0
        // That would mean its one position from the starter room since its +1 on the X position
        // So we split by _ and only parse the ints and this is how the rooms will be stores
        string[] parts = name.Split('_');
        int x = int.Parse(parts[1]);
        int y = int.Parse(parts[2]);
        return new Vector2Int(x, y);
    }

    public void MoveRoom(Vector2Int newRoom)
    {
        if (transitioning)
        {
            return;
        }

        // When debugging I tried to move to a room that didnt exist and game crashed so adding this check to ignore it and log it
        if (!rooms.ContainsKey(newRoom))
        {
            Debug.LogWarning("Room not found: " + newRoom);
            return;
        }

        StartCoroutine(SlideCameraToRoom(newRoom));
    }

    private IEnumerator SlideCameraToRoom(Vector2Int targetRoom)
    {
        transitioning = true;
        player.Freeze(true); //The reference to the player script and then we use freeze from that script (Since I want to use freeze in diff scripts)

        rooms[targetRoom].SetActive(true);

        Vector3 start = cameraTransform.position;

        Vector3 roomWorldPos = rooms[targetRoom].transform.position;
        Vector3 end = new Vector3(roomWorldPos.x, roomWorldPos.y, cameraTransform.position.z);

        float elapsed = 0f;
        while (elapsed < cameraMoveTime)
        {
            elapsed += Time.deltaTime; // Look up more about delta time
            float t = elapsed / cameraMoveTime;
            t = t * t * (3f - 2f * t); // SmoothStep-like easing
            cameraTransform.position = Vector3.Lerp(start, end, t); // A smooth transition with interpolation percent of t (Look up a bit more about this)
            yield return null;
        }

        // Activate the new room and deactivate the old one
        // Slight change, put camera transform in the middle sinze the next room wasnt active during transition but only active after
        // so this should hopefully allow the player to not get stuck in limbo between rooms
        //rooms[targetRoom].SetActive(true);

        cameraTransform.position = end;

        rooms[currentRoom].SetActive(false);

        //Need this line because it broke and only set on old room
        currentRoom = targetRoom; 

        // Unfreeze player (Celeste style)
        player.Freeze(false); 
        transitioning = false;
    }
}
