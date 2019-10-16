using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;

public class GameConnectingState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly ClientManager _game;

    private GameObject _clientWorker;
    private GameObject _panelConnecting;
    
    public GameConnectingState(ConnectionStateMachine owner, ClientManager game) : base(owner)
    {
        _game = game;
    }

    public override void Enter()
    {
        _panelConnecting = UIManager.CreatePanel(UIManager.Instance.RootLobby, "", "UI/Lobby/PanelConnecting");
        
        // 使用PlayFab链接后台数据库
        // LobbyManager一激活，就会连接服务器
        _clientWorker = ClientManager.Instance.LobbyManager.gameObject;
        _clientWorker.SetActive(true);
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
