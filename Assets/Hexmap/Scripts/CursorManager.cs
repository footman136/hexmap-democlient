using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private Texture2D _curCameraMove;
    [SerializeField] private Texture2D _curFindPath;
    [SerializeField] private Texture2D _curCreateActor;
    [SerializeField] private Texture2D _curAttack;
    
    public enum CURSOR_TYPE
    {
        NONE = 0,
        CAMERA_MOVE = 1,
        FIND_PATH = 2,
        CRAETE_ACTOR = 3,
        ATTACK = 4,
    }

    private readonly Stack<CURSOR_TYPE> _cursorStack = new Stack<CURSOR_TYPE>();

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
        showCursor(type);
        _cursorStack.Push(type);
    }
    
    private void showCursor(CURSOR_TYPE type)
    {
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
            case CURSOR_TYPE.ATTACK:
                Cursor.SetCursor(_curAttack, Vector2.zero, CursorMode.Auto);
                break;
            case CURSOR_TYPE.NONE:
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                break;
        }
    }

    public void RestoreCursor()
    {
        if (_cursorStack.Count > 0)
            _cursorStack.Pop();
        if (_cursorStack.Count > 0)
            showCursor(_cursorStack.Peek());
        else
            showCursor(CURSOR_TYPE.NONE);
    }
}
