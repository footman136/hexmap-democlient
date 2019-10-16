using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;

public class GamePlayFabRegisterState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly ClientManager _game;

    private GameObject _panelConnecting;

    public GamePlayFabRegisterState(ConnectionStateMachine owner, ClientManager game) : base(owner)
    {
        _game = game;
    }

    public override void Enter()
    {
        _panelConnecting = UIManager.CreatePanel(UIManager.Instance.RootLobby, "", "UI/Lobby/PanelConnecting");
    }

    public override void Tick()
    {
    }

    public override void Exit(bool disabled)
    {
        if (_panelConnecting != null)
        {
            UIManager.DestroyPanel(ref _panelConnecting);
        }
    }
}
