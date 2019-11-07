using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private Texture2D _curCameraMove;
    [SerializeField] private Texture2D _curFindPath;
    [SerializeField] private Texture2D _curCreateActor;
    
    public enum CURSOR_TYPE
    {
        NONE = 0,
        CAMERA_MOVE = 1,
        FIND_PATH = 2,
        CRAETE_ACTOR = 3,
    }

    private CURSOR_TYPE _cursorType;
    private CURSOR_TYPE _cursorTypeLast;

    public static CursorManager Instance;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("CursorManager is a singlton, cannot be initialized twice!");
        }
        Instance = this;
    }
    
    public void ShowCursor(CURSOR_TYPE type)
    {
        _cursorTypeLast = _cursorType;
        _cursorType = type;  
        switch (type)
        {
            case CURSOR_TYPE.CAMERA_MOVE:
                Cursor.SetCursor(_curCameraMove, Vector2.zero, CursorMode.Auto);
                break;
            case CURSOR_TYPE.FIND_PATH:
                Cursor.SetCursor(_curFindPath, Vector2.zero, CursorMode.Auto);
                break;
            case CURSOR_TYPE.CRAETE_ACTOR:
                Cursor.SetCursor(_curCreateActor, Vector2.zero, CursorMode.Auto);
                break;
            case CURSOR_TYPE.NONE:
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                break;
        }
    }

    public void RestoreCursor()
    {
        ShowCursor(_cursorTypeLast);
    }
}
