using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CmdCreateSoldier3 : MonoBehaviour, ICommand
{
    public GameObject Cmd{set;get;}

    public bool CanRun ()
    {
        PickInfo pi = CommandManager.Instance.CurrentExecuter;
        if (pi.CurrentCity == null)
        {
            return false;
        }

        UrbanCity city = pi.CurrentCity;
        if (city.OwnerId != GameRoomManager.Instance.CurrentPlayer.TokenId)
            return false; // 如果不是我自己的城市，则本指令不显示
        return true;
    }
    public void Run()
    {
    }
    public void Stop()
    {
    }
}
