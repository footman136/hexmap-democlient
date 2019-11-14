using Assets.Gamelogic.FSM;
using static FSMStateActor;

namespace AI
{
    public class ActorDieState : FsmBaseState<StateMachineActor, StateEnum>
    {
        private readonly ActorBehaviour _actorBehaviour;
    
        public ActorDieState(StateMachineActor owner, ActorBehaviour ab) : base(owner)
        {
            _actorBehaviour = ab;
        }
    
        public override void Enter()
        {
        }
    
        public override void Tick()
        {
            if (Owner.TimeIsUp())
            {
                Owner.TriggerTransition(StateEnum.VANISH);
            }
        }
    
        public override void Exit(bool disabled)
        {
        }
    }
}