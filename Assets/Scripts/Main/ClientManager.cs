using UnityEngine;

namespace Main
{
    public class ClientManager : MonoBehaviour
    {
        // 状态机
        [SerializeField] private ConnectionStateMachine _stateMachine;
        public ConnectionStateMachine StateMachine => _stateMachine;
    
        // 登陆器
        [SerializeField] private PlayFabLogin _playFab;
        public PlayFabLogin PlayFab => _playFab;

        [SerializeField] private PlayerInfo _playerInfo;
        public PlayerInfo Player => _playerInfo;

        // 客户端网络链接-大厅
        [SerializeField] private GameLobbyManager _lobbyManager;
        public GameLobbyManager LobbyManager => _lobbyManager;

        // 进入房间以前需要准备的数据
        public EnterRoomData EnterRoom;
    
        public static ClientManager Instance { private set; get; }

        void Awake()
        {
            if(Instance != null)
                Debug.LogError("ClientManager is Singleton! Cannot be created again!");
            Instance = this;
            
            _lobbyManager.gameObject.SetActive(false);
            DontDestroyOnLoad(gameObject);
            
            // 初始化全局量
            Utils.InitLogFileName();
        }

        // Start is called before the first frame update
        void Start()
        {
            _stateMachine = new ConnectionStateMachine(this);
            _stateMachine.OnEnable(ConnectionFSMStateEnum.StateEnum.START);
            if (_playFab == null)
                _playFab = GetComponent<PlayFabLogin>();
        }

        private void OnDestroy()
        {
            _stateMachine.OnDisable();
        }

        // Update is called once per frame
        void Update()
        {
            _stateMachine.Tick();
        }
    
    }
    
    public struct EnterRoomData
    {
        public string Address;
        public int Port;
        public bool IsCreatingRoom;
        public int MaxPlayerCount;
        public string RoomName;
        public long RoomId;
        public bool Wrapping;
    };
}
