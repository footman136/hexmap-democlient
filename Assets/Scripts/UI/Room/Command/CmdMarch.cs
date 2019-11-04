using System.Collections;
using System.Collections.Generic;
using Animation;
using Google.Protobuf;
using Protobuf.Room;
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
        PanelRoomMain.Instance.ShowCursor(PanelRoomMain.CURSOR_TYPE.FIND_PATH);
        CommandManager.Instance.CommandTargetSelected += OnCommandTargetSelected;
        if (Cmd)
        {
            var ci = Cmd.GetComponent<CommandItem>();
            if (ci)
                ci.Select(true);
        }
    }
    public void Stop()
    {
        PanelRoomMain.Instance.ShowCursor(PanelRoomMain.CURSOR_TYPE.NONE);
        CommandManager.Instance.CommandTargetSelected -= OnCommandTargetSelected;
        if (Cmd)
        {
            var ci = Cmd?.GetComponent<CommandItem>();
            if (ci)
                ci.Select(false);
        }
    }

    private void OnCommandTargetSelected(PickInfo piTarget)
    {
        HexUnit whoMove = CommandManager.Instance.CurrentExecuter.CurrentUnit;
        if (!whoMove)
            return;
        HexCell cellTarget = piTarget.CurrentCell;
        if (!cellTarget)
            return;
        {
            GameRoomManager.Instance.HexmapHelper.hexGrid.FindPath(whoMove.Location, cellTarget, whoMove);
            if (!GameRoomManager.Instance.HexmapHelper.hexGrid.HasPath)
                return;
            var av = whoMove.GetComponent<ActorVisualizer>();
            if (av == null)
                return;

            TroopMove output = new TroopMove()
            {
                RoomId = GameRoomManager.Instance.RoomId,
                OwnerId = GameRoomManager.Instance.CurrentPlayer.TokenId,
                ActorId = av.ActorId,
                PosFromX = av.PosX,
                PosFromZ = av.PosZ,
                PosToX = cellTarget.coordinates.X,
                PosToZ = cellTarget.coordinates.Z,
            };
            GameRoomManager.Instance.SendMsg(ROOM.TroopMove, output.ToByteArray());
        }
        Stop();
    }
}
