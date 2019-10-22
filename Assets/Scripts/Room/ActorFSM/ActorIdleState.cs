using Assets.Gamelogic.FSM;

public class ActorIdleState : FsmBaseState<StateMachineActor, FSMStateActor.StateEnum>
{
    private readonly RoomLogic _roomLogic;

    public ActorIdleState(StateMachineActor owner, RoomLogic roomLogic) : base(owner)
    {
        _roomLogic = roomLogic;
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
