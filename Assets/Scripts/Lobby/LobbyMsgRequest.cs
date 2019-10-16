using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
// https://github.com/LitJSON/litjson
using LitJson;

public class LobbyMsgRequest
{
    public enum MSG
    {
        PLAYER_ENTER = 10001,
        SOCKET_EVENT = 10007,
        CHAT_MESSAGE = 11000,
    }
    public static string PLAYER_ENTER(string username, string password){
        JsonData registerJson = new JsonData ();
        registerJson ["cmd_id"] = ((int)MSG.PLAYER_ENTER).ToString();
        registerJson ["cmd"] = "PLAYER_ENTER";
        registerJson ["username"] = username;
        registerJson ["password"] = password;
 
        return registerJson.ToJson();
    }

    public static string SOCKET_EVENT(string message, SocketAction action)
    {
        JsonData registerJson = new JsonData ();
        registerJson["cmd_id"] = ((int)MSG.SOCKET_EVENT).ToString();
        registerJson ["cmd"] = "SOCKET_EVENT";
        registerJson ["message"] = message;
        registerJson ["action"] = (int)action;
 
        return registerJson.ToJson();
    }

    public static string CHAT_MESSAGE(string message)
    {
        JsonData registerJson = new JsonData ();
        registerJson["cmd_id"] = ((int)MSG.CHAT_MESSAGE).ToString();
        registerJson ["cmd"] = "CHAT_MESSAGE";
        registerJson ["message"] = message;
 
        return registerJson.ToJson();
    }
    
}
