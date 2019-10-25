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

        private float TIME_DELAY = 1f;
        private float timeSpan = 0;
        private float timeLast = 0;
        private Vector3 vecLast;
        public override void Enter()
        {
            float timeNow = Time.time;
            timeLast = timeNow;
        }

        public override void Tick()
        {
            float timeNow = Time.time;
            timeSpan += timeNow - timeLast;
            timeLast = timeNow;
            if (timeSpan > TIME_DELAY)
            {
                if(_actorBehaviour.CurrentPosition == vecLast)
                {
                    Owner.TriggerTransition(FSMStateActor.StateEnum.IDLE);
                }

                vecLast = _actorBehaviour.CurrentPosition;
                timeSpan = 0;
            }
        }

        public override void Exit(bool disabled)
        {
        }
    }
}