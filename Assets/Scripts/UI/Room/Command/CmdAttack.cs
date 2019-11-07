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
        cellTarget = TryFindANeighbor(av.HexUnit.Location, cellTarget);
        GameRoomManager.Instance.HexmapHelper.hexGrid.FindPath(currentCell, cellTarget, av.HexUnit);
        var hexmapHelper = GameRoomManager.Instance.HexmapHelper;
        if (!hexmapHelper.hexGrid.HasPath)
        {// 如果选中的是一个单位，则需要走到该单位的相邻点上去
            Debug.Log($"CmdAttack OnCommandTargetSelected Error - Cannot go to target position:<{currentCell.coordinates.X},{currentCell.coordinates.Z}> ");
            return;
        }

        Debug.Log($"CmdAttack - From<{av.PosX},{av.PosZ}> - Dest Pos<{cellTarget.coordinates.X},{cellTarget.coordinates.Z}>");
        ab.CommandArrived = ActorBehaviour.COMMAND_ARRIVED.FIGHT; // 移动结束后,进入战斗状态
        ab.StateMachine.TriggerTransition(FSMStateActor.StateEnum.WALK, cellTarget, 30f);
        
        Stop();
    }

    private class NeighborCell
    {
        public float _distance;
        public HexCell _cell;
    }

    /// <summary>
    /// 递归查询距离给定目标点current附近，距离地址点最近的有效目标点（没有单位在上面）
    /// </summary>
    /// <param name="from"></param>
    /// <param name="current"></param>
    /// <returns></returns>
    private HexCell TryFindANeighbor(HexCell from, HexCell current)
    {
        if (current.Unit == null)// 如果这个目标点上没有单位，则直接返回该点
            return current;
        List<NeighborCell> Neighbors = new List<NeighborCell>();
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = current.GetNeighbor(d);
            if (!neighbor) continue;
            float dist = Vector3.Distance(neighbor.Position, @from.Position);
            NeighborCell ncell = new NeighborCell()
            {
                _cell = neighbor,
                _distance = dist,
            };
            Neighbors.Add(ncell);
        }
        Neighbors.Sort((a, b) => (int)(a._distance - b._distance));
        foreach (var ncell in Neighbors)
        {
            var findCell = TryFindANeighbor(from, ncell._cell);
            if (findCell != null)
                return findCell;
        }

        return null;
    }
}
