using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;
using UnityEngine.SceneManagement;//头部引入

public class GameLobbyState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly ClientManager _game;

    public GameLobbyState(ConnectionStateMachine owner, ClientManager game) : base(owner)
    {
        _game = game;
    }

    public override void Enter()
    {
        UIManager.Instance.EndLoading();
        SceneManager.LoadScene("Lobby");
    }

    public override void Tick()
    {
    }

    public override void Exit(bool disabled)
    {
    }
}
