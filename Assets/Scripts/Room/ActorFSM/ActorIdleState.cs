using Assets.Gamelogic.FSM;
using static FSMStateActor;

namespace AI
{
    public class ActorIdleState : FsmBaseState<StateMachineActor, StateEnum>
    {
        private readonly ActorBehaviour _actorBehaviour;

        public ActorIdleState(StateMachineActor owner, ActorBehaviour ab) : base(owner)
        {
            _actorBehaviour = ab;
        }

        public override void Enter()
        {
        }

        public override void Tick()
        {
        }

        public override void Exit(bool disabled)
        {
        }
    }
}