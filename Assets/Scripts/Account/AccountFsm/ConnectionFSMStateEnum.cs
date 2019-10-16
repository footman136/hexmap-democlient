using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionFSMStateEnum
{
    [global::System.Serializable]
    public enum StateEnum : uint
    {
        NONE = 0,
        START = 1,
        PLAYFAB_LOGIN = 2,
        PLAYFAB_REGISTER = 3,
        CONNECTING = 4,
        CONNECTED = 5,
        PLAYING = 6,
        RESULT = 7,
        DISCONNECTED= 8,
        COUNT = 9,
    }
}
