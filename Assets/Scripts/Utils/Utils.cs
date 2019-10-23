using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace GameUtils
{
	public class Utils  
	{
		/// <summary>  
		/// 根据GUID获取19位的唯一数字序列  
		/// </summary>  
		/// <returns></returns>  
		public static long GuidToLongId()
		{
			byte[] buffer = Guid.NewGuid().ToByteArray();
			return BitConverter.ToInt64(buffer, 0);
		}
    
		public static string MD5Hash(byte[] bytes)
		{
			MD5 md5 = new MD5CryptoServiceProvider();
			byte[] result = md5.ComputeHash (bytes);
			string  strHash = "";			
			for (int i = 0; i < result.Length; i++)
				strHash += string.Format("{0:X2}", result[i]);
			return strHash.ToLower(); ;
		}
	
		public static bool IsPersistentFileExists(string fileName){
			
			return File.Exists (System.IO.Path.Combine (Application.persistentDataPath, fileName));
		}
	
		public static FileStream OpenPersistentFile(string fileName,bool write){
			
			string path = System.IO.Path.Combine (Application.persistentDataPath, fileName);
			FileStream s = null;
			if (!write) {
				s = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			} else {
				s = new FileStream (path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
			}
			#if UNITY_IOS
			UnityEngine.iOS.Device.SetNoBackupFlag(path);
			#endif
			return s;
		}
	
		public static void RemovePersistentFile(string fileName){
			string path = System.IO.Path.Combine (Application.persistentDataPath, fileName);
			if (File.Exists (path)) {
				File.Delete (path);
			}
		}
	
		private static string _logPathName = "";
		public static void InitLogFileName()
		{
			string logfilename = "myoutput.log";
			_logPathName = $"{Application.persistentDataPath}/{logfilename}";
			LogClear();
		}
	
		/// <summary>
		/// 这是一个专门用于在手机目录里写入log的函数，不同于Debug.Log()，可以在手机上记录log文件。
		/// 注意，要谨慎使用，因为它会导致log文件不断增大，可能导致手机SD卡空间被占用得越来越大
		/// Sep.2.2019. Liu Gang
		/// </summary>
		/// <param name="msg"></param>
		public static void Log(string msg)
		{
			Debug.Log(msg);
			if (string.IsNullOrEmpty(_logPathName))
			{
				return;
			}
			string logpathname = _logPathName;
			FileStream fs = new FileStream (logpathname, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
			StreamWriter sw = new StreamWriter(fs);
			sw.Flush();
			sw.BaseStream.Seek(0, SeekOrigin.End);
			sw.WriteLine(msg);
			sw.Flush();
			sw.Close();
			fs.Close();
		}
	
		/// <summary>
		/// 删除myoutput.log文件。Oct.11.2019. Liu Gang.
		/// </summary>
		public static void LogClear()
		{
			string logpathname = _logPathName;
			if (File.Exists(logpathname))
			{
				File.Delete(logpathname);
			}
		}
	}
}
