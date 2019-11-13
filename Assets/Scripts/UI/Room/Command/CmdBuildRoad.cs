using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CmdBuildRoad : MonoBehaviour, ICommand
{
    public GameObject Cmd{set;get;}

    public int CanRun ()
    {
        return 1;
    }
    public void Run()
    {
        UIManager.Instance.SystemTips("本功能尚未开放!", PanelSystemTips.MessageType.Info);
    }
    public void Tick()
    {
    }
    public void Stop()
    {
    }
}
