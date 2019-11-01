using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CmdCreateSoldier4 : MonoBehaviour, ICommand
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
