using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;

public class GameConnectedState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly ClientManager _game;

    public GameConnectedState(ConnectionStateMachine owner, ClientManager game) : base(owner)
    {
        _game = game;
    }

    private bool _bFirst;
    public override void Enter()
    {
        var go = UIManager.CreatePanel(UIManager.Instance.RootInGame, "", "UI/InGame/PanelCommandMenu");
        if (go != null)
        {
//            UIManager.Instance.CommandMenu = go.GetComponent<PanelCommandMenu>();
//            UIManager.Instance.CommandMenu.enabled = true; // 不知道是谁总是禁止它，所以不得不每次手动打开
//            if (UIManager.Instance.CommandMenu == null)
//            {
//                Debug.LogError("GameConnectedState Enter() - In Game Command Menu creation is failed!!!");
//            }
        }

        _bFirst = true;
    }

    public override void Tick()
    {
        if (_bFirst)
        { // 只运行一帧，就切换到下个状态了
            ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.PLAYING);
            _bFirst = false;
        }
    }

    public override void Exit(bool disabled)
    {
    }
}
