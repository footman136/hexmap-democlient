using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AI;
using GameUtils;
using Protobuf.Room;
using UnityEngine;

public class RoomLogic : MonoBehaviour
{

    public ActorManager ActorManager = new ActorManager();
    
    #region 初始化
    
    // Start is called before the first frame update
    void Start()
    {
        
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
    }

    private void RemoveListener()
    {
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.CreateAtroopReply, OnCreateATroopReply);
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.DestroyAtroopReply, OnDestroyATroopReply);
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.TroopMoveReply, OnTroopMoveReply);
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
            input.PosX, input.PosZ, input.Orientation, input.Species);
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

        GameRoomManager.Instance.HexmapHelper.DoMove(input.ActorId, input.PosToX, input.PosToZ, input.Speed);
    }

    private void OnTroopAiStateReply(byte[] bytes)
    {
        TroopAiStateReply input = TroopAiStateReply.Parser.ParseFrom(bytes);
        
    }

    #endregion
}
