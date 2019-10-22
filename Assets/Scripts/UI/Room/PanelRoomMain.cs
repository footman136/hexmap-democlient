using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PanelRoomMain : MonoBehaviour
{
    [SerializeField] private Texture2D _texCursorCreateActor;
    [SerializeField] private Texture2D _texCursorFindPath;
    
    public enum CommandType
    {
        CMD_NONE = 0,
        CMD_CREATE_ACTOR = 1,
        CMD_FIND_PATH = 2,
    };

    public CommandType _commandType;
    
    // Start is called before the first frame update
    void Start()
    {
        _commandType = CommandType.CMD_NONE;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            SetCommand(CommandType.CMD_NONE, false);
        }
    }

    public void SetCommand(CommandType command, bool bSetCursor = true)
    {
        _commandType = command;
        if (!bSetCursor)
            return;
        switch (command)
        {
            case CommandType.CMD_NONE:
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                break;
            case CommandType.CMD_CREATE_ACTOR:
                Cursor.SetCursor(_texCursorCreateActor, Vector2.zero, CursorMode.Auto);
                break;
            case CommandType.CMD_FIND_PATH:
                Cursor.SetCursor(_texCursorFindPath, Vector2.zero, CursorMode.Auto);
                break;
        }
    }

    public void OnClickExit()
    {
        LeaveRoom output = new LeaveRoom()
        {
            RoomId = GameRoomManager.Instance.RoomId,
            ReleaseIfNoUser = true,
        };
        if(GameRoomManager.Instance)
            GameRoomManager.Instance.SendMsg(ROOM.LeaveRoom, output.ToByteArray());
    }

    public void OnClickCreateActor()
    {
        SetCommand(CommandType.CMD_CREATE_ACTOR);
    }
}
