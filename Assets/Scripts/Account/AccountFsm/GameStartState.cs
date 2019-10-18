using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;

public class GameStartState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly ClientManager _game;

    private GameObject _panelLogin;

    public GameStartState(ConnectionStateMachine owner, ClientManager game) : base(owner)
    {
        _game = game;
    }

    public override void Enter()
    {
        UIManager.Instance.EndLoading();
        if (_panelLogin == null)
            _panelLogin = UIManager.CreatePanel(UIManager.Instance.Root, "", "UI/Logo/PanelLogin");
        if(_panelLogin == null)
            Debug.LogError("GameStartState Enter failed! PanelLogin create failed!!!");
        else
        {
            var panelLogin = _panelLogin.GetComponent<PanelLogin>();
            if (panelLogin != null)
            {
                panelLogin.Show(true);
                panelLogin.ShowRegister(false);
            }
        }
    }

    public override void Tick()
    {
    }

    public override void Exit(bool disabled)
    {
        if (_panelLogin != null)
        {
            UIManager.DestroyPanel(ref _panelLogin);
        }
    }
}
