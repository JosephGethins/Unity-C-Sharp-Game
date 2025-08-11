using UnityEngine;

public class RoomExitLogic : MonoBehaviour
{
    public Vector2Int targetRoom; // So set this individually per exit through Unity
    public bool FollowCamera = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            RoomManager.Instance.MoveRoom(targetRoom, FollowCamera);
        }
    }
}