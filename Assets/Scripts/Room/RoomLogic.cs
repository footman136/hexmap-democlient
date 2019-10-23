using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GameUtils;
using Protobuf.Room;
using UnityEngine;

public class RoomLogic : MonoBehaviour
{
    #region 初始化
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
    }

    private void RemoveListener()
    {
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.CreateAtroopReply, OnCreateATroopReply);
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.DestroyAtroopReply, OnDestroyATroopReply);
        
    }
    
    #endregion
    
    #region 消息处理

    private void OnCreateATroopReply(byte[] bytes)
    {
        CreateATroopReply input = CreateATroopReply.Parser.ParseFrom(bytes);

        GameRoomManager.Instance.HexmapHelper.CreateUnit(input.Species, input.PosX, input.PosZ, input.Orientation,
            input.ActorId, input.OwnerId);
    }

    private void OnDestroyATroopReply(byte[] bytes)
    {
        DestroyATroopReply input = DestroyATroopReply.Parser.ParseFrom(bytes);

        GameRoomManager.Instance.HexmapHelper.DestroyUnit(input.ActorId);
    }

    #endregion
}
