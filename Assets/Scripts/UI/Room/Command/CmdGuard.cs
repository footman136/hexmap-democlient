using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CmdGuard : MonoBehaviour, ICommand
{
    public GameObject Cmd{set;get;}

    public int CanRun ()
    {
        return 1;
    }
    public void Run()
    {
        var avMe = CommandManager.Instance.CurrentExecuter.CurrentActor;
        if (!avMe)
            return;
        var abMe = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(avMe.ActorId);
        if (abMe == null)
            return;
        abMe.StateMachine.TriggerTransition(FSMStateActor.StateEnum.GUARD);
        Stop();

    }
    public void Tick()
    {
    }
    public void Stop()
    {
    }
}
