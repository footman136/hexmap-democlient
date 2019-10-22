using Assets.Gamelogic.FSM;

public class ActorFightState : FsmBaseState<StateMachineActor, FSMStateActor.StateEnum>
{
    private readonly RoomLogic _roomLogic;

    public ActorFightState(StateMachineActor owner, RoomLogic roomLogic) : base(owner)
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
