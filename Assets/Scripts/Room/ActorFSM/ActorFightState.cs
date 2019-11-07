using Animation;
using Assets.Gamelogic.FSM;
using UnityEngine;

namespace AI
{
    public class ActorFightState : FsmBaseState<StateMachineActor, FSMStateActor.StateEnum>
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
            // 找周围一圈看看有没有敌人
            HexCell current = GameRoomManager.Instance.HexmapHelper.GetCell(_actorBehaviour.CellIndex);
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (!neighbor) continue;
                if (!neighbor.Unit) continue;
                var enemyAv = neighbor.Unit.GetComponent<ActorVisualizer>();
                if (enemyAv == null) continue;
                if (enemyAv.OwnerId != GameRoomManager.Instance.CurrentPlayer.TokenId)
                { // 所有者不是自己就肯定是敌人,暂时不考虑外交关系
                    _enemyActorId = enemyAv.ActorId;
                }
            }

            if (_enemyActorId == 0)
            { // 如果没有找到敌人
                Owner.TriggerTransition(FSMStateActor.StateEnum.GUARD);
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
                
                return;
            }

        }

        public override void Exit(bool disabled)
        {
        }
    }
}