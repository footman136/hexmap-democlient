using System.Collections;
using System.Collections.Generic;
using GameUtils;
using Main;
using UnityEngine;

public class UrbanManager
{
    public HexmapHelper _HexmapHelper;

    public Dictionary<long, UrbanCity> Cities = new Dictionary<long, UrbanCity>();
    
    

    public UrbanCity CreateRandomCity()
    {
        int size;
        HexCell cityCenter = SearchAnUrbanArea(out size);
        UrbanCity city = new UrbanCity()
        {
            RoomId = GameRoomManager.Instance.RoomId,
            OwnerId = GameRoomManager.Instance.CurrentPlayer.TokenId,
            CityId = Utils.GuidToLongId(),
            PosX = cityCenter.coordinates.X,
            PosZ = cityCenter.coordinates.Z,
            CellIndex = cityCenter.Index,
            CityName = "N/A",
            CitySize = size,
        };
        return city;
    }

    public UrbanCity CreateCityHere(HexCell cell)
    {
        int size = 1;
        bool bSuccess = TryALargeCityHere(cell);
        if (!bSuccess)
        {
            bSuccess = TryASmallCityHere(cell);
            size = 0;
        }

        if (bSuccess)
        {
            UrbanCity city = new UrbanCity()
            {
                RoomId = GameRoomManager.Instance.RoomId,
                OwnerId = GameRoomManager.Instance.CurrentPlayer.TokenId,
                CityId = Utils.GuidToLongId(),
                PosX = cell.coordinates.X,
                PosZ = cell.coordinates.Z,
                CellIndex = cell.Index,
                CityName = "N/A",
                CitySize = size,
            };
            return city;
        }

        return null;
    }

    private HexCell SearchAnUrbanArea(out int size)
    {
        const int tryCount = 100;
        size = 0;
        HexCell findCity = null;
        for (int i = 0; i < tryCount; ++i)
        {
            int x = Random.Range(0, _HexmapHelper.hexGrid.cellCountX);
            int y = Random.Range(0, _HexmapHelper.hexGrid.cellCountZ);
            HexCell current = _HexmapHelper.hexGrid.GetCell(x, y);
            bool bSuccess = TryALargeCityHere(current);
            if (bSuccess)
            {
                findCity = current;
                size = 1;
                break;
            }
        }
        if (!findCity)
        {
            for (int i = 0; i < tryCount; ++i)
            {
                int x = Random.Range(0, _HexmapHelper.hexGrid.cellCountX);
                int y = Random.Range(0, _HexmapHelper.hexGrid.cellCountZ);
                HexCell current = _HexmapHelper.hexGrid.GetCell(x, y);
                bool bSuccess = TryASmallCityHere(current);
                if (bSuccess)
                {
                    findCity = current;
                    break;
                }
            }
        }

        return findCity;
    }

    public bool TryALargeCityHere(HexCell current)
    {
        if (current.IsUnderwater)
        {
            return false;
        }

        if (current.UrbanLevel > 0)
        {
            return false;
        }
        int elevation = current.Elevation;
        bool bSuccess = true;
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = current.GetNeighbor(d);
            if (neighbor != null)
            {
                if (neighbor.Elevation < elevation - 1 || neighbor.Elevation > elevation + 1) // 城市的周边6格高度差只能相差正负1
                {
                    bSuccess = false;
                    break;
                }
            }

            if (neighbor.UrbanLevel > 0) // 城市不能挨着
            {
                bSuccess = false;
                break;
            }
        }

        return bSuccess;
    }

    public bool TryASmallCityHere(HexCell current)
    {
        if (current.IsUnderwater)
        {
            return false;
        }

        if (current.UrbanLevel > 0)
        {
            return false;
        }

        return true;
    }

    public void AddCity(UrbanCity city, bool isMyCity)
    {
        if (Cities.ContainsKey(city.CityId))
        {
            GameRoomManager.Instance.Log("MSG: Duplicated city!");
        }
        else
        {
            Cities.Add(city.CityId, city);
        }
        _HexmapHelper.AddCity(city.CellIndex, city.CitySize, isMyCity);
    }

    public void RemoveCity(long cityId)
    {
        if (Cities.ContainsKey(cityId))
        {
            var city = Cities[cityId];
            _HexmapHelper.RemoveCity(city.CellIndex, city.CitySize);
            Cities.Remove(cityId);
        }
    }
}
