using System;
using Assets.Gamelogic.FSM;
using UnityEngine;
using static FSMStateActor;

namespace AI
{
    public class ActorWalkFightState : FsmBaseState<StateMachineActor, StateEnum>
    {
        private readonly ActorBehaviour _actorBehaviour;

        public ActorWalkFightState(StateMachineActor owner, ActorBehaviour ab) : base(owner)
        {
            _actorBehaviour = ab;
        }

        public override void Enter()
        {
        }

        private float timeSpan = 0;
        private const float _TIME_DELAY = 1f;
        private Vector3 vecLast;
        public override void Tick()
        {
            if (timeSpan < _TIME_DELAY)
            {
                timeSpan += Time.deltaTime;
                return;
            }
            timeSpan = 0;

            if (AttackEnemyInRange(_actorBehaviour, Owner.TargetActorId))
                return;
            
            if(_actorBehaviour.CurrentPosition == vecLast && _actorBehaviour.CellIndex == Owner.TargetCellIndex)
            { // 没有敌人的话,到达目的地以后,就休息了  
                Owner.TriggerTransition(StateEnum.IDLE);
            }
            vecLast = _actorBehaviour.CurrentPosition;
        }

        public override void Exit(bool disabled)
        {
        }
    
        public static bool AttackEnemyInRange(ActorBehaviour abMe, long targetId)
        {
            ActorBehaviour abEnemy = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(targetId);
            if (abEnemy == null)
            { // 优先找指定的敌人,如果没有找到,则搜索射程以内的敌人(从近到远)
                abEnemy = abMe.FindEnemyInRange();
            }
        
            if (abEnemy != null)
            {
                if (abMe.IsEnemyInRange(abEnemy))
                {
                    if (abMe.CalcDistance(abEnemy) < 18f * abMe.ShootingRange)
                    { // 敌人进入射程,进入攻击状态. 注意:这时候的敌人,可能不是之前要打的敌人
                        abMe.IsCounterAttack = false; // 这是主动攻击, 不是反击, 记录在自己身上, Stop的时候用
                        abMe.StateMachine.TriggerTransition(StateEnum.FIGHT, abEnemy.CellIndex, abEnemy.ActorId,
                            abMe.AttackDuration);
                        CmdAttack.TryCommand();
                        return true;
                    }
                }
            }
            return false;
        }
    }
}