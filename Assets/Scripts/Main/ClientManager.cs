using UnityEngine;

namespace Main
{
    public class ClientManager : MonoBehaviour
    {
        // 状态机
        private ConnectionStateMachine _stateMachine;
        public ConnectionStateMachine StateMachine => _stateMachine;
    
        // 客户端网络链接-大厅
        [SerializeField] private GameLobbyManager _lobbyManager;
        public GameLobbyManager LobbyManager => _lobbyManager;
    
        // 客户端网络连接-房间
        [SerializeField] private GameRoomManager _roomManager;
        public GameRoomManager RoomManager => _roomManager;
    
        // 登陆器
        [SerializeField] private PlayFabLogin _playFab;
        public PlayFabLogin PlayFab => _playFab;

        [SerializeField] private PlayerInfo _playerInfo;
        public PlayerInfo Player => _playerInfo;

        public static ClientManager Instance { private set; get; }

        void Awake()
        {
            Instance = this;
            _lobbyManager.gameObject.SetActive(false);
            _roomManager.gameObject.SetActive(false);
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
        
        }
    
    }
}
