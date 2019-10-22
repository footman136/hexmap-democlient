
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
        LOBBY = 6,
        ROOM = 7,
        RESULT = 8,
        DISCONNECTED = 9,
        CONNECTING_ROOM = 10,
        CONNECTED_ROOM = 11,
        DISCONNECTED_ROOM = 12,
        COUNT = 13,
    }
}
