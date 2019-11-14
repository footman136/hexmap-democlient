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
        [SerializeField] private float _durationTime; // 状态的持续时间
        public float DurationTime => _durationTime;

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
            var walkGuardState = new ActorWalkGuardState(this, _actorBehaviour);
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
                {StateEnum.WALKGUARD, walkGuardState},
                {StateEnum.GUARD, guardState},
            };

            SetStates(stateList);

            var allowedTransitions = new Dictionary<StateEnum, IList<StateEnum>>();

            allowedTransitions.Add(StateEnum.IDLE, new List<StateEnum>
            {
                StateEnum.IDLE,
                StateEnum.DIE,
                StateEnum.HARVEST,
                StateEnum.FIGHT, // 敌人如果在附近,直接可以进入战斗状态
                StateEnum.WALK,
                StateEnum.WALKFIGHT,
                StateEnum.WALKGUARD,
            });
            allowedTransitions.Add(StateEnum.DIE, new List<StateEnum>
            {
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
                StateEnum.WALKGUARD,
            });
            allowedTransitions.Add(StateEnum.WALK, new List<StateEnum>
            {
                StateEnum.IDLE,
                StateEnum.WALK,
                StateEnum.DIE,
                StateEnum.WALKFIGHT,
                StateEnum.WALKGUARD,
            });
            allowedTransitions.Add(StateEnum.WALKFIGHT, new List<StateEnum>
            {
                StateEnum.IDLE,
                StateEnum.DIE,
                StateEnum.WALK,
                StateEnum.WALKFIGHT,
                StateEnum.FIGHT,
                StateEnum.GUARD,
            });
            allowedTransitions.Add(StateEnum.FIGHT, new List<StateEnum>
            {
                StateEnum.IDLE,
                StateEnum.DIE,
                StateEnum.WALK,
                StateEnum.WALKFIGHT,
                StateEnum.FIGHT,
                StateEnum.WALKGUARD,
                StateEnum.GUARD,
            });
            allowedTransitions.Add(StateEnum.WALKGUARD, new List<StateEnum>
            {
                StateEnum.IDLE,
                StateEnum.DIE,
                StateEnum.WALK,
                StateEnum.FIGHT,
                StateEnum.WALKGUARD,
                StateEnum.GUARD,
            });
            allowedTransitions.Add(StateEnum.GUARD, new List<StateEnum>
            {
                StateEnum.IDLE,
                StateEnum.DIE,
                StateEnum.WALK,
                StateEnum.WALKFIGHT,
                StateEnum.FIGHT,
                StateEnum.WALKGUARD,
                StateEnum.GUARD,
            });
            SetTransitions(allowedTransitions);
        }

        public void TriggerTransition(StateEnum newState, int cellIndex = 0, float durationTime = 0, long actorId = 0)
        {
            if (IsValidTransition(newState))
            {
                SetTarget(cellIndex, actorId);
                var ab = _actorBehaviour;
                TroopAiState output = new TroopAiState()
                {
                    RoomId = ab.RoomId,
                    OwnerId = ab.OwnerId,
                    ActorId = ab.ActorId,
                    State = (int) newState,
                    PosXFrom = ab.PosX,
                    PosZFrom = ab.PosZ,
                    PosXTo = TargetPosX,
                    PosZTo = TargetPosZ,
                    CellIndexFrom = ab.CellIndex,
                    CellIndexTo = TargetCellIndex,
                    TargetId = TargetActorId,
                    Orientation = ab.Orientation,
                    Speed = ab.Speed,
                };
                GameRoomManager.Instance.SendMsg(ROOM.TroopAiState, output.ToByteArray());

                var oldState = CurrentAiState;
                CurrentAiState = newState;

                TransitionTo(newState);

                _startTime = Time.time; // 必须放在后面,因为这个数值大多在Tick()和Exit()里进行判定,而在Enter()里是没用的
                _durationTime = durationTime;

                if (logChanges)
                {
                    Debug.Log(
                        $"ActorStateMachine: State changed from<{oldState}> to<{newState}> - TargetPosition:<{TargetPosX},{TargetPosZ}>");
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
