using Animation;
using Assets.Gamelogic.FSM;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;
using static FSMStateActor;

namespace AI
{
    public class ActorFightState : FsmBaseState<StateMachineActor, StateEnum>
    {
        private readonly ActorBehaviour _actorBehaviour;

        private long _enemyActorId;

        public ActorFightState(StateMachineActor owner, ActorBehaviour ab) : base(owner)
        {
            _actorBehaviour = ab;
        }

        public override void Enter()
        {
            _enemyActorId = 0;
            ActorBehaviour abEnemy = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(Owner.TargetActorId);
            if (abEnemy != null && _actorBehaviour.IsEnemyInRange(abEnemy))
            {
                _enemyActorId = abEnemy.ActorId;
                FightStart output = new FightStart()
                {
                    RoomId = GameRoomManager.Instance.RoomId,
                    OwnerId = _actorBehaviour.OwnerId,
                    ActorId = _actorBehaviour.ActorId,
                    TargetId = abEnemy.ActorId,
                    // SkillId
                };
                GameRoomManager.Instance.SendMsg(ROOM.FightStart, output.ToByteArray());
            }
            else
            { // 如果没有找到敌人
                Owner.TriggerTransition(StateEnum.GUARD);
            }
        }

        private float timeSpan = 0;
        private float TIME_DELAY = 1f;
        public override void Tick()
        {
            if (timeSpan < TIME_DELAY)
            {
                timeSpan += Time.deltaTime;
                return;
            }
            
            timeSpan = 0;

            if (Owner.TimeIsUp())
            {
                if (_actorBehaviour.AmmuBase > 0)
                {
                    Owner.TriggerTransition(StateEnum.FIGHT);
                }
                else
                {
                    Owner.TriggerTransition(StateEnum.IDLE);
                }
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