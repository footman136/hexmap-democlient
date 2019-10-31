using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectCircle : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    // Start is called before the first frame update
    void Start()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        SetSize(5);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetSize(float size)
    {
        ParticleSystem.EmitParams param = new ParticleSystem.EmitParams
        {
            startSize = size,
        };
        ParticleSystem.Particle particle = 
        _particleSystem.Emit(param,1);
        transform.localScale = new Vector3(size,0,size);
    }
}
