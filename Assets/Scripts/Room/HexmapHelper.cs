using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
    }

    public HexCell GetCell(int posX, int posZ)
    {
        return hexGrid.GetCell(new HexCoordinates(posX, posZ));
    }

    public HexCell GetCell(Vector3 position)
    {
        return hexGrid.GetCell(position);
    }

    public HexCell GetCell(int cellIndex)
    {
        return hexGrid.GetCell(cellIndex);
    }

    HexCell currentCell;
    HexUnit selectedUnit;
    bool UpdateCurrentCell () {
        HexCell cell =
            hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        if (cell != currentCell) {
            currentCell = cell;
            return true;
        }
        return false;
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
                    neighbor.Walled = true;
                    neighbor.UrbanLevel = Random.Range(2,3);
                    neighbor.ClearRes();
                    neighbor.IncreaseVisibility();
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
    /// <returns></returns>
    public bool CreateUnit (long roomId, long ownerId, long actorId, int posX, int posZ, float orientation, string unitName, int cellIndex, int actorInfoId,
        string name, int hp, float attackPower, float defencePower, float speed, float filedOfVision, float shootingRange)
    {
        HexCell cell = GetCell(posX, posZ);
        if (!cell)
        {
            GameRoomManager.Instance.Log($"HexmapHelper CreateUnit ：创建Actor失败！格子越界 - <{posX},{posZ}> - {unitName}");
            return false;
        }

        if (cell.Unit)
        {
            HexCell newCell = null;
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = cell.GetNeighbor(d);
                if (!neighbor.Unit)
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
                return false;
            }
        }
        string unitPathName = $"Arts/BeffioPrefabs/Soldiers/{unitName}";
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
                    av.AttackPower = attackPower;
                    av.DefencePower = defencePower;
                    av.Speed = speed;
                    av.FieldOfVision = filedOfVision;
                    av.ShootingRange = shootingRange;
                }

                // 关闭预制件上没用的东西，看以后这东西能否用得上，如果没用，就完全干掉
                hu.GetComponentInChildren<ThirdPersonUserControl>().enabled = false;
                hu.GetComponentInChildren<ThirdPersonCharacter>().enabled = false;
                hu.GetComponentInChildren<CapsuleCollider>().enabled = false;
                EnableFollowCamera(hu, false);

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
                        AttackPower = attackPower,
                        DefencePower = defencePower,
                        Speed = speed,
                        FieldOfVision = filedOfVision,
                        ShootingRange = shootingRange,
                    };
                    GameRoomManager.Instance.RoomLogic.ActorManager.AddActor(ab, hu);
                }
                GameRoomManager.Instance.Log($"MSG: CreateATroopReply - 创建了一个Actor - {unitName}");
                return true;
            }
        }

        return false;
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

    public void DoMove (long actorId, int posXFrom, int posZFrom, int posXTo, int posZTo)
    {
        if (ActorVisualizer.AllActors.ContainsKey(actorId))
        {
            var av = ActorVisualizer.AllActors[actorId];
            if (av != null)
            {
                var hu = av.GetComponent<HexUnit>();
                if (hu != null)
                {
                    HexCell hcFrom = GetCell(posXFrom, posZFrom);
                    HexCell hcTo = GetCell(posXTo, posZTo);
                    if (hu.IsValidDestination(hcTo))
                    {
                        hexGrid.FindPath(hcFrom, hcTo, hu);
                        if (hexGrid.HasPath)
                        {
                            List<HexCell> listPath = hexGrid.GetPath();
                            Debug.Log($"DoMove: From<{hcFrom.coordinates.X},{hcFrom.coordinates.Z}> - To<{hcTo.coordinates.X},{hcTo.coordinates.Z}>");
                            Debug.Log($"DoMove: From<{hcFrom.Position.x},{hcFrom.Position.z}> - To<{hcTo.Position.x},{hcTo.Position.z}>");
                            hu.Travel(listPath, av.Speed);
                        }
                    }
                }
            }
        }
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

    public void EnableFollowCamera(HexUnit unit, bool bEnable)
    {
        if (!unit)
            return;
        unit.transform.Find("CameraFollow").gameObject.SetActive(bEnable);
    }

    #endregion
}
