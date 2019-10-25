using Assets.Gamelogic.FSM;

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

        public override void Tick()
        {
            if(_actorBehaviour.CurrentPosition == _actorBehaviour.TargetPosition)
            {
                Owner.TriggerTransition(FSMStateActor.StateEnum.IDLE);
            }
        }

        public override void Exit(bool disabled)
        {
        }
    }
}