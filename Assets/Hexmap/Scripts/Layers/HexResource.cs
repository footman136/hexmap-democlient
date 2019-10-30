using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 保存每个格子上，资源的具体种类和数值 
/// </summary>
public class HexResource
{
    public const int RES_MAX_TYPE = 3; 
    // 木材-0；粮食-1；铁矿-2
    public int ResType; 
    public int [] ResAmount = new int[RES_MAX_TYPE];
    
    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)ResType);
        writer.Write(ResAmount[ResType]);
    }
    public void Load(BinaryReader reader, int header)
    {
        ResType = reader.ReadByte();
        ResAmount[ResType] = reader.ReadInt32();
    }

    public void SetAmount(int type, int value)
    {
        ResAmount[type] = value;
    }

    public int GetAmount(int type)
    {
        return ResAmount[type];
    }
}
