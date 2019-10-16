using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson; // https://github.com/LitJSON/litjson


/// <summary>
///    ————————————————
///    版权声明：本文为CSDN博主「末零」的原创文章，遵循 CC 4.0 BY-SA 版权协议，转载请附上原文出处链接及本声明。
///    原文链接：https://blog.csdn.net/n_moling/article/details/71480931    
/// </summary>
public class MsgDefine
{
    enum CMD
    {
        PLAYER_ENTER = 10001,
        NORMAL_MESSAGE = 11000,
    }
    
    public static string PLAYER_ENTER(string account, long tokenId){
        JsonData registerJson = new JsonData ();
        registerJson ["cmd_id"] = ((int)CMD.PLAYER_ENTER).ToString();
        registerJson ["cmd"] = "PLAYER_ENTER";
        registerJson ["account"] = account;
        registerJson ["token_id"] = tokenId;
 
        return registerJson.ToJson();
    }

    public static string NORMAL_MESSAGE(string message)
    {
        JsonData registerJson = new JsonData ();
        registerJson["cmd_id"] = ((int)CMD.NORMAL_MESSAGE).ToString();
        registerJson ["cmd"] = "NORMAL_MESSAGE";
        registerJson ["message"] = message;
 
        return registerJson.ToJson();
    }
}
