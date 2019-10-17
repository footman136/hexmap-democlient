using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// https://github.com/LitJSON/litjson
using LitJson;
using System;
using Google.Protobuf;
using Main;

// https://blog.csdn.net/u014308482/article/details/52958148
using Protobuf.Lobby;

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

    /// <summary>
    /// 新增的发送消息函数，增加了消息ID，会把前面的消息ID（4字节）和后面的消息内容组成一个包再发送
    /// </summary>
    /// <param name="msgId">消息ID</param>
    /// <param name="???"></param>
    public void SendMsg(LOBBY msgId, byte[] data)
    {
        byte[] sendData = new byte[data.Length + 4];
        byte[] sendHeader = System.BitConverter.GetBytes((int)msgId);
        
        Array.Copy(sendHeader, 0, sendData, 0, 4);
        Array.Copy(data, 0, sendData, 4, data.Length);
        SendMsg(sendData);
    }

    void OnComplete(SocketAction action, string msg)
    {
        switch (action)
        {
            case SocketAction.Connect:
            {
                UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Success);
                // 发送第一条消息，登录该用户，第一次使用Protobuf哦。。。Oct.17.2019. Liu Gang.
                PlayerEnter data = new PlayerEnter()
                {
                    Account = ClientManager.Instance.Player.Account,
                    TokenId = ClientManager.Instance.Player.TokenId,
                };
                SendMsg(LOBBY.PlayerEnter, data.ToByteArray());
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
        LobbyMsgReply.ProcessMsg(data, data.Length);
    }
}
