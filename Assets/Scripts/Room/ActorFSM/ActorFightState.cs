using Animation;
using Assets.Gamelogic.FSM;
using UnityEngine;

namespace AI
{
    public class ActorFightState : FsmBaseState<StateMachineActor, FSMStateActor.StateEnum>
    {
        private readonly ActorBehaviour _actorBehaviour;

        public ActorFightState(StateMachineActor owner, ActorBehaviour ab) : base(owner)
        {
            _actorBehaviour = ab;
        }

        public override void Enter()
        {
            
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
                Owner.TriggerTransition(FSMStateActor.StateEnum.GUARD);
                return;
            }

            // 找周围一圈看看有没有敌人
            HexCell current = GameRoomManager.Instance.HexmapHelper.GetCell(_actorBehaviour.CellIndex);
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor.Unit != null)
                {
                    var enemyAv = neighbor.Unit.GetComponent<ActorVisualizer>();
                    if (enemyAv != null)
                    {
                        if (enemyAv.OwnerId != GameRoomManager.Instance.CurrentPlayer.TokenId)
                        { // 所有者不是自己就肯定是敌人,暂时不考虑外交关系
                            
                        }
                    }
                }
            }
        }

        public override void Exit(bool disabled)
        {
        }
    }
}