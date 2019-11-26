using System;
using System.Collections.Generic;
using AI;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Google.Protobuf;
using Protobuf.Room;

using static FSMStateActor;

    public class StateMachineActor : FiniteStateMachine<StateEnum>
    {
        public ActorBehaviour _actorBehaviour;
        public StateEnum CurrentAiState;

        // 状态机的参数
        // 时间相关的参数，这两个参数在Enter()的时候尚未生效（可能还是上一个数值），但在Tick()和Exit()的时候仍然有效
        [SerializeField] private float _startTime; // 本状态开始的时间
        [SerializeField] private float _durationTime; // 状态剩余的持续时间
        public float DurationTime => _durationTime;
        [SerializeField] private float _totalTime; // 状态总的持续时间
        public float TotalTime => _totalTime;

        // 目标点坐标的参数，这些参数在Enter()的就已经生效了，Tick()有效，但是在Exit()的时候就失效了（可能已经变成下一个目标点）
        public Vector3 TargetPosition; // 3D精确坐标，等同于transform.localPosition
        public int TargetPosX;
        public int TargetPosZ;
        public int TargetCellIndex;
        public long TargetActorId;

        [SerializeField] private readonly bool logChanges = true;

        public StateMachineActor(ActorBehaviour ab)
        {
            _actorBehaviour = ab;

            var idleState = new ActorIdleState(this, _actorBehaviour);
            var dieState = new ActorDieState(this, _actorBehaviour);
            var vanishState = new ActorVanishState(this, _actorBehaviour);
            var harvestState = new ActorHarvestState(this, _actorBehaviour);
            var walkState = new ActorWalkState(this, _actorBehaviour);
            var walkFightState = new ActorWalkFightState(this, _actorBehaviour);
            var fightState = new ActorFightState(this, _actorBehaviour);
            var delayFightState = new ActorDelayFightState(this, _actorBehaviour);
            var guardState = new ActorGuardState(this, _actorBehaviour);

            var stateList = new Dictionary<StateEnum, IFsmState>
            {
                {StateEnum.IDLE, idleState},
                {StateEnum.DIE, dieState},
                {StateEnum.VANISH, vanishState},
                {StateEnum.HARVEST, harvestState},
                {StateEnum.WALK, walkState},
                {StateEnum.WALKFIGHT, walkFightState},
                {StateEnum.FIGHT, fightState},
                {StateEnum.DELAYFIGHT, delayFightState},
                {StateEnum.GUARD, guardState},
            };

            SetStates(stateList);

            var allowedTransitions = new Dictionary<StateEnum, IList<StateEnum>>();

            allowedTransitions.Add(StateEnum.IDLE, new List<StateEnum>
            {
                StateEnum.IDLE,
                StateEnum.DIE,
                StateEnum.HARVEST,
                StateEnum.WALK,
                StateEnum.WALKFIGHT,
                StateEnum.FIGHT, // 敌人如果在附近,直接可以进入战斗状态
                StateEnum.DELAYFIGHT, // 间隔一定的时间再攻击, 弹药基数足够的情况下,第二次攻击都用此方式
                StateEnum.GUARD,
            });
            allowedTransitions.Add(StateEnum.DIE, new List<StateEnum>
            {
                StateEnum.DIE,
                StateEnum.VANISH,
            });
            allowedTransitions.Add(StateEnum.VANISH, new List<StateEnum>
            {
                StateEnum.NONE,
                StateEnum.VANISH,
            });
            allowedTransitions.Add(StateEnum.HARVEST, new List<StateEnum>
            {
                StateEnum.IDLE,
                StateEnum.DIE,
                StateEnum.WALK,
                StateEnum.WALKFIGHT,
                StateEnum.FIGHT,
                StateEnum.GUARD,
            });
            allowedTransitions.Add(StateEnum.WALK, new List<StateEnum>
            {
                StateEnum.IDLE,
                StateEnum.DIE,
                StateEnum.WALK,
                StateEnum.WALKFIGHT,
                StateEnum.FIGHT,
                StateEnum.DELAYFIGHT,
                StateEnum.GUARD,
            });
            allowedTransitions.Add(StateEnum.WALKFIGHT, new List<StateEnum>
            {
                StateEnum.IDLE,
                StateEnum.DIE,
                StateEnum.WALK,
                StateEnum.WALKFIGHT,
                StateEnum.FIGHT,
                StateEnum.DELAYFIGHT,
                StateEnum.GUARD,
            });
            allowedTransitions.Add(StateEnum.FIGHT, new List<StateEnum>
            {
                StateEnum.IDLE,
                StateEnum.DIE,
                StateEnum.WALK,
                StateEnum.WALKFIGHT,
                StateEnum.FIGHT,
                StateEnum.DELAYFIGHT,
                StateEnum.GUARD,
            });
            allowedTransitions.Add(StateEnum.DELAYFIGHT, new List<StateEnum>
            {
                StateEnum.IDLE,
                StateEnum.DIE,
                StateEnum.WALK,
                StateEnum.WALKFIGHT,
                StateEnum.FIGHT,
                StateEnum.GUARD,
            });
            allowedTransitions.Add(StateEnum.GUARD, new List<StateEnum>
            {
                StateEnum.IDLE,
                StateEnum.DIE,
                StateEnum.WALK,
                StateEnum.WALKFIGHT,
                StateEnum.FIGHT,
                StateEnum.GUARD,
            });
            SetTransitions(allowedTransitions);
        }

        public void TriggerTransition(StateEnum newState, int cellIndex = 0, long actorId = 0, float durationTime = 0, float totalTime = 0)
        {
            if (IsValidTransition(newState))
            {
                SetTarget(cellIndex, actorId);
                var ab = _actorBehaviour;
                ActorAiState output = new ActorAiState()
                {
                    RoomId = ab.RoomId,
                    OwnerId = ab.OwnerId,
                    ActorId = ab.ActorId,
                    AiState = (int) newState,
                    AiCellIndexFrom = ab.CellIndex,
                    AiCellIndexTo = TargetCellIndex,
                    AiTargetId = TargetActorId,
                    Orientation = ab.Orientation,
                    AiDurationTime = durationTime,
                    AiTotalTime = totalTime, 
                };
                GameRoomManager.Instance.SendMsg(ROOM.ActorAiState, output.ToByteArray());

                var oldState = CurrentAiState;
                CurrentAiState = newState;

                TransitionTo(newState);

                _startTime = Time.time; // 必须放在后面,因为这个数值大多在Tick()和Exit()里进行判定,而在Enter()里是没用的
                _durationTime = durationTime;
                _totalTime = totalTime; 

                if (logChanges)
                {
                    Debug.Log(
                        $"ActorStateMachine: <{ab.ActorId}> State changed from<{oldState}> to<{newState}> - TargetPosition:<{TargetPosX},{TargetPosZ}> DurationTime:<{output.AiDurationTime}> -Time:{DateTime.Now.ToLongTimeString()}");
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
        
        #region 参数-目的地

        public void SetTarget(int cellIndex, long actorId)
        {
            TargetActorId = actorId;
            HexCell cellTarget = GameRoomManager.Instance.HexmapHelper.GetCell(cellIndex);
            if (cellIndex == 0) return;
            
            TargetPosition = cellTarget.Position;
            TargetPosX = cellTarget.coordinates.X;
            TargetPosZ = cellTarget.coordinates.Z;
            TargetCellIndex = cellIndex;
        }
        
        #endregion

        #region 参数-时间
        
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

        #endregion
    }
