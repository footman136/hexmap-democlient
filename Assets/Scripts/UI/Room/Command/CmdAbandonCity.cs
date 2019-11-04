using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CmdAbandonCity : MonoBehaviour, ICommand
{
    public GameObject Cmd{set;get;}

    public int CanRun ()
    {
        return 1;
    }
    public void Run()
    {
        string title = "提示";
        string content = "你要废弃这座城市吗?"; 
        UIManager.Instance.MessageBox(title, content, (int)PanelMessageBox.BUTTON.YES|(int)PanelMessageBox.BUTTON.NO, OnConfirm);
    }
    public void Stop()
    {
    }

    public void OnConfirm(int button)
    {
        switch ((PanelMessageBox.BUTTON)button)
        {
            case PanelMessageBox.BUTTON.YES:
                Debug.Log("Delete a City!");
                break;
            case PanelMessageBox.BUTTON.NO:
                break;
        }

        Stop();
    }
}
