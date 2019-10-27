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
        HexCell cityCenter = SearchAnBurbanArea(out size);
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

    private HexCell SearchAnBurbanArea(out int size)
    {
        const int tryCount = 100;
        size = 0;
        HexCell findCity = null;
        for (int i = 0; i < tryCount; ++i)
        {
            int x = Random.Range(0, _HexmapHelper.hexGrid.cellCountX);
            int y = Random.Range(0, _HexmapHelper.hexGrid.cellCountZ);
            HexCell current = _HexmapHelper.hexGrid.GetCell(x, y);
            if (current.IsUnderwater)
            {
                continue;
            }

            if (current.UrbanLevel > 0)
            {
                continue;
            }
            int elevation = current.Elevation;
            bool bSuccess = true;
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor != null)
                {
                    if (neighbor.Elevation < elevation - 1 || neighbor.Elevation > elevation + 1)
                    {
                        bSuccess = false;
                        break;
                    }
                }

                if (neighbor.UrbanLevel > 0)
                {
                    bSuccess = false;
                    break;
                }
            }

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
                if (current.IsUnderwater)
                {
                    continue;
                }

                if (current.UrbanLevel > 0)
                {
                    continue;
                }

                findCity = current;
                break;
            }
        }


        return findCity;
    }

    public void AddCity(UrbanCity city)
    {
        if (Cities.ContainsKey(city.CityId))
        {
            GameRoomManager.Instance.Log("MSG: Duplicated city!");
        }
        else
        {
            Cities.Add(city.CityId, city);
        }
        _HexmapHelper.AddCity(city.CellIndex, city.CitySize);
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
