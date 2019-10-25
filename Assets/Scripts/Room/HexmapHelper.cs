using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Animation;
using UnityEngine;

public class HexmapHelper : MonoBehaviour
{
    public HexGrid hexGrid;
    
    public Material terrainMaterial;
    
    const int mapFileVersion = 5;

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
    /// <returns></returns>
    public bool CreateUnit (long roomId, long ownerId, long actorId, int posX, int posZ, float orientation, string unitName)
    {
        HexCell cell = GetCell(posX, posZ);
        if (cell && !cell.Unit)
        {
            string unitPathName = $"Arts/Prefabs/Client/{unitName}";
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
                    }

                    if (!GameRoomManager.Instance.RoomLogic.ActorManager.AllActors.ContainsKey(actorId))
                    {
                        GameRoomManager.Instance.RoomLogic.ActorManager.AddActor(roomId, ownerId, actorId, posX, posZ, orientation, unitName, hu);
                    }
                    GameRoomManager.Instance.Log($"MSG: CreateATroopReply - 创建了一个Actor - {unitName}");
                    return true;
                }
            }
        }
        else
        {
            GameRoomManager.Instance.Log($"HexmapHelper ：创建Actor失败！原来这个格子没有物体，现在有了物体 - <{posX},{posZ}> - {unitName}");
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

    public void DoMove (long actorId, int posXFrom, int posZFrom, int posXTo, int posZTo, float speed)
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
                            hu.Travel(listPath, speed);
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
                }
            }
        }
    }

    #endregion
}
