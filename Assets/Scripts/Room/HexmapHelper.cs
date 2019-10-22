using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HexmapHelper : MonoBehaviour
{
    public HexGrid hexGrid;
    
    const int mapFileVersion = 5;

    void Awake()
    {
        // 这一行，查了两个小时。。。如果没有，打包客户端后，地表看不到任何颜色，都是灰色。
        Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
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
}
