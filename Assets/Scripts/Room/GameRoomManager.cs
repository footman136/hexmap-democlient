using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Main;
using Google.Protobuf;
using Protobuf.Room;
using System;
using System.IO;

public class GameRoomManager : ClientScript
{
    public static GameRoomManager Instance;

    public HexmapHelper HexmapHelper;
    public RoomLogic RoomLogic;

    public long RoomId;
    public string RoomName;

    public PlayerEnter CurrentPlayer;

    public bool IsAiOn;
    
    #region 初始化
    void Awake()
    {
        if(Instance != null)
            Debug.LogError("GameRoomManager is Singleton! Cannot be created again!");
        Instance = this;
    }

    private EnterRoomData roomData;
    // Start is called before the first frame update
    void Start()
    {
         roomData = new EnterRoomData();
        if (ClientManager.Instance != null)
        {
            roomData = ClientManager.Instance.EnterRoom;
        }
        else
        {// 单独运行本场景的时候，CliengtManager不存在
            roomData.Address = "192.168.20.131";
            roomData.Port = 8888;
            roomData.RoomName = "遗落の战境20";
            roomData.IsCreatingRoom = false;
            roomData.RoomId = 4787989458105721498;
        }

        _address = roomData.Address;
        _port = roomData.Port;
        
        base.Start();
        UIManager.Instance.BeginLoading();

        Completed += OnComplete;
        Received += OnReceiveMsg;
        RoomLogic.Init();
        Log($"GameRoomManager.Start()! 开始链接RoomServer - {_address}:{_port}");

        if (ClientManager.Instance == null)
            StartCoroutine(LoadMap());

        IsAiOn = true;
    }

    IEnumerator LoadMap()
    {
        yield return null;
        CreateJoinRoom(roomData);
        
    }

    void OnDestroy()
    {
        Completed -= OnComplete;
        Received -= OnReceiveMsg;
        RoomLogic.Fini();
    }

    // Update is called once per frame
    protected void Update()
    {
        base.Update();
    }
    #endregion

    #region 收发消息
    /// <summary>
    /// 新增的发送消息函数，增加了消息ID，会把前面的消息ID（4字节）和后面的消息内容组成一个包再发送
    /// </summary>
    /// <param name="msgId">消息ID</param>
    /// <param name="???"></param>
    public void SendMsg(ROOM msgId, byte[] data)
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
                // 登录到RoomServer
                PlayerEnter data = new PlayerEnter();
                if (ClientManager.Instance != null)
                {
                    data.Account = ClientManager.Instance.Player.Account;
                    data.TokenId = ClientManager.Instance.Player.TokenId;
                }
                else
                {
                    data.Account = "Footman";
                    data.TokenId = 123456;
                }

                CurrentPlayer = data; // 保存当前玩家的信息在本类，这样以后不用大老远去找ClientManager
                SendMsg(ROOM.PlayerEnter, data.ToByteArray());
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
        Log(msg);
    }
    
    void OnReceiveMsg(byte[] data)
    {
        RoomMsgReply.ProcessMsg(data, data.Length);
    }
    #endregion
    
    #region 事件处理

    public void CreateJoinRoom(EnterRoomData roomData)
    {
        if (roomData.IsCreatingRoom)
        {// 创建房间流程

            // 把地图数据上传到房间服务器保存。后面的和加入房间一样了。
            BinaryReader reader = HexmapHelper.BeginLoadBuffer(roomData.RoomName);
            if (reader != null)
            {
                const int CHUNK_SIZE = 900;
                byte[] bytes = new byte[CHUNK_SIZE];
                int size = CHUNK_SIZE;
                bool isFileEnd = false;
                int index = 0;
                while (!isFileEnd)
                {
                    if (!HexmapHelper.LoadBuffer(reader, out bytes, ref size, ref isFileEnd))
                    {
                        Log($"Load Buffer Failed - {roomData.RoomName}");
                    }
                    
                    UploadMap output = new UploadMap()
                    {
                        RoomName = roomData.RoomName,
                        MaxPlayerCount = roomData.MaxPlayerCount,
                        MapData = ByteString.CopyFrom(bytes),
                        PackageIndex = index++,
                        IsLastPackage = isFileEnd,
                    };

                    SendMsg(ROOM.UploadMap, output.ToByteArray());
                }

                Log($"MSG: 发送地图数据 - 地图名:{roomData.RoomName} - Total Size:{reader.BaseStream.Length}");
                HexmapHelper.EndLoadBuffer(ref reader);
            }
        }
        else
        {// 直接申请进入房间
            // 发出EnterRoom消息，进入房间
            EnterRoom output = new EnterRoom()
            {
                RoomId = roomData.RoomId,
            };
            SendMsg(ROOM.EnterRoom, output.ToByteArray());
            Log($"MSG: CreateJoinRoom - 申请进入房间：{roomData.RoomName}");
        }
    }
    #endregion
}
