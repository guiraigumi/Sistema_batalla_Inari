using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public AudioClip selectSFX;
    public AudioClip moveSFX;
    public AudioClip actionSFX;


    //variable del audio source
    private AudioSource _audioSource;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }
    
    public void SelectSound()
    {
        _audioSource.PlayOneShot(selectSFX);
    }

    public void MoveSound()
    {
        _audioSource.PlayOneShot(moveSFX);
    }

    public void ActionSound()
    {
        _audioSource.PlayOneShot(actionSFX);
    }

}
