using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HexmapHelper : MonoBehaviour
{
    public HexGrid hexGrid;
    
    const int mapFileVersion = 5;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool Load(string mapName)
    {
        string path = Path.Combine(Application.persistentDataPath, mapName + ".map");
        if (!File.Exists(path)) {
            Debug.LogError("File does not exist " + path);
            return false;
        }
        using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
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

        return true;
    }
}
