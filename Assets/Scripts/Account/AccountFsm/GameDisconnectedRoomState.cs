using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;
using UnityEngine.SceneManagement;

public class GameDisconnectedRoomState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly ClientManager _game;

    /// <summary>
    /// 当Connecting房间服务器的时候，如果发现错误，需要退回到Lobby场景，本状态是过渡，，需要切换场景
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="game"></param>
    public GameDisconnectedRoomState(ConnectionStateMachine owner, ClientManager game) : base(owner)
    {
        _game = game;
    }

    public override void Enter()
    {
        SceneManager.LoadScene("Lobby");
    }

    public override void Tick()
    {
    }

    public override void Exit(bool disabled)
    {
    }
}
