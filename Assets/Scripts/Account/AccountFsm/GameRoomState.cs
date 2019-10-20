using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;
using UnityEngine.SceneManagement;

public class GameRoomState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly ClientManager _game;

    public GameRoomState(ConnectionStateMachine owner, ClientManager game) : base(owner)
    {
        _game = game;
    }

    public override void Enter()
    {
        GameRoomManager.Instance.CreateJoinRoom(ClientManager.Instance.EnterRoom);
    }

    public override void Tick()
    {
    }

    public override void Exit(bool disabled)
    {
    }
}
