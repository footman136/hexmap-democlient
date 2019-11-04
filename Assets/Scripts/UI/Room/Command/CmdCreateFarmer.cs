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
        PanelRoomMain.Instance.AskCreateUnit(city, 10001);

        Stop();
    }
    public void Stop()
    {
    }
}
