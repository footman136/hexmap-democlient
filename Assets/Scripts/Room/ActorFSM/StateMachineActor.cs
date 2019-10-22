using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;

public class StateMachineActor : FiniteStateMachine<FSMStateActor.StateEnum>
{
    public RoomLogic _roomLogic;
    public FSMStateActor.StateEnum CurrentAiState { private set; get; }
    public float _startTime; // 本状态开始的时间
    [SerializeField] private readonly bool logChanges = true;

    public  StateMachineActor(RoomLogic roomLogic)
    {
        _roomLogic = roomLogic;
        
        var idleState = new ActorIdleState(this, _roomLogic);
        var walkState = new ActorWalkState(this, _roomLogic);
        var fightState = new ActorFightState(this, _roomLogic);
        var dieState = new ActorDieState(this, _roomLogic);
        
        var stateList = new Dictionary<FSMStateActor.StateEnum, IFsmState>
        {
            { FSMStateActor.StateEnum.IDLE, idleState },
            { FSMStateActor.StateEnum.WALK, walkState },
            { FSMStateActor.StateEnum.FIGHT, fightState },
            { FSMStateActor.StateEnum.DIE, dieState },
        };

        SetStates(stateList);
        
        var allowedTransitions = new Dictionary<FSMStateActor.StateEnum, IList<FSMStateActor.StateEnum>>();

        allowedTransitions.Add(FSMStateActor.StateEnum.IDLE, new List<FSMStateActor.StateEnum>
        {
            FSMStateActor.StateEnum.WALK,
            FSMStateActor.StateEnum.FIGHT,
            FSMStateActor.StateEnum.DIE,
        });
        allowedTransitions.Add(FSMStateActor.StateEnum.WALK, new List<FSMStateActor.StateEnum>
        {
            FSMStateActor.StateEnum.IDLE,
            FSMStateActor.StateEnum.FIGHT,
            FSMStateActor.StateEnum.DIE,
        });
        allowedTransitions.Add(FSMStateActor.StateEnum.FIGHT, new List<FSMStateActor.StateEnum>
        {
            FSMStateActor.StateEnum.IDLE,
            FSMStateActor.StateEnum.WALK,
            FSMStateActor.StateEnum.DIE,
        });
        allowedTransitions.Add(FSMStateActor.StateEnum.DIE, new List<FSMStateActor.StateEnum>
        {
            FSMStateActor.StateEnum.NONE,
        });
        SetTransitions(allowedTransitions);
    }
    
    public void TriggerTransition(FSMStateActor.StateEnum newState)
    {
        if (IsValidTransition(newState))
        {
            var oldState = CurrentAiState;
            CurrentAiState = newState;

            _startTime = Time.time;
            TransitionTo(newState);
            if (logChanges)
            {
                Debug.Log("ActorStateMachine: State changed from<" + oldState + "> to<" + newState + ">");
            }
        }
        else
        {
            Debug.LogErrorFormat("ActorStateMachine: Invalid transition from {0} to {1} detected.",
                CurrentAiState, newState);
        }
    }
    protected override void OnEnableImpl()
    {
        _startTime = Time.time;
    }
    
}
