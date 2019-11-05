using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CmdHalt : MonoBehaviour, ICommand
{
    public GameObject Cmd{set;get;}

    public int CanRun ()
    {
        return 1;
    }
    public void Run()
    {
        var whoMove = CommandManager.Instance.CurrentExecuter.CurrentActor;
        if (!whoMove)
            return;
        var av = whoMove;
        var ab = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(av.ActorId);
        ab?.StateMachine.TriggerTransition(FSMStateActor.StateEnum.IDLE); 
    }
    public void Stop()
    {
    }
}
