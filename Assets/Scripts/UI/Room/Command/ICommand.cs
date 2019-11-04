using UnityEngine;

public interface ICommand 
{
    GameObject Cmd{set;get;}
    /// <summary>
    /// 该指令是否可以运行
    /// </summary>
    /// <returns>1:正常;0:Disalbe;-1:隐藏本按钮</returns>
    int CanRun();
    void Run();
    void Stop();
}
