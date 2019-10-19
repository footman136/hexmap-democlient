using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;

public class GameConnectedState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly ClientManager _game;

    public GameConnectedState(ConnectionStateMachine owner, ClientManager game) : base(owner)
    {
        _game = game;
    }

    // 暂时本状态没太大用，仅仅是作为过渡
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
