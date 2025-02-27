﻿using Assets.Gamelogic.FSM;
using UnityEngine;
using static FSMStateActor;

namespace AI
{
    public class ActorWalkState : FsmBaseState<StateMachineActor, StateEnum>
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
                Owner.TriggerTransition(StateEnum.IDLE);
            }

            vecLast = _actorBehaviour.CurrentPosition;
        }

        public override void Exit(bool disabled)
        {
        }

    }
}