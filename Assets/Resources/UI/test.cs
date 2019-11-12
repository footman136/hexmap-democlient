using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //Invoke("SystemTip", 3f);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.A))
        {
            SystemTip();
        }
    }

    void SystemTip()
    {
        //UIManager.Instance.SystemTips("Hello World!", PanelSystemTips.MessageType.Success);
    }
}
