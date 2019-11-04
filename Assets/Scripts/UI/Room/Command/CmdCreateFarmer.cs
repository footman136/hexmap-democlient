using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;

public class CmdCreateFarmer : MonoBehaviour, ICommand
{
    public GameObject Cmd{set;get;}

    public int CanRun ()
    {
        return 1;
    }
    public void Run()
    {
        PickInfo pi = CommandManager.Instance.CurrentExecuter;
        if (pi.CurrentCity == null)
            return;
        UrbanCity city = pi.CurrentCity;

        HexCell cellCenter = GameRoomManager.Instance.HexmapHelper.GetCell(city.CellIndex);// 城市中心地块
        if (cellCenter.Unit != null)
        {
            string msg = $"当前位置有一支部队，请把该部队移走，然后再生产部队！";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Warning);
            return;
        }

        var actorInfoTable = CsvDataManager.Instance.GetTable("actor_info");
        string artPrefab = actorInfoTable.GetValue(10001, "ArtPrefab");
        
        CreateATroop output = new CreateATroop()
        {
            RoomId = GameRoomManager.Instance.RoomId,
            OwnerId = GameRoomManager.Instance.CurrentPlayer.TokenId,
            ActorId = GameUtils.Utils.GuidToLongId(),
            PosX = city.PosX,
            PosZ = city.PosZ,
            Orientation = Random.Range(0f, 360f),
            Species = artPrefab, // 预制件的名字
            CellIndex = city.CellIndex,
            ActorInfoId = 10001,
        };
        GameRoomManager.Instance.SendMsg(ROOM.CreateAtroop, output.ToByteArray());
        Stop();
    }
    public void Stop()
    {
    }
}
