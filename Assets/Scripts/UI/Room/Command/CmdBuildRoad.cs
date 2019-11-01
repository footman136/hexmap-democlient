using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CmdBuildRoad : MonoBehaviour, ICommand
{
    public GameObject Cmd{set;get;}

    public bool CanRun ()
    {
        return true;
    }
    public void Run()
    {
    }
    public void Stop()
    {
    }
}
