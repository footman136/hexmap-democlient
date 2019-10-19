using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;

/// <summary>
/// 客户端管理游戏状态的状态机 
/// </summary>
public class ConnectionStateMachine : FiniteStateMachine<ConnectionFSMStateEnum.StateEnum>
{
    private ClientManager _game;
    public ConnectionFSMStateEnum.StateEnum CurrentState { private set; get; }
    public float _startTime; // 本状态开始的时间
    [SerializeField] private bool logChanges = true;
    
    public ConnectionStateMachine(ClientManager game)
    {
        _game = game;
            
        var startState = new GameStartState(this, _game);
        var playFabLoginState = new GamePlayFabLoginState(this, _game);
        var playFabRegisterState = new GamePlayFabRegisterState(this, _game);
        var connectingState = new GameConnectingState(this, _game);
        var connectedState = new GameConnectedState(this, _game);
        var disconnectedState = new GameDisconnectedState(this, _game);
        var resultState = new GameResultState(this, _game);
        var lobbyState = new GameLobbyState(this, _game);
        var roomState = new GameRoomState(this, _game);
        var connectingRoomState = new GameConnectingRoomState(this, _game);
        var connectedRoomState = new GameConnectedRoomState(this, _game);

        var stateList = new Dictionary<ConnectionFSMStateEnum.StateEnum, IFsmState>
        {
            {ConnectionFSMStateEnum.StateEnum.START, startState},
            {ConnectionFSMStateEnum.StateEnum.PLAYFAB_LOGIN, playFabLoginState},
            {ConnectionFSMStateEnum.StateEnum.PLAYFAB_REGISTER, playFabRegisterState},
            {ConnectionFSMStateEnum.StateEnum.CONNECTING, connectingState},
            {ConnectionFSMStateEnum.StateEnum.CONNECTED, connectedState},
            {ConnectionFSMStateEnum.StateEnum.DISCONNECTED, disconnectedState},
            {ConnectionFSMStateEnum.StateEnum.RESULT, resultState},
            {ConnectionFSMStateEnum.StateEnum.LOBBY, lobbyState},
            {ConnectionFSMStateEnum.StateEnum.ROOM, roomState},
            {ConnectionFSMStateEnum.StateEnum.CONNECTING_ROOM, connectingRoomState},
            {ConnectionFSMStateEnum.StateEnum.CONNECTED_ROOM, connectedRoomState},
        };
        SetStates(stateList);
        
        var allowedTransitions = new Dictionary<ConnectionFSMStateEnum.StateEnum, IList<ConnectionFSMStateEnum.StateEnum>>();

        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.START, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.PLAYFAB_LOGIN,
            ConnectionFSMStateEnum.StateEnum.PLAYFAB_REGISTER,
        });
        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.PLAYFAB_LOGIN, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.START,
            ConnectionFSMStateEnum.StateEnum.CONNECTING,
        });
        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.PLAYFAB_REGISTER, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.START,
            ConnectionFSMStateEnum.StateEnum.CONNECTING,
        });
        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.CONNECTING, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.CONNECTED,
            ConnectionFSMStateEnum.StateEnum.DISCONNECTED,
        });
        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.CONNECTED, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.LOBBY,
        });
        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.DISCONNECTED, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.LOBBY,
        });
        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.RESULT, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.LOBBY,
        });
        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.LOBBY, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.START,
            ConnectionFSMStateEnum.StateEnum.CONNECTING_ROOM,
        });
        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.CONNECTING_ROOM, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.LOBBY,
            ConnectionFSMStateEnum.StateEnum.CONNECTED_ROOM,
        });
        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.CONNECTED_ROOM, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.ROOM,
        });
        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.ROOM, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.LOBBY,
            ConnectionFSMStateEnum.StateEnum.RESULT,
        });
        SetTransitions(allowedTransitions);
    }
    public void TriggerTransition(ConnectionFSMStateEnum.StateEnum newState)
    {
        if (IsValidTransition(newState))
        {
            var oldState = CurrentState; 
            CurrentState = newState;

            _startTime = Time.time;
            TransitionTo(newState);
            if (logChanges)
            {
                Debug.Log("DinoStateMachine: State changed from<" + oldState + "> to<" + newState + ">");
            }
        }
        else
        {
            Debug.LogErrorFormat("DinoStateMachine: Invalid transition from {0} to {1} detected.",
                CurrentState, newState);
        }
    }
    protected override void OnEnableImpl()
    {
        _startTime = Time.time;
    }
    
}
