using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;

public class GameConnectedRoomState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly ClientManager _game;

    public GameConnectedRoomState(ConnectionStateMachine owner, ClientManager game) : base(owner)
    {
        _game = game;
    }

    private bool _bFirst;
    public override void Enter()
    {
        _bFirst = true;
    }

    public override void Tick()
    {
        if (_bFirst)
        { // 只运行一帧，就切换到下个状态了
            ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.LOBBY);
            _bFirst = false;
        }
    }

    public override void Exit(bool disabled)
    {
    }
}
