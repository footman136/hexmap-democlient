using System.Collections;
using System.Collections.Generic;
using AI;
using UnityEngine;

public class CmdAttack : MonoBehaviour, ICommand
{
    public GameObject Cmd{set;get;}

    public int CanRun ()
    {
        return 1;
    }
    public void Run()
    {
        CursorManager.Instance.ShowCursor(CursorManager.CURSOR_TYPE.ATTACK);
        CommandManager.Instance.CommandTargetSelected += OnCommandTargetSelected;
        if (Cmd)
        {
            var ci = Cmd.GetComponent<CommandItem>();
            if (ci)
                ci.Select(true);
        }
    }
    public void Tick()
    {
    }
    public void Stop()
    {
        CursorManager.Instance.ShowCursor(CursorManager.CURSOR_TYPE.NONE);
        CommandManager.Instance.CommandTargetSelected -= OnCommandTargetSelected;
        if (!Cmd) return;
        var ci = Cmd.GetComponent<CommandItem>();
        if (ci)
            ci.Select(false);
    }
    
    private void OnCommandTargetSelected(PickInfo piTarget)
    {
        var av = CommandManager.Instance.CurrentExecuter.CurrentActor;
        if (!av)
            return;
        
        HexCell cellTarget = piTarget.CurrentCell;
        if (!cellTarget)
            return;
        
        var ab = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(av.ActorId);
        if (ab == null)
            return;
        
        var currentCell = av.HexUnit.Location;
        GameRoomManager.Instance.HexmapHelper.hexGrid.FindPath(currentCell, cellTarget, av.HexUnit);
        var hexmapHelper = GameRoomManager.Instance.HexmapHelper;
        if (!hexmapHelper.hexGrid.HasPath)
            return;

        Debug.Log($"CmdAttack - Dest<{cellTarget.coordinates.X},{cellTarget.coordinates.Z}> - Dest Pos<{cellTarget.coordinates.X},{cellTarget.coordinates.Z}>");
        ab.CommandArrived = ActorBehaviour.COMMAND_ARRIVED.FIGHT; // 移动结束后,进入战斗状态
        ab.StateMachine.TriggerTransition(FSMStateActor.StateEnum.WALK, cellTarget, 30f);
        
        Stop();
    }
}
