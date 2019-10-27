using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AI;
using GameUtils;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;

public class RoomLogic : MonoBehaviour
{

    public ActorManager ActorManager = new ActorManager();
    public UrbanManager UrbanManager = new UrbanManager();
    
    
    #region 初始化
    
    // Start is called before the first frame update
    void Start()
    {
        UrbanManager._HexmapHelper = GameRoomManager.Instance.HexmapHelper;
    }

    // Update is called once per frame
    void Update()
    {
        ActorManager.Tick();
    }

    public void Init()
    {
        AddListener();
    }

    public void Fini()
    {
        RemoveListener();
    }

    private void AddListener()
    {
        MsgDispatcher.RegisterMsg((int)ROOM_REPLY.CreateAtroopReply, OnCreateATroopReply);
        MsgDispatcher.RegisterMsg((int)ROOM_REPLY.DestroyAtroopReply, OnDestroyATroopReply);
        MsgDispatcher.RegisterMsg((int)ROOM_REPLY.TroopMoveReply, OnTroopMoveReply);
        MsgDispatcher.RegisterMsg((int)ROOM_REPLY.TroopAiStateReply, OnTroopAiStateReply);
        MsgDispatcher.RegisterMsg((int)ROOM_REPLY.AskForCitiesReply, OnAskForCitiesReply);
        MsgDispatcher.RegisterMsg((int)ROOM_REPLY.CityAddReply, OnCityAddReply);
        MsgDispatcher.RegisterMsg((int)ROOM_REPLY.CityRemoveReply, OnCityRemoveReply);
    }

    private void RemoveListener()
    {
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.CreateAtroopReply, OnCreateATroopReply);
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.DestroyAtroopReply, OnDestroyATroopReply);
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.TroopMoveReply, OnTroopMoveReply);
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.AskForCitiesReply, OnAskForCitiesReply);
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.CityAddReply, OnCityAddReply);
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.CityRemoveReply, OnCityRemoveReply);
    }
    
    #endregion
    
    #region 消息处理

    private void OnCreateATroopReply(byte[] bytes)
    {
        CreateATroopReply input = CreateATroopReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = "创建Actor失败！";
            GameRoomManager.Instance.Log("MSG: CreateATroopReply - " + msg);
            return;
        }
        
        GameRoomManager.Instance.HexmapHelper.CreateUnit(input.RoomId, input.OwnerId, input.ActorId,   
            input.PosX, input.PosZ, input.Orientation, input.Species, input.CellIndex);
    }

    private void OnDestroyATroopReply(byte[] bytes)
    {
        DestroyATroopReply input = DestroyATroopReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = "销毁Actor失败！";
            GameRoomManager.Instance.Log("MSG: DestroyATroopReply - " + msg);
            return;
        }

        GameRoomManager.Instance.HexmapHelper.DestroyUnit(input.ActorId);
    }

    private void OnTroopMoveReply(byte[] bytes)
    {
        TroopMoveReply input = TroopMoveReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = "移动Actor失败！";
            GameRoomManager.Instance.Log("MSG: TroopMoveReply - " + msg);
            return;
        }

        GameRoomManager.Instance.HexmapHelper.DoMove(input.ActorId, input.PosFromX, input.PosFromZ, input.PosToX, input.PosToZ, input.Speed);
    }

    private void OnTroopAiStateReply(byte[] bytes)
    {
        TroopAiStateReply input = TroopAiStateReply.Parser.ParseFrom(bytes);
        
    }

    private void OnAskForCitiesReply(byte[] bytes)
    {
        AskForCitiesReply input = AskForCitiesReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = $"查询城市信息失败！";
            GameRoomManager.Instance.Log("MSG: AskForCities Error - " + msg);
            return;
        }
        GameRoomManager.Instance.Log($"MSG: AskForCitiesReply OK - 城市个数:{input.MyCityCount}/{input.TotalCityCount}");

        // 如果我一个城市都没有，则主动创建一个城市
        if (input.MyCityCount == 0)
        {
            UrbanCity city = GameRoomManager.Instance.RoomLogic.UrbanManager.CreateRandomCity();
            if (city == null)
            {
                string msg2 = "自动创建城市失败！";
                UIManager.Instance.SystemTips(msg2, PanelSystemTips.MessageType.Error);
                GameRoomManager.Instance.Log("MSG: DOWNLOAD_MAP_REPLY - " + msg2);
            }
            else
            {
                CityAdd output = new CityAdd()
                {
                    RoomId = city.RoomId,
                    OwnerId = city.OwnerId,
                    CityId = city.CityId,
                    PosX = city.PosX,
                    PosZ = city.PosZ,
                    CellIndex= city.CellIndex,
                    CityName = city.CityName,
                    CitySize = city.CitySize,
                };
                GameRoomManager.Instance.SendMsg(ROOM.CityAdd, output.ToByteArray());
                GameRoomManager.Instance.Log("MSG: DOWNLOAD_MAP_REPLY - 申请创建房间...");
            }
        }
        
        // 进入房间整体流程完成
        UIManager.Instance.EndLoading();
    }

    private void OnCityAddReply(byte[] bytes)
    {
        CityAddReply input = CityAddReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = $"创建城市失败！";
            GameRoomManager.Instance.Log("MSG: CityAddReply Error - " + msg);
            return;
        }
        UrbanCity city = new UrbanCity()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            CityId = input.CityId,
            PosX = input.PosX,
            PosZ = input.PosZ,
            CellIndex = input.CellIndex,
            CityName = input.CityName,
            CitySize = input.CitySize,
            IsCapital = input.IsCapital,
        };
        UrbanManager.AddCity(city);

        if (city.OwnerId == GameRoomManager.Instance.CurrentPlayer.TokenId
            && city.IsCapital)
        {
            GameRoomManager.Instance.HexmapHelper.SetCameraPosition(input.CellIndex);
        }
        GameRoomManager.Instance.Log($"MSG: CityAddReply OK - 创建城市，坐标:{city.PosX}, {city.PosZ} - Index:{city.CellIndex} - CitySize:{city.CitySize} - IsCapital:{city.IsCapital}");
    }

    private void OnCityRemoveReply(byte[] bytes)
    {
        CityRemoveReply input = CityRemoveReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = $"删除城市失败！";
            GameRoomManager.Instance.Log("MSG: CityRemoveReply Error - " + msg);
            return;
        }
        UrbanManager.RemoveCity(input.CityId);
    }
    

    #endregion
}
