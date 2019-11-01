using System;
using UnityEngine;
using UnityEngine.UI;

public class CommandItem : MonoBehaviour
{
    [SerializeField] private Text _label;
    public delegate void ClickCallBack(GameObject obj);
    private ClickCallBack _clickCallBack;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init(string label, ClickCallBack callBack)
    {
        _label.text = label;
        _clickCallBack = callBack;
    }
    
    public void OnClickCommand()
    {
        _clickCallBack?.Invoke(gameObject);
    }
}
