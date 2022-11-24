using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    //variable para acceder al AudioSource

    private AudioSource _audioSoucer; 

    void Awake()
    {
        //Asignamos la variable
        _audioSoucer = GetComponent <AudioSource>();
    }
    // Start is called before the first frame update
    void Start()
    {
        //Reproducimos la BGM
        _audioSoucer.Play();
    }
    //Funcion para parar la BGM
    public void StopBGM()
    {
        _audioSoucer.Stop();
    }
}
