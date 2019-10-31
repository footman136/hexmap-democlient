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

    public enum RESOURCE_TYPE
    {
        WOOD = 0,
        FOOD = 1,
        IRON = 2,
    };

    // 储量等级
    public int[] RESERVE_LEVEL = {
        0, 100, 400, 800
    };
    
    // 木材-0；粮食-1；铁矿-2
    public RESOURCE_TYPE ResType; 
    private int [] ResAmount = new int[RES_MAX_TYPE];
    
    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)ResType);
        writer.Write(ResAmount[(int)ResType]);
    }
    public void Load(BinaryReader reader, int header)
    {
        ResType = (RESOURCE_TYPE)reader.ReadByte();
        ResAmount[(int)ResType] = reader.ReadInt32();
    }

    public void SetAmount(RESOURCE_TYPE type, int value)
    {
        ResAmount[(int)type] = value;
    }

    public int GetAmount(RESOURCE_TYPE type)
    {
        return ResAmount[(int)type];
    }

    public int GetLevel(RESOURCE_TYPE type)
    {
        for (int i = 0; i < 4; ++i)
        {
            if (ResAmount[(int) type] <= RESERVE_LEVEL[i])
            {
                return i;
            }
        }

        return 3;
    }
}
