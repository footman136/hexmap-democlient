using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Google.Protobuf;
using Main;
using Protobuf.Lobby;

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
        
        AskCreateRoom data = new AskCreateRoom();
        ClientManager.Instance.LobbyManager.SendMsg(LOBBY.AskCreateRoom, data.ToByteArray());
    }

    public override void Tick()
    {
    }

    public override void Exit(bool disabled)
    {
        UIManager.Instance.EndConnecting();
    }
}
