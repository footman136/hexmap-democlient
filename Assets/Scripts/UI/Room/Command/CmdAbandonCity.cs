using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CmdAbandonCity : MonoBehaviour, ICommand
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
