using System.Collections;
using System.Collections.Generic;
using Main;
using UnityEngine;

public class PanelBattleResult : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClickExit()
    {
        ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.LOBBY);
    }
}
