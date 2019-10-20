using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
// https://github.com/LitJSON/litjson
using LitJson;
using Main;
// https://blog.csdn.net/u014308482/article/details/52958148
using Protobuf.Lobby;

public class LobbyMsgReply
{

    public static void ProcessMsg(byte[] data, int size)
    {
        if (size < 4)
        {
            Debug.Log($"LOBBY - ProcessMsg Error - invalid data size:{size}");
            return;
        }

        byte[] recvHeader = new byte[4];
        Array.Copy(data, 0, recvHeader, 0, 4);
        byte[] recvData = new byte[size - 4];
        Array.Copy(data, 4, recvData, 0, size - 4);

        int msgId = System.BitConverter.ToInt32(recvHeader,0);
        switch ((LOBBY_REPLY) msgId)
        {
            case LOBBY_REPLY.PlayerEnterReply:
                PLAYER_ENTER_REPLY(recvData);
                break;
            case LOBBY_REPLY.AskRoomListReply:
                ASK_ROOM_LIST_REPLY(recvData);
                break;
            case LOBBY_REPLY.AskCreateRoomReply:
                ASK_CREATE_ROOM_REPLY(recvData);
                break;
        }
    }

    static void PLAYER_ENTER_REPLY(byte[] bytes)
    {
        PlayerEnterReply input = PlayerEnterReply.Parser.ParseFrom(bytes);
        if (input.Ret)
        {
            ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.CONNECTED);
        }
        else
        {
            string msg = "玩家进入大厅失败！！！";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            Debug.Log(msg);
            ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.DISCONNECTED);
        }
    }

    static void ASK_ROOM_LIST_REPLY(byte[] bytes)
    {
        AskRoomListReply input = AskRoomListReply.Parser.ParseFrom(bytes);
        PanelLobbyMain.Instance.ClearRoomList();
        foreach (var room in input.Rooms)
        {
            PanelLobbyMain.Instance.AddRoomInfo(room.Name, room.RoomId, room.PlayerCount, room.MaxPlayerCount, room.CreateTime);
        }
    }

    static void ASK_CREATE_ROOM_REPLY(byte[] bytes)
    {
        UIManager.Instance.EndConnecting();
        AskCreateRoomReply input = AskCreateRoomReply.Parser.ParseFrom(bytes);
        if (input.Ret)
        {
            // 根据大厅传递回来的RoomServer的地址，链接RoomServer
            // 这个类是Room场景初始化的时候,GameRoomManager需要的数据，因为跨场景了，所以需要一个全局的地方进行传递
            EnterRoomData roomData = ClientManager.Instance.EnterRoom;
            roomData.Address = input.RoomServerAddress;
            roomData.Port = input.RoomServerPort;
            roomData.MaxPlayerCount = input.MaxPlayerCount;
            roomData.RoomName = input.RoomName;
            roomData.IsCreateByMe = true;
            roomData.RoomId = 0;
            
            // 正式进入房间了。。。加载Room场景
            ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.CONNECTING_ROOM);
            Debug.Log($"MSG: 大厅回复可以创建房间。RoomServer:{roomData.Address}:{roomData.Port} - Room Name:{roomData.RoomName}");
        }
        else
        {
            string msg = $"大厅发现没有多余的房间服务器可以分配！";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            Debug.Log("MSG: " + msg);
        }
    }
}
