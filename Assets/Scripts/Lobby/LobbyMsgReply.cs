using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
// https://github.com/LitJSON/litjson
using LitJson;
// https://blog.csdn.net/u014308482/article/details/52958148
using Protobuf.Lobby;
using static MsgDefine;

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
            case LOBBY_REPLY.PLAYER_ENTER_REPLY:
                PLAYER_ENTER_REPLY(recvData);
                break;
        }
    }

    static void PLAYER_ENTER_REPLY(byte[] data)
    {
        PlayerEnterReply per = PlayerEnterReply.Parser.ParseFrom(data);
        if (per.Ret)
        {
            
        }
        else
        {
            string msg = "玩家进入大厅失败！！！";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            Debug.Log(msg);
        }
    }
    static void CHAT_MESSAGE(JsonData dataJson)
    {
    }
}
