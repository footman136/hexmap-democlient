using AI;
using UnityEngine;

public class CmdMarch : MonoBehaviour, ICommand
{
    public GameObject Cmd{set;get;}

    public int CanRun ()
    {
        return 1;
    }
    public void Run()
    {
        CursorManager.Instance.ShowCursor(CursorManager.CURSOR_TYPE.FIND_PATH);
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
        // 看看行动点够不够
        if (!CmdAttack.IsActionPointGranted())
        {
            string msg = "行动点数不够, 本操作无法执行! ";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            Debug.Log("CmdMarch OnCommandTargetSelected Error - " + msg);
            Stop();
            return;
        }
        
        var av = CommandManager.Instance.CurrentExecuter.CurrentActor;
        if (!av)
            return;
        
        HexCell cellTarget = piTarget.CurrentCell;
        if (!cellTarget)
            return;
        
        {
            var ab = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(av.ActorId);
            if (ab == null)
                return;
            
            var currentCell = av.HexUnit.Location;
            GameRoomManager.Instance.HexmapHelper.hexGrid.FindPath(currentCell, cellTarget, av.HexUnit);
            var hexmapHelper = GameRoomManager.Instance.HexmapHelper;
            if (!hexmapHelper.hexGrid.HasPath)
                return;

//            TroopMove output = new TroopMove()
//            {
//                RoomId = GameRoomManager.Instance.RoomId,
//                OwnerId = GameRoomManager.Instance.CurrentPlayer.TokenId,
//                ActorId = av.ActorId,
//                PosFromX = av.PosX,
//                PosFromZ = av.PosZ,
//                PosToX = cellTarget.coordinates.X,
//                PosToZ = cellTarget.coordinates.Z,
//            };
//            GameRoomManager.Instance.SendMsg(ROOM.TroopMove, output.ToByteArray());
            
            HexCell newCell = hexmapHelper.hexGrid.GetCell(currentCell.coordinates.X, currentCell.coordinates.Z);
            HexCell newCell2 = hexmapHelper.hexGrid.GetCell(currentCell.Position);
            if (newCell.Position != currentCell.Position)
            {
                Debug.LogWarning($"OhNo Hexmap!!! - Orgin<{currentCell.coordinates.X},{currentCell.coordinates.Z}> - New<{newCell.coordinates.X},{newCell.coordinates.Z}>");
            }
            if (newCell2.Position != currentCell.Position)
            {
                Debug.LogWarning($"OhNo Hexmap 2!!! - Orgin<{currentCell.coordinates.X},{currentCell.coordinates.Z}> - New2<{newCell2.coordinates.X},{newCell2.coordinates.Z}>");
            }
        
            Debug.Log($"CmdMarch - From<{av.PosX},{av.PosZ}> - Dest<{cellTarget.coordinates.X},{cellTarget.coordinates.Z}>");
            ab.StateMachine.TriggerTransition(FSMStateActor.StateEnum.WALK, cellTarget.Index);
        }
        Stop();
        // 消耗行动点 
        CmdAttack.TryCommand();
    }
}
