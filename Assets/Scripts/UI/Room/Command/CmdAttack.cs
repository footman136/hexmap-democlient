using System.Collections;
using System.Collections.Generic;
using AI;
using UnityEngine;
using static FSMStateActor;

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
        var avMe = CommandManager.Instance.CurrentExecuter.CurrentActor;
        if (!avMe)
            return;
        
        HexCell cellTarget = piTarget.CurrentCell;
        if (!cellTarget)
            return;
        
        var abMe = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(avMe.ActorId);
        if (abMe == null)
            return;
        
        ////////////////////
        // 如果目标点在射程以内,则直接攻击
        if (piTarget.CurrentActor)
        {
            var abTarget = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(piTarget.CurrentActor.ActorId);
            if (abMe.IsEnemyInRange(abTarget))
            {
                abMe.StateMachine.TriggerTransition(StateEnum.FIGHT, cellTarget, abMe.AttackDuration, piTarget.CurrentActor.ActorId);
                return;
            }
        }
        
        ////////////////////
        // 敌人不在附近,则需要找过去 
        var hexmapHelper = GameRoomManager.Instance.HexmapHelper;
        var cellMe = avMe.HexUnit.Location;
        cellTarget = hexmapHelper.TryFindADest(avMe.HexUnit.Location, cellTarget);
        
        GameRoomManager.Instance.HexmapHelper.hexGrid.FindPath(cellMe, cellTarget, avMe.HexUnit);
        if (!hexmapHelper.hexGrid.HasPath)
        {// 如果选中的是一个单位，则需要走到该单位的相邻点上去
            Debug.Log($"CmdAttack OnCommandTargetSelected Error - Cannot go to target position:<{cellMe.coordinates.X},{cellMe.coordinates.Z}> ");
            return;
        }

        Debug.Log($"CmdAttack - From<{avMe.PosX},{avMe.PosZ}> - Dest Pos<{cellTarget.coordinates.X},{cellTarget.coordinates.Z}>");

        if (piTarget.CurrentActor && piTarget.CurrentActor.OwnerId != avMe.OwnerId)
        {// 目标点是一支部队,且是敌人的部队,则盯住这支部队猛打
            abMe.StateMachine.TriggerTransition(FSMStateActor.StateEnum.WALKFIGHT, cellTarget, 0, piTarget.CurrentActor.ActorId);
        }
        else
        {// 目标点仅仅是一个位置坐标,则在行军过程中,搜索进攻,发现任意敌人就停下来打它
            abMe.StateMachine.TriggerTransition(FSMStateActor.StateEnum.WALKFIGHT, cellTarget);
        }

        Stop();
    }
}
