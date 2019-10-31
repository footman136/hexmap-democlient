using System.IO;
using Main;
using UnityEngine;
using UnityEngine.UI;
using Google.Protobuf;
using Protobuf.Lobby;

public class PanelNewMapMenu : MonoBehaviour {

	public HexGrid hexGrid;

	public HexMapGenerator mapGenerator;

	[SerializeField] private InputField _name;
	[SerializeField] private InputField _countMax;
	[SerializeField] private HexmapHelper _hexmapHelper;
	[SerializeField] private Toggle _togGen;
	[SerializeField] private Toggle _togWrap;

	bool generateMaps = true;

	bool wrapping = true;
	
	const int mapFileVersion = 5;

	private string _originalRoomName;

	void Awake()
	{
		_originalRoomName = _name.text;
	}
	void Start()
	{
		int nextId = PlayerPrefs.GetInt("RoomNextId");
		_name.text = _originalRoomName + nextId.ToString();
		nextId++;
		PlayerPrefs.SetInt("RoomNextId", nextId);
	}
	

	public void ToggleMapGeneration ()
	{
		generateMaps = _togGen.isOn;
	}

	public void ToggleWrapping () {
		wrapping = _togWrap.isOn;
	}

	public void Open () {
		gameObject.SetActive(true);
		//HexMapCamera.Locked = true;
	}

	public void Close () {
		gameObject.SetActive(false);
		//HexMapCamera.Locked = false;
	}

	public void CreateSmallMap () {
		CreateMap(20, 15);
	}

	public void CreateMediumMap () {
		CreateMap(40, 30);
	}

	public void CreateLargeMap () {
		CreateMap(80, 60);
	}
                       
	void CreateMap (int x, int z) {

		if (string.IsNullOrEmpty(_name.text) || string.IsNullOrEmpty(_countMax.text))
		{
			UIManager.Instance.SystemTips("地图名或最大玩家数不能是空的！", PanelSystemTips.MessageType.Error);
			return;
		}
		if (generateMaps) {
			mapGenerator.GenerateMap(x, z, wrapping);
		}
		else {
			hexGrid.CreateMap(x, z, wrapping);
		}

		Save();
		//HexMapCamera.ValidatePosition();
		Close();
	}

	/// <summary>
	/// 地图网格数据保存为文件，其他数据保存到Redis
	/// </summary>
	void Save()
	{
		string mapName = _name.text;
		int maxPlayerCount = int.Parse(_countMax.text);
		_hexmapHelper.Save(mapName, maxPlayerCount);
		ClientManager.Instance.LobbyManager.Log($"在本地生成地图 - {mapName}");
		
		// 向大厅服务器发送请求加入房间的的信息，让大厅确认是否可以进入
		UIManager.Instance.BeginConnecting();
		AskCreateRoom data = new AskCreateRoom()
		{
			MaxPlayerCount = maxPlayerCount,
		  	RoomName = mapName,
		};
		ClientManager.Instance.LobbyManager.SendMsg(LOBBY.AskCreateRoom, data.ToByteArray());
		Debug.Log("MSG: 询问大厅：是否可以加入房间？");
	}
}