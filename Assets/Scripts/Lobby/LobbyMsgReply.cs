using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
// https://github.com/LitJSON/litjson
using LitJson;

public class LobbyMsgReply
{
    public enum MSG
    {
        PLAYER_ENTER = 10001,
        SOCKET_EVENT = 10007,
        CHAT_MESSAGE = 11000,
    }

    public static void ProcessMsg(byte[] data)
    {
        var message = System.Text.Encoding.UTF8.GetString(data, 0, data.Length);
        var dataJson = JsonMapper.ToObject(message);
        int cmdId = Int32.Parse(dataJson["cmd_id"].ToString());
        switch ((MSG) cmdId)
        {
            case MSG.PLAYER_ENTER:
                PLAYER_ENTER(dataJson);
                break;
            case MSG.SOCKET_EVENT:
                SOCKET_EVENT(dataJson);
                break;
            case MSG.CHAT_MESSAGE:
                CHAT_MESSAGE(dataJson);
                break;
        }
    }

    public static void PLAYER_ENTER(JsonData dataJson)
    {
    }
    public static void SOCKET_EVENT(JsonData dataJson)
    {
        //int intAction = Int32.Parse(dataJson["cmd_id"].ToString());
        var json = dataJson["cmd_id"];
        int action = int.Parse(json.ToString());
    }
    public static void CHAT_MESSAGE(JsonData dataJson)
    {
    }
}
