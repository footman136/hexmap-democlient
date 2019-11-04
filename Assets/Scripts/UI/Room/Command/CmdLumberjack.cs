using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CmdLumberjack : MonoBehaviour, ICommand
{
    public GameObject Cmd{set;get;}

    public int CanRun ()
    {
        return 1;
    }
    public void Run()
    {
    }
    public void Stop()
    {
    }
}
