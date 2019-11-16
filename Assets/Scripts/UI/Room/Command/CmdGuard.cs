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
        var avMe = CommandManager.Instance.CurrentExecuter.CurrentActor;
        if (!avMe)
            return;
        var avTarget = piTarget.CurrentActor;
        
        HexCell cellTarget = piTarget.CurrentCell;
        if (!cellTarget)
            return;
        
        var abMe = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(avMe.ActorId);
        if (abMe == null)
            return;
        
        // 如果目标点就是现在的位置,直接进入驻守(警戒)状态
        if (cellTarget == CommandManager.Instance.CurrentExecuter.CurrentCell)
        {
            abMe.StateMachine.TriggerTransition(FSMStateActor.StateEnum.GUARD);
            Stop();
            return;
        }
        
        var hexmapHelper = GameRoomManager.Instance.HexmapHelper;
        var currentCell = avMe.HexUnit.Location;
        cellTarget = hexmapHelper.TryFindADest(avMe.HexUnit.Location, cellTarget);
        GameRoomManager.Instance.HexmapHelper.hexGrid.FindPath(currentCell, cellTarget, avMe.HexUnit);
        if (!hexmapHelper.hexGrid.HasPath)
        {// 如果选中的是一个单位，则需要走到该单位的相邻点上去
            Debug.Log($"CmdAttack OnCommandTargetSelected Error - Cannot go to target position:<{currentCell.coordinates.X},{currentCell.coordinates.Z}> ");
            return;
        }

        Debug.Log($"CmdAttack - From<{avMe.PosX},{avMe.PosZ}> - Dest Pos<{cellTarget.coordinates.X},{cellTarget.coordinates.Z}>");
        abMe.StateMachine.TriggerTransition(FSMStateActor.StateEnum.WALKGUARD, cellTarget.Index, 30f);
        
        Stop();
    }
}
