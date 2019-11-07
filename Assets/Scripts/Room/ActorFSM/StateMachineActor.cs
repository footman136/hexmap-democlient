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
    
    // 状态机的参数
    // 时间相关的参数，这两个参数在Enter()的时候尚未生效（可能还是上一个数值），但在Tick()和Exit()的时候仍然有效
    [SerializeField] private float _startTime; // 本状态开始的时间
    [SerializeField] private float _durationTime; // 状态的持续时间
    public float DurationTime => _durationTime;
    // 目标点坐标的参数，这些参数在Enter()的就已经生效了，Tick()有效，但是在Exit()的时候就失效了（可能已经变成下一个目标点）
    public Vector3 TargetPosition; // 3D精确坐标，等同于transform.localPosition
    public int TargetPosX;
    public int TargetPosZ;
    public int TargetCellIndex;
    
    [SerializeField] private readonly bool logChanges = true;

    public  StateMachineActor(ActorBehaviour ab)
    {
        _actorBehaviour = ab;
        
        var idleState = new ActorIdleState(this, _actorBehaviour);
        var walkState = new ActorWalkState(this, _actorBehaviour);
        var fightState = new ActorFightState(this, _actorBehaviour);
        var dieState = new ActorDieState(this, _actorBehaviour);
        var vanishState = new ActorDieState(this, _actorBehaviour);
        var harvestState = new ActorHarvestState(this, _actorBehaviour);
        var guardState = new ActorGuardState(this, _actorBehaviour);
        
        var stateList = new Dictionary<FSMStateActor.StateEnum, IFsmState>
        {
            { FSMStateActor.StateEnum.IDLE, idleState },
            { FSMStateActor.StateEnum.WALK, walkState },
            { FSMStateActor.StateEnum.FIGHT, fightState },
            { FSMStateActor.StateEnum.DIE, dieState },
            { FSMStateActor.StateEnum.VANISH, vanishState },
            { FSMStateActor.StateEnum.HARVEST, harvestState },
            { FSMStateActor.StateEnum.GUARD, guardState },
        };

        SetStates(stateList);
        
        var allowedTransitions = new Dictionary<FSMStateActor.StateEnum, IList<FSMStateActor.StateEnum>>();

        allowedTransitions.Add(FSMStateActor.StateEnum.IDLE, new List<FSMStateActor.StateEnum>
        {
            FSMStateActor.StateEnum.IDLE,
            FSMStateActor.StateEnum.WALK,
            FSMStateActor.StateEnum.FIGHT,
            FSMStateActor.StateEnum.DIE,
            FSMStateActor.StateEnum.HARVEST,
            FSMStateActor.StateEnum.GUARD,
        });
        allowedTransitions.Add(FSMStateActor.StateEnum.WALK, new List<FSMStateActor.StateEnum>
        {
            FSMStateActor.StateEnum.IDLE,
            FSMStateActor.StateEnum.WALK,
            FSMStateActor.StateEnum.FIGHT,
            FSMStateActor.StateEnum.DIE,
            FSMStateActor.StateEnum.GUARD,
        });
        allowedTransitions.Add(FSMStateActor.StateEnum.FIGHT, new List<FSMStateActor.StateEnum>
        {
            FSMStateActor.StateEnum.IDLE,
            FSMStateActor.StateEnum.WALK,
            FSMStateActor.StateEnum.FIGHT,
            FSMStateActor.StateEnum.DIE,
            FSMStateActor.StateEnum.GUARD,
        });
        allowedTransitions.Add(FSMStateActor.StateEnum.GUARD, new List<FSMStateActor.StateEnum>
        {
            FSMStateActor.StateEnum.IDLE,
            FSMStateActor.StateEnum.WALK,
            FSMStateActor.StateEnum.FIGHT,
            FSMStateActor.StateEnum.DIE,
        });
        allowedTransitions.Add(FSMStateActor.StateEnum.HARVEST, new List<FSMStateActor.StateEnum>
        {
            FSMStateActor.StateEnum.IDLE,
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
    
    public void TriggerTransition(FSMStateActor.StateEnum newState, HexCell cellTarget = null, float durationTime = 0)
    {
        if (IsValidTransition(newState))
        {
            SetTarget(cellTarget);
            var ab = _actorBehaviour;
            TroopAiState output = new TroopAiState()
            {
                RoomId = ab.RoomId,
                OwnerId = ab.OwnerId,
                ActorId = ab.ActorId,
                State = (int)newState,
                PosXFrom = ab.PosX,
                PosZFrom = ab.PosZ,
                PosXTo = TargetPosX,
                PosZTo = TargetPosZ,
                CellIndexFrom = ab.CellIndex,
                CellIndexTo = TargetCellIndex,
                Orientation = ab.Orientation,
                Speed = ab.Speed,
            };
            GameRoomManager.Instance.SendMsg(ROOM.TroopAiState,output.ToByteArray());

            var oldState = CurrentAiState;
            CurrentAiState = newState;

            TransitionTo(newState);
            
            _startTime = Time.time; // 必须放在后面,因为这个数值大多在Tick()和Exit()里进行判定,而在Enter()里是没用的
            _durationTime = durationTime;
            
            if (logChanges)
            {
                Debug.Log($"ActorStateMachine: State changed from<{oldState}> to<{newState}> - TargetPosition:<{TargetPosX},{TargetPosZ}>");
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

    public void SetTarget(HexCell cellTarget)
    {
        if (!cellTarget)
            return;
        TargetPosition = cellTarget.Position;
        TargetPosX = cellTarget.coordinates.X;
        TargetPosZ = cellTarget.coordinates.Z;
        TargetCellIndex = cellTarget.Index;
    }
        
    public void RestartTime()
    {
        _startTime = Time.time;
    }

    public bool TimeIsUp()
    {
        return Time.time - _startTime > _durationTime;
    }

    public float GetLastedTime()
    {
        return Time.time - _startTime;
    }
    
}
