using Assets.Gamelogic.FSM;
using static FSMStateActor;

namespace AI
{
    public class ActorGuardState : FsmBaseState<StateMachineActor, StateEnum>
    {
        private readonly ActorBehaviour _actorBehaviour;

        public ActorGuardState(StateMachineActor owner, ActorBehaviour ab) : base(owner)
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