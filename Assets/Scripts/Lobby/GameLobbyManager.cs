using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// https://github.com/LitJSON/litjson
using LitJson;
using System;
using Main;

public class GameLobbyManager : ClientScript
{
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("GameLobbyManager.Start()");
        base.Start();

        Completed += OnComplete;
        Received += OnReceiveMsg;
    }

    void OnDestroy()
    {
        Completed -= OnComplete;
        Received -= OnReceiveMsg;
    }

    // Update is called once per frame
    protected void Update()
    {
        base.Update();
    }

    void OnComplete(SocketAction action, string msg)
    {
        switch ((SocketAction)action)
        {
            case SocketAction.Connect:
            {
                UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Success);
                // 发送第一条消息，登录该用户
                long tokenId = ClientManager.Instance.Player.TokenId;
                string account = ClientManager.Instance.Player.Account;
                SendMsg(MsgDefine.PLAYER_ENTER(account, tokenId));
            }
                break;
            case SocketAction.Send:
                break;
            case SocketAction.Receive:
                break;
            case SocketAction.Close:
                UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
                break;
            case SocketAction.Error:
                break;
        }
        Debug.Log(msg);
    }
    
    void OnReceiveMsg(byte[] data)
    {
        LobbyMsgReply.ProcessMsg(data);
    }
}
