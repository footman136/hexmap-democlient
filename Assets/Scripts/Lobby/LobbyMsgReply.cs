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
            Debug.Log($"ProcessMsg Error - invalid data size:{size}");
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
            ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.START);
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
}
