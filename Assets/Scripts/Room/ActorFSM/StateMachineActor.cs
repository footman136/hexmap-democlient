using System.Collections.Generic;
using AI;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Google.Protobuf;
using Protobuf.Room;

public class StateMachineActor : FiniteStateMachine<FSMStateActor.StateEnum>
{
    public ActorBehaviour _actorBehaviour;
    public FSMStateActor.StateEnum CurrentAiState;
    public float _startTime; // 本状态开始的时间
    [SerializeField] private readonly bool logChanges = true;

    public  StateMachineActor(ActorBehaviour ab)
    {
        _actorBehaviour = ab;
        
        var idleState = new ActorIdleState(this, _actorBehaviour);
        var walkState = new ActorWalkState(this, _actorBehaviour);
        var fightState = new ActorFightState(this, _actorBehaviour);
        var dieState = new ActorDieState(this, _actorBehaviour);
        var vanishState = new ActorDieState(this, _actorBehaviour);
        
        var stateList = new Dictionary<FSMStateActor.StateEnum, IFsmState>
        {
            { FSMStateActor.StateEnum.IDLE, idleState },
            { FSMStateActor.StateEnum.WALK, walkState },
            { FSMStateActor.StateEnum.FIGHT, fightState },
            { FSMStateActor.StateEnum.DIE, dieState },
            { FSMStateActor.StateEnum.VANISH, vanishState },
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
            FSMStateActor.StateEnum.WALK,
            FSMStateActor.StateEnum.FIGHT,
            FSMStateActor.StateEnum.DIE,
        });
        allowedTransitions.Add(FSMStateActor.StateEnum.FIGHT, new List<FSMStateActor.StateEnum>
        {
            FSMStateActor.StateEnum.IDLE,
            FSMStateActor.StateEnum.WALK,
            FSMStateActor.StateEnum.FIGHT,
            FSMStateActor.StateEnum.DIE,
        });
        allowedTransitions.Add(FSMStateActor.StateEnum.DIE, new List<FSMStateActor.StateEnum>
        {
            FSMStateActor.StateEnum.VANISH,
        });
        allowedTransitions.Add(FSMStateActor.StateEnum.VANISH, new List<FSMStateActor.StateEnum>
        {
            FSMStateActor.StateEnum.NONE,
        });
        SetTransitions(allowedTransitions);
    }
    
    public void TriggerTransition(FSMStateActor.StateEnum newState)
    {
        if (IsValidTransition(newState))
        {
            var ab = _actorBehaviour;
            TroopAiState output = new TroopAiState()
            {
                RoomId = ab.RoomId,
                OwnerId = ab.OwnerId,
                ActorId = ab.ActorId,
                State = (int)newState,
                PosFromX = ab.PosX,
                PosFromZ = ab.PosZ,
                PosToX = ab.TargetPosX,
                PosToZ = ab.TargetPosZ,
                Orientation = ab.Orientation,
                Speed = 0.5f,
            };
            GameRoomManager.Instance.SendMsg(ROOM.TroopAiState,output.ToByteArray());

            var oldState = CurrentAiState;
            CurrentAiState = newState;

            _startTime = Time.time;
            TransitionTo(newState);
            if (logChanges)
            {
                Debug.Log($"ActorStateMachine: State changed from<{oldState}> to<{newState}> - TargetPosition:<{ab.TargetPosX},{ab.TargetPosZ}>");
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
