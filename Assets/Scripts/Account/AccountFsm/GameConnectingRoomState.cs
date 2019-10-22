using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Google.Protobuf;
using Main;
using Protobuf.Lobby;
using UnityEngine.SceneManagement;

public class GameConnectingRoomState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly ClientManager _game;

    public GameConnectingRoomState(ConnectionStateMachine owner, ClientManager game) : base(owner)
    {
        _game = game;
    }

    public override void Enter()
    {
        // Room场景加载的时候，会自动链接 RoomServer
        SceneManager.LoadScene("Room");
    }

    public override void Tick()
    {
    }

    public override void Exit(bool disabled)
    {
        UIManager.Instance.EndConnecting();
    }
}
