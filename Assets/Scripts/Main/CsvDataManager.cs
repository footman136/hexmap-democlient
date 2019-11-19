using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using UnityEngine;
using GameUtils;

public class CsvDataManager : MonoBehaviour
{
    public static CsvDataManager Instance;
    private Dictionary<string, CsvStreamReader> _liStreamReaders = new Dictionary<string, CsvStreamReader>();
    
    void Awake()
    {
        if (Instance)
        {
            Debug.LogError("CsvDataManager is singlon, cannot be initialized more than once!");
        }
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //    ————————————————
    //    版权声明：本文为CSDN博主「WSHBlog」的原创文章，遵循 CC 4.0 BY-SA 版权协议，转载请附上原文出处链接及本声明。
    //    原文链接：https://blog.csdn.net/u014076894/article/details/39082343        
    private string GetDataPath()
    {
        string folderName = "/Data";  
        string filePath = 
#if UNITY_ANDROID && !UNITY_EDITOR
        "jar:file://" + Application.dataPath + "!/assets/" + folderName + "/";
#elif UNITY_IPHONE && !UNITY_EDITOR
        Application.dataPath + "/Raw/" + folderName + "/";
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR
        "file://" + Application.dataPath + "/StreamingAssets" + "/" + folderName + "/";
#else
        string.Empty;
#endif

        return filePath;
    }
    
    public void LoadDataAll()
    {
        string dataPath = Application.streamingAssetsPath + "/Data";
        List<string> lstDataFiles = new List<string>();
        Utils.GetDir(dataPath, "*.csv", ref lstDataFiles);
        
        Debug.Log($"ScvDataManager LoadDataAll Info - {lstDataFiles.Count} data file found! directory:{dataPath}");
        
        foreach (var fullname in lstDataFiles)
        {
            
            int index = fullname.LastIndexOf('/');
            int index2 = fullname.LastIndexOf('.');
            string nakedName = fullname.Substring(index+1, index2-index-1);
            CsvStreamReader csv = new CsvStreamReader(fullname, System.Text.Encoding.UTF8);
            _liStreamReaders.Add(nakedName, csv);
        }
    }

    public IEnumerator LoadDataAllAndroid()
    {
        string dataPath = Application.streamingAssetsPath + "/Data";
        //List<string> lstDataFiles = new List<string>();
        //Utils.GetDir(dataPath, "*.csv", ref lstDataFiles);
        string[] files = {"server_config_client","actor_info","command_id","command_set" };
        List<string> lstDataFiles = files.Select(file => dataPath + "/" + file + ".csv").ToList();

        Debug.Log($"ScvDataManager LoadDataAllAndroid Info - {lstDataFiles.Count} data file found! directory:{dataPath}");
        
        foreach (var fullname in lstDataFiles)
        {
            int index = fullname.LastIndexOf('/');
            int index2 = fullname.LastIndexOf('.');
            string nakedName = fullname.Substring(index+1, index2-index-1);
            var www = new WWW(fullname);
            yield return www;
            if (www.isDone && www.error == null)
            {
                Debug.Log($"ScvDataManager LoadDataAllAndroid - file:{fullname}");
                MemoryStream ms = new MemoryStream(www.bytes);
                CsvStreamReader csv = new CsvStreamReader(fullname, ms, System.Text.Encoding.UTF8);
                _liStreamReaders.Add(nakedName, csv);
            }
            else if(www.error != null)
            {
                Debug.LogError($"CsvDataManaer LoadDataAll Android Error - {www.error} - url:{fullname}");
            }
        }
    }

    public CsvStreamReader GetTable(string filename)
    {
        if (_liStreamReaders.ContainsKey(filename))
        {
            return _liStreamReaders[filename];
        }

        return null;
    }
}

