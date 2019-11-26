using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AI;
using Animation;
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
        MsgDispatcher.RegisterMsg((int)ROOM_REPLY.ActorAddReply, OnActorAddReply);
        MsgDispatcher.RegisterMsg((int)ROOM_REPLY.ActorRemoveReply, OnActorRemoveReply);
        MsgDispatcher.RegisterMsg((int)ROOM_REPLY.ActorMoveReply, OnActorMoveReply);
        MsgDispatcher.RegisterMsg((int)ROOM_REPLY.CityAddReply, OnCityAddReply);
        MsgDispatcher.RegisterMsg((int)ROOM_REPLY.CityRemoveReply, OnCityRemoveReply);
        MsgDispatcher.RegisterMsg((int)ROOM_REPLY.TryCommandReply, OnTryCommandReply);
    }

    private void RemoveListener()
    {
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.ActorAddReply, OnActorAddReply);
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.ActorRemoveReply, OnActorRemoveReply);
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.ActorMoveReply, OnActorMoveReply);
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.CityAddReply, OnCityAddReply);
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.CityRemoveReply, OnCityRemoveReply);
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.TryCommandReply, OnTryCommandReply);
    }
    
    #endregion
    
    #region 消息处理

    private void OnActorAddReply(byte[] bytes)
    {
        ActorAddReply input = ActorAddReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = "创建Actor失败！";
            GameRoomManager.Instance.Log("MSG: OnActorAddReply - " + msg);
            return;
        }

        if (input.CellIndex == 0)
        {
            Debug.LogError("RoomLogic OnActorAddReply Error - CellIndex is lost!!!");
            return;
        }

        if (GameRoomManager.Instance == null
            || GameRoomManager.Instance.HexmapHelper == null)
        {
            Debug.LogError("What the Fuck! with GameRoomManager and HexmapHelper!!!");
            return;
        }
        GameRoomManager.Instance.HexmapHelper.CreateUnit(input.RoomId, input.OwnerId, input.ActorId,   
            input.PosX, input.PosZ, input.Orientation, input.Species, input.CellIndex, input.ActorInfoId, 
            input.Name, input.Hp, input.HpMax, input.AttackPower, input.DefencePower, input.Speed, input.FieldOfVision, 
            input.ShootingRange, input.AttackDuration, input.AttackInterval, input.AmmoBase, input.AmmoBaseMax);

        // 每增加一个单位, 都要根据现有的AI-代理权情况来给这个单位设置一下, 批量处理在另外的地方
        var ab = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(input.ActorId);
        if (ab != null)
        {
            ab.HasAiRights = GameRoomManager.Instance.GetAiPlayer(input.OwnerId);
        }
    }

    private void OnActorRemoveReply(byte[] bytes)
    {
        ActorRemoveReply input = ActorRemoveReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = $"销毁Actor失败！{input.ActorId}";
            GameRoomManager.Instance.Log("MSG: OnActorRemoveReply Error - " + msg);
        }
        else
        {
            PanelRoomMain.Instance.RemoveSelection(input.ActorId); // 如果该单位被选中,要取消选中
            if (input.DieType == 0)
            { // 自然死亡, 部队解散
                GameRoomManager.Instance.HexmapHelper.DestroyUnit(input.ActorId);
                string msg = $"成功解散部队!{input.ActorId}";
                GameRoomManager.Instance.Log("MSG: OnActorRemoveReply - OK " + msg);
            }
            else if (input.DieType == 1)
            { // 战斗致死, 这时候先不删除该单元,而是要等动画结束以后再删除
                var ab = ActorManager.GetActor(input.ActorId);
                if (ab == null) return;
                ab.StateMachine.TriggerTransition(FSMStateActor.StateEnum.DIE);
                GameRoomManager.Instance.Log($"MSG: OnActorRemoveReply - OK - {ab.Name}:{ab.ActorId} 被杀死!");
            }
        }
    }

    private void OnActorMoveReply(byte[] bytes)
    {
        ActorMoveReply input = ActorMoveReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = "移动Actor失败！";
            GameRoomManager.Instance.Log("MSG: OnActorMoveReply - " + msg);
            return;
        }

        GameRoomManager.Instance.HexmapHelper.DoMove(input.ActorId, input.CellIndexFrom, input.CellIndexTo);
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
        bool isMyCity = input.OwnerId == GameRoomManager.Instance.CurrentPlayer.TokenId;
        UrbanManager.AddCity(city, isMyCity);

        if (city.OwnerId == GameRoomManager.Instance.CurrentPlayer.TokenId
            && city.IsCapital)
        {
            GameRoomManager.Instance.HexmapHelper.SetCameraPosition(input.CellIndex);
        }
        GameRoomManager.Instance.Log($"MSG: OnCityAddReply OK - 创建城市，坐标:{city.PosX}, {city.PosZ} - Index:{city.CellIndex} - CitySize:{city.CitySize} - IsCapital:{city.IsCapital}");
    }

    private void OnCityRemoveReply(byte[] bytes)
    {
        CityRemoveReply input = CityRemoveReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = $"删除城市失败！";
            GameRoomManager.Instance.Log("MSG: OnCityRemoveReply Error - " + msg);
        }
        else
        {
            UrbanManager.RemoveCity(input.CityId);
            GameRoomManager.Instance.Log($"MSG: OnCityRemoveReply OK - 成功删除城市:{input.CityId}");
            PanelRoomMain.Instance.SetSelection(null);
        }
    }

    private void OnTryCommandReply(byte[] bytes)
    {
        TryCommandReply input = TryCommandReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            UIManager.Instance.SystemTips(input.ErrMsg, PanelSystemTips.MessageType.Error);
            GameRoomManager.Instance.Log($"CmdAttack OnTryCommandReply Error - " + input.ErrMsg);
            var ab = ActorManager.GetActor(input.ActorId);
            if (ab != null)
            {
                ab.StateMachine.TriggerTransition(FSMStateActor.StateEnum.IDLE);
            }
            return;
        }

        // 该指令可以执行,虽然是马后炮
    }
    #endregion
}
