using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AI;
using Animation;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;
using Random = UnityEngine.Random;
using GameUtils;

public class HexmapHelper : MonoBehaviour
{
    public HexGrid hexGrid;
    public HexMapCamera hexCamera;
    
    public Material terrainMaterial;
    
    const int mapFileVersion = 7;

    #region 初始化
    
    void Awake()
    {
        // 这一行，查了两个小时。。。如果没有，打包客户端后，地表看不到任何颜色，都是灰色。
        Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
        terrainMaterial.DisableKeyword("GRID_ON");
        HexGameUI.CurrentCamera = Camera.main;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    #endregion
    
    #region 地图
    
    string GetSelectedPath (string nameInput) {
        string mapName = nameInput;
        if (mapName.Length == 0) {
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    public void Save(string mapName, int countMax)
    {
        string path = Path.Combine(Application.persistentDataPath, mapName + ".map");
        using (
            BinaryWriter writer =
                new BinaryWriter(File.Open(path, FileMode.Create))
        ) {
            writer.Write(mapFileVersion);
            hexGrid.Save(writer);
        }
		
        Debug.Log("MSG: 询问大厅：是否可以加入房间？");
    }
    
    public bool Load(string mapName)
    {
        string path = GetSelectedPath(mapName);
        try
        {
            BinaryReader reader = new BinaryReader(File.OpenRead(path));
            int header = reader.ReadInt32();
            if (header <= mapFileVersion)
            {
                hexGrid.Load(reader, header);
                HexMapCamera.ValidatePosition();
            }
            else {
                Debug.LogWarning("Unknown map format " + header);
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Exception - Hexmap Load file failed - {e}");
            return false;
        }
        return true;
    }

    public BinaryReader BeginLoadBuffer(string mapName)
    {
        string path = GetSelectedPath(mapName);
        if (!File.Exists(path))
            return null;
        BinaryReader reader = null;
        try
        {
            reader = new BinaryReader(File.OpenRead(path));
        }
        catch (Exception e)
        {
            Debug.Log($"Exception - Hexmap BeginLoadBuffer file failed - {e}");
        }

        return reader;
    }
    public bool LoadBuffer(BinaryReader reader, out byte[] bytes, ref int size, ref bool isFileEnd)
    {
        try
        {
            isFileEnd = false;
            long remain = reader.BaseStream.Length - reader.BaseStream.Position;
            if( remain < size)
            {
                size = (int)remain;
            }
            bytes = reader.ReadBytes(size);
            if (reader.BaseStream.Position == reader.BaseStream.Length)
                isFileEnd = true;
        }
        catch (Exception e)
        {
            bytes = null;
            Debug.Log($"Exception - Hexmap Loaduffer file failed - {e}");
            return false;
        }
        return true;
    }

    public void EndLoadBuffer(ref BinaryReader reader)
    {
        reader.Close();
        //reader = null;
    }

    public BinaryWriter BeginSaveBuffer(string mapName)
    {
        string path = GetSelectedPath(mapName);

        try
        {
            BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create));
            return writer;
        }
        catch (Exception e)
        {
            Debug.Log($"Exception - Hexmap BeginSaveBuffer open file failed - {mapName}");
        }

        return null;
    }

    public void SaveBuffer(BinaryWriter writer, byte[] bytes)
    {
         writer.Write(bytes);
    }

    public void EndSaveBuffer(ref BinaryWriter writer)
    {
        writer.Close();
        //writer = null;
    }
    
    #endregion

    #region 工具
    
    HexCell GetCellUnderCursor () {
        return
            hexGrid.GetCell(HexGameUI.CurrentCamera.ScreenPointToRay(Input.mousePosition));
    }

    public HexCell GetCell(Vector3 position)
    {
        return hexGrid.GetCell(position);
    }

    public HexCell GetCell(int cellIndex)
    {
        if (cellIndex < 0 || cellIndex >= hexGrid.cellCountX * hexGrid.cellCountZ)
        {
            Debug.LogError($"HexmapHelper GetCell Error - index is out of range : {cellIndex} - should less than {hexGrid.cellCountX * hexGrid.cellCountZ}");
        }
        return hexGrid.GetCell(cellIndex);
    }

    HexCell currentCell;
    HexUnit selectedUnit;
    bool UpdateCurrentCell () {
        HexCell cell =
            hexGrid.GetCell(HexGameUI.CurrentCamera.ScreenPointToRay(Input.mousePosition));
        if (cell != currentCell) {
            currentCell = cell;
            return true;
        }
        return false;
    }

    #endregion
    
    #region 射程
    
    /// <summary>
    /// 递归得到距离自己一定范围内的所有Cell
    /// </summary>
    /// <param name="current">当前点，以该点为中心</param>
    /// <param name="range">距离，格子数——间隔几个格子</param>
    /// <returns>得到的结果从近到远排列</returns>
    public List<HexCell> GetCellsInRange(HexCell current, int range)
    {
        List<HexCell> cellList = getCellsInRange(current, current, range);
        cellList.Sort((a, b)=>sortByCellDistance(a, b, current));
        return cellList;
    }

    private int sortByCellDistance(HexCell a, HexCell b, HexCell center)
    {
        float dist1 = Vector3.SqrMagnitude(a.Position - center.Position);
        float dist2 = Vector3.SqrMagnitude(b.Position - center.Position);
        return (int)(dist1 - dist2);
    }

    private List<HexCell> getCellsInRange(HexCell center, HexCell current, int range)
    {
        List<HexCell> findCells = new List<HexCell>();
        if (range == 0)
        {
            findCells.Add(current);
            return findCells;
        }

        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = current.GetNeighbor(d);
            if (!neighbor) continue;
            var findCells2 = getCellsInRange(center, neighbor, range - 1);
            findCells = findCells.Union(findCells2).ToList();
        }

        return findCells;
    }
    
    #endregion
    
    #region 最近可到达点
    
    /// <summary>
    /// 递归查询距离给定目标点current附近，距离地址点最近的有效目标点（没有单位在上面）
    /// </summary>
    /// <param name="from"></param>
    /// <param name="current"></param>
    /// <returns></returns>
    public HexCell TryFindADest(HexCell from, HexCell to)
    {
        if (to.Unit == null)// 如果当前目标点上没有单位，则直接返回该点
            return to;
        List<HexCell> Neighbors = new List<HexCell>();
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = to.GetNeighbor(d);
            if (!neighbor) continue;
            Neighbors.Add(neighbor);
        }
        Neighbors.Sort((a, b) => sortByCellDistance(a, b, from));
        foreach (var cell in Neighbors)
        {
            if (cell.Unit == null) // 邻居如果有空位,就是找到了
                return cell;
        }

        // 从最近的位置继续往下找
        return TryFindADest(from, Neighbors[0]);
    }
    
    #endregion

    #region 城市
        
    public void AddCity(int cellIndex, int citySize, bool isMyCity)
    {
        HexCell current = hexGrid.GetCell(cellIndex);
        if (current)
        {
            current.Walled = true;
            current.UrbanLevel = 3;
            current.ClearRes(); // 把城内的其它资源都干掉
            current.IncreaseVisibility();
            if (citySize == 1) // 大号城市，大了一圈
            {
                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    HexCell neighbor = current.GetNeighbor(d);
                    if (neighbor)
                    {
                        neighbor.Walled = true;
                        neighbor.UrbanLevel = Random.Range(2, 3);
                        neighbor.ClearRes();
                        neighbor.IncreaseVisibility();
                    }
                }
            }
        }
    }

    public void RemoveCity(int cellIndex, int citySize)
    {
        HexCell current = hexGrid.GetCell(cellIndex);
        if (current)
        {
            current.Walled = false;
            current.UrbanLevel = 0;
            current.ResetVisibility();
            if (citySize == 1)// 大号城市
            {
                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    HexCell neighbor = current.GetNeighbor(d);
                    neighbor.Walled = false;
                    neighbor.UrbanLevel = 0;
                    neighbor.ResetVisibility();
                }
            }
        }
    }

    public void SetCameraPosition(int cellIndex)
    {
        HexCell current = hexGrid.GetCell(cellIndex);
        hexCamera.SetPosition(current);
    }

    public void SetCameraPosition(Vector3 pos)
    {
        hexCamera.SetPosition(pos);
    }

    public Vector3 GetCameraPosition()
    {
        return hexCamera.GetPosition();
    }

    #endregion
    
    #region 单元

    /// <summary>
    /// 
    /// </summary>
    /// <param name="roomId">房间id</param>
    /// <param name="ownerId">所属玩家id</param>
    /// <param name="actorId">自己的id</param>
    /// <param name="posX"></param>
    /// <param name="posZ"></param>
    /// <param name="orientation"></param>
    /// <param name="unitName"></param>
    /// <param name="cellIndex">该单位在地形块HexCell中的Index，因为根据PosX,PosZ可能得不到正确的cell，只能用这个数据确保正确</param>
    /// <param name="actorInfoId">兵种ID，在actor_info表中的id</param>
    /// <param name="name"></param>
    /// <param name="hp"></param>
    /// <param name="attackPower"></param>
    /// <param name="defencePower"></param>
    /// <param name="speed"></param>
    /// <param name="filedOfVision"></param>
    /// <param name="shootingRange"></param>
    /// <param name="attackDuration"></param>
    /// <param name="attackInterval"></param>
    /// <param name="ammoBase"></param>
    /// <param name="ammoBaseMax"></param>
    /// <returns></returns>
    public ActorVisualizer CreateUnit (long roomId, long ownerId, long actorId, int posX, int posZ, float orientation, 
        string unitName, int cellIndex, int actorInfoId,
        string name, int hp, int hpMax, float attackPower, float defencePower, float speed, float filedOfVision, float shootingRange,
        float attackDuration, float attackInterval, int ammoBase, int ammoBaseMax)
    {
        HexCell cell = GetCell(cellIndex);
        if (!cell)
        {
            GameRoomManager.Instance.Log($"HexmapHelper CreateUnit ：创建Actor失败！格子越界 - <{posX},{posZ}> - {unitName}");
            return null;
        }

        if (cellIndex == 0)
        {
            Debug.LogError("HexmapHelper CreateUnit Error - CellIndex is lost!!!");
            return null;
        }

        if (cell.Unit)
        {
            HexCell newCell = null;
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = cell.GetNeighbor(d);
                if (neighbor && !neighbor.Unit)
                {
                    newCell = neighbor;
                    break;
                }
            }

            if (newCell)
            {
                GameRoomManager.Instance.Log($"HexmapHelper ：创建Actor失败！物体位置重合了,重新放置! - 原坐标:<{posX},{posZ}> - 新坐标:<{newCell.coordinates.X},{newCell.coordinates.Z}> - {unitName}");
                cell = newCell;
            }
            else
            {
                GameRoomManager.Instance.Log($"HexmapHelper ：创建Actor失败！原来这个格子没有物体，现在有了物体, 附近也没有空地! - <{posX},{posZ}> - {unitName}");
                return null;
            }
        }
        
        // 区分颜色
        string newUnitName = unitName;
        if (ownerId != GameRoomManager.Instance.CurrentPlayer.TokenId)
        { // 如果不是自己，蓝色就换成红色（绿色换成黄色）
            if (unitName.IndexOf("BLUE") > 0)
            {
                newUnitName = unitName.Replace("BLUE", "RED");
            }
            else if (unitName.IndexOf("GREEN") > 0)
            {
                newUnitName = unitName.Replace("GREEN", "YELLOW");
            }
        }

        string unitPathName = $"Arts/BeffioPrefabs/Soldiers/{newUnitName}";
        var go = Resources.Load<HexUnit>(unitPathName);
        if (go != null)
        {
            HexUnit hu = Instantiate(go);
            if (hu != null)
            {
                hexGrid.AddUnit(hu, cell, orientation);
                var av = hu.GetComponent<ActorVisualizer>();
                if (av != null)
                {
                    av.RoomId = roomId;
                    av.OwnerId = ownerId;
                    av.ActorId = actorId;
                    av.PosX = posX;
                    av.PosZ = posZ;
                    av.Orientation = orientation;
                    av.Species = unitName;
                    av.CellIndex = cellIndex;
                    av.ActorInfoId = actorInfoId;
                    
                    av.Name = name;
                    av.Hp = hp;
                    av.HpMax = hpMax;
                    av.AttackPower = attackPower;
                    av.DefencePower = defencePower;
                    av.Speed = speed;
                    av.FieldOfVision = filedOfVision;
                    av.ShootingRange = shootingRange;

                    av.AttackDuration = attackDuration;
                    av.AttackInterval = attackInterval;
                    av.AmmoBase = ammoBase;
                    av.AmmoBaseMax = ammoBaseMax;
                }

                // 关闭预制件上没用的东西，看以后这东西能否用得上，如果没用，就完全干掉
                if(hu.GetComponentInChildren<ThirdPersonUserControl>())
                    hu.GetComponentInChildren<ThirdPersonUserControl>().enabled = false;
                if(hu.GetComponentInChildren<ThirdPersonCharacter>())
                    hu.GetComponentInChildren<ThirdPersonCharacter>().enabled = false;
                if(hu.GetComponentInChildren<CapsuleCollider>())
                    hu.GetComponentInChildren<CapsuleCollider>().enabled = false;
                EnableFollowCamera(av, false);

                if (!GameRoomManager.Instance.RoomLogic.ActorManager.AllActors.ContainsKey(actorId))
                {
                    ActorBehaviour ab = new ActorBehaviour()
                    {
                        RoomId = roomId,
                        OwnerId = ownerId,
                        ActorId = actorId,
                        PosX = posX,
                        PosZ = posZ,
                        Orientation = orientation,
                        Species = unitName,
                        CellIndex = cellIndex,
                        ActorInfoId = actorInfoId,
                    
                        Name = name,
                        Hp = hp,
                        HpMax = hpMax,
                        AttackPower = attackPower,
                        DefencePower = defencePower,
                        Speed = speed,
                        FieldOfVision = filedOfVision,
                        ShootingRange = shootingRange,
                        
                        AttackDuration = attackDuration,
                        AttackInterval = attackInterval,
                        AmmoBase = ammoBase,
                        AmmoBaseMax = ammoBaseMax,
                    };
                    GameRoomManager.Instance.RoomLogic.ActorManager.AddActor(ab, hu);
                }
                GameRoomManager.Instance.Log($"MSG: CreateATroopReply - 创建了一个Actor - {unitName}");
                return av;
            }
        }

        return null;
    }

    public bool DestroyUnit (long actorId) 
    {
        GameRoomManager.Instance.RoomLogic.ActorManager.RemoveActor(actorId);
        if (ActorVisualizer.AllActors.ContainsKey(actorId))
        {
            var av = ActorVisualizer.AllActors[actorId];
            if (av != null)
            {
                var hu = av.GetComponent<HexUnit>();
                if (hu != null)
                {
                    GameRoomManager.Instance.Log($"MSG: DestroyATroopReply -  销毁了一个Actor - {av.Species}");
                    hexGrid.RemoveUnit(hu);
                    return true;
                }
                
            }
        }

        return false;
    }

    public List<HexCell> DoMove (long actorId, int cellIndexFrom, int cellIndexTo)
    {
        if (ActorVisualizer.AllActors.ContainsKey(actorId))
        {
            var av = ActorVisualizer.AllActors[actorId];
            if (av != null)
            {
                var hu = av.GetComponent<HexUnit>();
                if (hu != null)
                {
                    HexCell hcFrom = GetCell(cellIndexFrom);
                    HexCell hcTo = GetCell(cellIndexTo);
                    if (hu.IsValidDestination(hcTo))
                    {
                        hexGrid.FindPath(hcFrom, hcTo, hu);
                        if (hexGrid.HasPath)
                        {
                            List<HexCell> listPath = hexGrid.GetPath();
                            Debug.Log($"DoMove: From<{hcFrom.coordinates.X},{hcFrom.coordinates.Z}> - To<{hcTo.coordinates.X},{hcTo.coordinates.Z}>");
                            Debug.Log($"DoMove: From<{hcFrom.Position.x},{hcFrom.Position.z}> - To<{hcTo.Position.x},{hcTo.Position.z}>");
                            hu.Travel(listPath, av.Speed);
                            
                            // 把路线备份一份出来
                            List<HexCell> returnPath = ListPool<HexCell>.Get();
                            returnPath.Clear();
                            foreach (var current in listPath)
                            {
                                returnPath.Add(current);
                            }
                            return returnPath;
                        }
                    }
                }
            }
        }
        return null;
    }

    public void Stop(long actorId)
    {
        if (ActorVisualizer.AllActors.ContainsKey(actorId))
        {
            var av = ActorVisualizer.AllActors[actorId];
            if (av != null)
            {
                var hu = av.GetComponent<HexUnit>();
                if (hu != null)
                {
                    hu.Stop();
                    hexGrid.ClearPath();
                }
            }
        }
    }

    public void LookAt(long actorId, Vector3 position)
    {
        if (ActorVisualizer.AllActors.ContainsKey(actorId))
        {
            var av = ActorVisualizer.AllActors[actorId];
            if (av != null)
            {
                var hu = av.GetComponent<HexUnit>();
                if (hu != null)
                {
                    StartCoroutine(hu.LookAt(position));
                }
            }
        }
    }

    public void EnableFollowCamera(ActorVisualizer av, bool bEnable)
    {
        if (av == null)
            return;
        av.transform.Find("CameraFollow").gameObject.SetActive(bEnable);
    }

    public void ShowPath(ActorVisualizer av)
    {
        if (av != null && av.ListPath != null && av.ListPath.Count > 0)
        {
            hexGrid.ShowPath(av.ListPath);
        }
        else
        {
            hexGrid.ShowPath(null);
        }
    }

    #endregion
}
