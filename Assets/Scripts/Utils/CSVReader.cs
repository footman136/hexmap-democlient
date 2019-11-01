using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;

public class CSVReader : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    bool ReadFile( string file)
    {
        string pathFile = Application.streamingAssetsPath + file;
        FileStream fs = new FileStream(pathFile, FileMode.Open, FileAccess.Read, FileShare.None);
        StreamReader sr = new StreamReader(fs, System.Text.Encoding.GetEncoding("utf-8"));
 
        string str = "";
        string s = Console.ReadLine();
        while (str != null)
        {    str = sr.ReadLine();
            string[] xu = new String[2];
            xu = str.Split(',');
            string ser = xu[0]; 
            string dse = xu[1];                if (ser == s)
            { Console.WriteLine(dse);break;
            }
        }   sr.Close();
        
    }
}

