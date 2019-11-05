using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CmdCreateSoldier2 : MonoBehaviour, ICommand
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
        PanelRoomMain.Instance.AskCreateUnit(city, 10011);
    }
    public void Tick()
    {
    }
    public void Stop()
    {
    }
}
