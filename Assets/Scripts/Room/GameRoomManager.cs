using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Main;
using Google.Protobuf;
using Protobuf.Room;
using System;
using System.IO;
using AI;
using Animation;

public class GameRoomManager : ClientScript
{
    public static GameRoomManager Instance;

    private const float _HEART_BEAT_INTERVAL = 15f; // 心跳间隔(秒)
    
    public HexmapHelper HexmapHelper;
    public RoomLogic RoomLogic;
    public RoomPlayerInfo CurrentPlayer;

    // AI代理权: 专门存储其他玩家, 自己是CurrentPlayer, 不在这里
    private Dictionary<long, RoomPlayerInfo> _playersAi = new Dictionary<long, RoomPlayerInfo>();
    public Dictionary<long, RoomPlayerInfo> PlayersAi => _playersAi;

    [HideInInspector]
    public CsvDataManager CsvDataManager;
    public CommandManager CommandManager;
    public FightManager FightManager;

    public long RoomId;
    public string RoomName;


    public bool IsAiOn;
    
    #region 初始化
    void Awake()
    {
        if(Instance != null)
            Debug.LogError("GameRoomManager is Singleton! Cannot be created again!");
        Instance = this;
        
        //读取数据
        // 为了测试战场内的功能,我需要单独运行Room场景,直接进入房间服务器,这时候CsvDataManger还没有被初始化,所以需要单独被初始化
        // 但是因为这个初始化只能是异步的(从StreamingAssets目录里读取文件,只能使用WWW异步读取),
        // 所以这里做出分支,如果ClientManager.Instance不存在,就说是是独立运行的分支.
        
        if (ClientManager.Instance != null)
        { // CsvDataManager这个实例在游戏一开始已经被ClientManager初始化过
            CsvDataManager = ClientManager.Instance.CsvDataManager;
            CommandManager.LoadCommands();
            roomData = ClientManager.Instance.EnterRoom;
            _address = roomData.Address;
            _port = roomData.Port;
        }
        else
        { // CsvDataManager尚未被初始化过
            CsvDataManager = gameObject.AddComponent<CsvDataManager>();
            StartCoroutine(DownloadDataFiles());
        }
    }

    private EnterRoomData roomData;
    // Start is called before the first frame update
    void Start()
    {
        UIManager.Instance.BeginLoading();
        
        base.Start();

        //网络
        Completed += OnComplete;
        Received += OnReceiveMsg;
        RoomLogic.Init();

        if (ClientManager.Instance != null)
        { // 数据都有了,可以直连房间服务器
            Log($"GameRoomManager.Start()! 开始链接RoomServer - {_address}:{_port}");
            Connect();
        }
    }

    void OnDestroy()
    {
        Completed -= OnComplete;
        Received -= OnReceiveMsg;

        // 退出的时候, 最后尝试一下看消息能否发出
        LeaveRoom output = new LeaveRoom()
        {
            RoomId = RoomId,
            ReleaseIfNoUser = true,
        };
        SendMsg(ROOM.LeaveRoom, output.ToByteArray());
        RoomLogic.Fini();
    }

    // Update is called once per frame
    protected void Update()
    {
        base.Update();
    }
    
    IEnumerator DownloadDataFiles()
    {
        // 这个函数仅在单独调试房间服务器的时候才被调用, 所以做如下判定
        // 单独运行本场景的时候，CliengtManager不存在
        if (ClientManager.Instance != null)
            yield break; 
        yield return StartCoroutine(CsvDataManager.LoadDataAllAndroid());
        CommandManager.LoadCommands();
        
        roomData = new EnterRoomData();
        {
            long defaultRoomId = 0;
            // 从[server_config]表里读取服务器地址和端口
            var csv = CsvDataManager.Instance.GetTable("server_config_client");
            if (csv != null)
            {
                _address = csv.GetValue(1, "RoomServerAddress");
                _port = csv.GetValueInt(1, "RoomServerPort");
                defaultRoomId = csv.GetValueLong(1, "DefaultRoomId");
            }
            
            roomData.Address = _address;
            roomData.Port = _port;
            roomData.RoomName = "遗落の战境3";
            roomData.IsCreatingRoom = false;
            roomData.RoomId = defaultRoomId;
        }
        
        // 读取了数据表才能开始连接房间服务器
        Log($"GameRoomManager.Start()! 开始链接RoomServer - {_address}:{_port}");
        Connect();
        
        //初始化结束
        IsAiOn = false;
        Debug.Log("GameRoomManager DowloadDataFiles - OK!");
    }

    public void LoadMap()
    {
        CreateJoinRoom(roomData);
    }

    #endregion
    
    #region 心跳
    
    public void StartHeartBeat()
    {
        InvokeRepeating(nameof(HeartBeat), 0, _HEART_BEAT_INTERVAL);
    }

    private void StopHeartBeat()
    {
        CancelInvoke(nameof(HeartBeat));
    }
    private void HeartBeat()
    {
        HeartBeat output = new HeartBeat();
        SendMsg(ROOM.HeartBeat, output.ToByteArray());
    }
    
    #endregion

    #region 得到角色

//    public ActorBehaviour GetActorBehaviour(long actorId)
//    {
//        var ab = RoomLogic.ActorManager.GetActor(actorId);
//        return ab;
//    }

    public ActorVisualizer GetActorVisualizer(long actorId)
    {
        var ab = RoomLogic.ActorManager.GetActor(actorId);
        if (ab == null) return null;
        var cell = HexmapHelper.GetCell(ab.CellIndex);
        var av = cell.Unit.GetComponent<ActorVisualizer>();
        return av;
    }
    
    #endregion

    #region AI Rights - AI 代理权

    public void AddAiPlayer(long aiPlayerId, string account)
    {
        if (_playersAi.ContainsKey(aiPlayerId))
        {
            Log($"GameRoomManager AddAiPlayer Error - Duplicated Ai Player! - {account} - {aiPlayerId}");
        }
        RoomPlayerInfo rpi = gameObject.AddComponent<RoomPlayerInfo>();
        if (rpi)
        {
            rpi.Init(account, aiPlayerId);
            RoomLogic.ActorManager.SetAiRights(aiPlayerId, true);
            _playersAi[aiPlayerId] = rpi;
        }
    }

    public void RemoveAiPlayer(long aiPlayerId)
    {
        if (_playersAi.ContainsKey(aiPlayerId))
        {
            RoomLogic.ActorManager.SetAiRights(aiPlayerId, false);
            RoomPlayerInfo rpi = GetAiPlayer(aiPlayerId);
            _playersAi.Remove(aiPlayerId);
            Destroy(rpi);
        }
    }
    

    public RoomPlayerInfo GetAiPlayer(long aiPlayerId)
    {
        if (_playersAi.ContainsKey(aiPlayerId))
        {
            return _playersAi[aiPlayerId];
        }

        return null;
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
                PlayerEnter enter = new PlayerEnter();
                if (ClientManager.Instance != null)
                {
                    enter.Account = ClientManager.Instance.Player.Account;
                    enter.TokenId = ClientManager.Instance.Player.TokenId;
                }
                else
                {
                    enter.Account = "Footman3";
                    enter.TokenId = 123456;
                    //enter.TokenId = 1234561;
                }
                CurrentPlayer.Init(enter.Account, enter.TokenId);
                SendMsg(ROOM.PlayerEnter, enter.ToByteArray());
                StartHeartBeat(); // 开始心跳
                Log(msg);
            }
                break;
            case SocketAction.Send:
                break;
            case SocketAction.Receive:
                break;
            case SocketAction.Close:
                StopHeartBeat();
                UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
                UIManager.Instance.MessageBox("错误", msg, (int)PanelMessageBox.BUTTON.OK, OnClickMessageBox);
                Debug.LogWarning(msg);
                break;
            case SocketAction.Error:
                UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
                UIManager.Instance.MessageBox("错误", msg, (int)PanelMessageBox.BUTTON.OK, OnClickMessageBox);
                Debug.LogWarning(msg);
                break;
        }
    }
    
    void OnClickMessageBox(int index)
    {
        if (ClientManager.Instance != null)
        {
            ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.LOBBY);
        }
    }
    
    void OnReceiveMsg(byte[] data)
    {
        RoomMsgReply.ProcessMsg(data, data.Length);
    }
    
    #endregion
    
    #region 事件处理

    public void CreateJoinRoom(EnterRoomData roomData)
    {
        Debug.Log("GameRoomManager CreateJoinRoom() Begin...");
        if (roomData.IsCreatingRoom)
        {// 创建房间流程
            
            Log($"MSG: CreateJoinRoom - 创建房间：{roomData.RoomName}");

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
