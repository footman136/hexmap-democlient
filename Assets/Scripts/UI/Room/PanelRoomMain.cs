using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PanelRoomMain : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClickExit()
    {
        LeaveRoom output = new LeaveRoom()
        {
            RoomId = GameRoomManager.Instance.RoomId,
            ReleaseIfNoUser = true,
        };
        GameRoomManager.Instance.SendMsg(ROOM.LeaveRoom, output.ToByteArray());
    }
}
