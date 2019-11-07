using System;
using Assets.Gamelogic.FSM;
using UnityEngine;

namespace AI
{
    public class ActorWalkState : FsmBaseState<StateMachineActor, FSMStateActor.StateEnum>
    {
        private readonly ActorBehaviour _actorBehaviour;

        public ActorWalkState(StateMachineActor owner, ActorBehaviour ab) : base(owner)
        {
            _actorBehaviour = ab;
        }

        public override void Enter()
        {
        }

        private float timeSpan = 0;
        private float TIME_DELAY = 1f;
        private Vector3 vecLast;
        public override void Tick()
        {
            if (timeSpan < TIME_DELAY)
            {
                timeSpan += Time.deltaTime;
                return;
            }
            timeSpan = 0;
            
            if(_actorBehaviour.CurrentPosition == vecLast && _actorBehaviour.CellIndex == Owner.TargetCellIndex)
            {   
                OnArrived();
            }

            vecLast = _actorBehaviour.CurrentPosition;
        }

        public override void Exit(bool disabled)
        {
        }

        public void OnArrived()
        {
            switch (_actorBehaviour.CommandArrived)
            {
                case ActorBehaviour.COMMAND_ARRIVED.FIGHT:
                    Owner.TriggerTransition(FSMStateActor.StateEnum.FIGHT, null, Owner.DurationTime);
                    break;
                case ActorBehaviour.COMMAND_ARRIVED.GUARD:
                    Owner.TriggerTransition(FSMStateActor.StateEnum.GUARD);
                    break;
                case ActorBehaviour.COMMAND_ARRIVED.NONE:
                    Owner.TriggerTransition(FSMStateActor.StateEnum.IDLE);
                    break;
            }
        }
    }
}