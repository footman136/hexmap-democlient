using UnityEngine;

public interface ICommand 
{
    GameObject Cmd{set;get;}
    bool CanRun();
    void Run();
    void Stop();
}
