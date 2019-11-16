using Assets.Gamelogic.FSM;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;
using static FSMStateActor;

namespace AI
{
    public class ActorDelayFightState : FsmBaseState<StateMachineActor, StateEnum>
    {
        private readonly ActorBehaviour _actorBehaviour;

        private long _enemyActorId;
        private bool hasPlayFight = false;

        public ActorDelayFightState(StateMachineActor owner, ActorBehaviour ab) : base(owner)
        {
            _actorBehaviour = ab;
        }

        public override void Enter()
        {
            hasPlayFight = false;
            _enemyActorId = Owner.TargetActorId;
        }

        private float timeSpan = 0;
        private const float TIME_DELAY = 1f;
        public override void Tick()
        {
            if (timeSpan < TIME_DELAY)
            {
                timeSpan += Time.deltaTime;
                return;
            }
            
            timeSpan = 0;
            
            if (!hasPlayFight && Owner.GetLastedTime() > _actorBehaviour.AttackInterval)
            { // 战斗动画播放结束, 休息一会儿
                hasPlayFight = true;
                TroopPlayAni output = new TroopPlayAni()
                {
                    RoomId = _actorBehaviour.RoomId,
                    OwnerId = _actorBehaviour.OwnerId,
                    ActorId = _actorBehaviour.ActorId,
                    AiState = 2, // FSMStateActor.StateEnum
                };              
                GameRoomManager.Instance.SendMsg(ROOM.TroopPlayAni, output.ToByteArray());
            }

            if (Owner.TimeIsUp())
            {
                Owner.TriggerTransition(StateEnum.IDLE);
            }
        }

        public override void Exit(bool disabled)
        {
            if (_enemyActorId == 0)
                return;
            FightStop output = new FightStop()
            {
                RoomId = GameRoomManager.Instance.RoomId,
                OwnerId = _actorBehaviour.OwnerId,
                ActorId = _actorBehaviour.ActorId,
                TargetId = _enemyActorId,
            };
            GameRoomManager.Instance.SendMsg(ROOM.FightStop, output.ToByteArray());
        }
    }
}