﻿using System.Collections;
using System.Collections.Generic;
using Animation;
using UnityEngine;

public class PickInfo
{
    public enum SelectorType
    {
        NONE = 0,
        CELL = 1,
        UNIT = 2,
        CITY = 3,
    }

    public HexCell CurrentCell;
    //public HexUnit CurrentUnit;
    public UrbanCity CurrentCity;
    public ActorVisualizer CurrentActor;

    public void Clear()
    {
        CurrentCell = null;
        //CurrentUnit = null;
        CurrentCity = null;
        CurrentActor = null;
    }
}    

