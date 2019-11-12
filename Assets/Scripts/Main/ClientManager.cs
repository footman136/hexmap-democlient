using UnityEngine;
using GameUtils;
using System.Collections;

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

        [HideInInspector]
        public CsvDataManager CsvDataManager;

        // 进入房间以前需要准备的数据
        public EnterRoomData EnterRoom;
    
        public static ClientManager Instance { private set; get; }

        void Awake()
        {
            if (Instance != null)
            {
                // 如果从大厅回退到登录，代码还是会走到这里，只能手动删除一下，这个错误也改为一个警告。
                // 因为这是无法避免的。
                Debug.LogWarning("ClientManager is Singleton! Cannot be created again!");
                DestroyImmediate(Instance.gameObject);
            }

            Instance = this;
            
            _lobbyManager.gameObject.SetActive(false);
            DontDestroyOnLoad(gameObject);
            
            // 初始化全局量
            Utils.InitLogFileName();
            
            // 初始化数据表
            CsvDataManager = gameObject.AddComponent<CsvDataManager>();
            StartCoroutine(DownloadDataFiles());
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
            Instance = null;
        }

        // Update is called once per frame
        void Update()
        {
            _stateMachine.Tick();
        }

        IEnumerator DownloadDataFiles()
        {
            yield return StartCoroutine(CsvDataManager.LoadDataAllAndroid());
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
