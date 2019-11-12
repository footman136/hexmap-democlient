using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;
using UnityEngine.SceneManagement;

public class GameDisconnectedState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly ClientManager _game;

    private bool isFirst;
    /// <summary>
    /// 当ConnectingLobby的时候，如果失败，则需要回退到logo场景，本状态是过渡，需要切换场景
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="game"></param>
    public GameDisconnectedState(ConnectionStateMachine owner, ClientManager game) : base(owner)
    {
        _game = game;
    }

    public override void Enter()
    {
        SceneManager.LoadScene("Logo");
        isFirst = true;
    }

    public override void Tick()
    {
        if (isFirst)
        {
            isFirst = false;
            ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.START);
        }
    }

    public override void Exit(bool disabled)
    {
    }
}
