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
        private bool isRest = false;

        public ActorFightState(StateMachineActor owner, ActorBehaviour ab) : base(owner)
        {
            _actorBehaviour = ab;
        }

        public override void Enter()
        {
            _enemyActorId = 0;
            isRest = false;
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
            { // 如果没有找到敌人, 休息
                Owner.TriggerTransition(StateEnum.IDLE);
            }
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

            if (!isRest && Owner.GetLastedTime() > _actorBehaviour.AttackDuration - _actorBehaviour.AttackInterval)
            { // 战斗动画播放结束, 休息一会儿
                isRest = true;
                
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
                IsCounterAttack = _actorBehaviour.IsCounterAttack,
            };
            GameRoomManager.Instance.SendMsg(ROOM.FightStop, output.ToByteArray());
        }
    }
}