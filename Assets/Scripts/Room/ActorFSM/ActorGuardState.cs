using Assets.Gamelogic.FSM;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;
using static FSMStateActor;

namespace AI
{
    /// <summary>
    /// 警戒状态有两个功能:1-定时恢复[弹药基数AmmoBase]; 2-如果附近有敌人,主动攻击(AI)
    /// </summary>
    public class ActorGuardState : FsmBaseState<StateMachineActor, StateEnum>
    {
        private readonly ActorBehaviour _actorBehaviour;

        private const float TIME_SUPPLY = 10f; // 间隔一定时间,恢复AmmoBase 

        public ActorGuardState(StateMachineActor owner, ActorBehaviour ab) : base(owner)
        {
            _actorBehaviour = ab;
        }

        public override void Enter()
        {
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

            // 1-恢复[弹药基数AmmoBase]
            if (Owner.GetLastedTime() >= TIME_SUPPLY)
            {
                Owner.RestartTime();
                if (_actorBehaviour.AmmoBase < _actorBehaviour.AmmoBaseMax)
                {
                    _actorBehaviour.AmmoBase++;
                    AmmoSupply output = new AmmoSupply()
                    {
                        RoomId = _actorBehaviour.RoomId,
                        OwnerId = _actorBehaviour.OwnerId,
                        ActorId = _actorBehaviour.ActorId,
                        AmmoBase = _actorBehaviour.AmmoBase,
                    };
                    GameRoomManager.Instance.SendMsg(ROOM.AmmoSupply, output.ToByteArray());
                }
            }
            
            // 2-寻找附近的敌人, 如果有, 且有弹药, 就干他
//            var abEnemy = _actorBehaviour.FindEnemyInRange();
//            if (abEnemy != null && _actorBehaviour.AmmoBase > 0)
//            {
//                _actorBehaviour.StateMachine.TriggerTransition(StateEnum.FIGHT, 0, 0, abEnemy.ActorId);
//            }
        }

        public override void Exit(bool disabled)
        {
        }
    }
}