using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;

public class GameConnectingRoomState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly ClientManager _game;

    public GameConnectingRoomState(ConnectionStateMachine owner, ClientManager game) : base(owner)
    {
        _game = game;
    }

    public override void Enter()
    {
        UIManager.Instance.BeginLoading();
        UIManager.Instance.BeginConnecting();
        
        // 使用PlayFab链接后台数据库
        // RoomManager一激活，就会连接服务器
        GameObject roomWorker = ClientManager.Instance.RoomManager.gameObject;
        roomWorker.SetActive(true);
    }

    public override void Tick()
    {
    }

    public override void Exit(bool disabled)
    {
        UIManager.Instance.EndConnecting();
    }
}
