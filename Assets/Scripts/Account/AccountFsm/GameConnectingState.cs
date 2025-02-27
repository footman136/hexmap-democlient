﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;

public class GameConnectingState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly ClientManager _game;

    public GameConnectingState(ConnectionStateMachine owner, ClientManager game) : base(owner)
    {
        _game = game;
    }

    public override void Enter()
    {
        UIManager.Instance.BeginLoading();
        UIManager.Instance.BeginConnecting();
        
        // 使用PlayFab链接后台数据库
        // LobbyManager一激活，就会连接服务器
        var lobbyWorker = ClientManager.Instance.LobbyManager.gameObject;
        lobbyWorker.SetActive(true);
    }

    public override void Tick()
    {
    }

    public override void Exit(bool disabled)
    {
        UIManager.Instance.EndConnecting();
    }
}
