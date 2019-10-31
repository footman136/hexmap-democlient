using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrbanCity
{
    public long RoomId;
    public long OwnerId;
    public long CityId;

    public int PosX;
    public int PosZ;
    public int CellIndex;

    public string CityName;
    public int CitySize;
    public bool IsCapital;

    public void Init(long roomId, long ownerId, long cityId, int posX, int posZ, int cellIndex, string cityName, int citySize)
    {
        RoomId = roomId;
        OwnerId = ownerId;
        CityId = cityId;
        PosX = posX;
        PosZ = posZ;
        CellIndex = cellIndex;
        CityName = cityName;
        CitySize = citySize;
    }

}
