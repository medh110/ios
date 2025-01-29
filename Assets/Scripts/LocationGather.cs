using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System;
using System.ComponentModel;

public class LocationGather : MonoBehaviour
{
    [SerializeField]
    private char unit = 'K';


    public TMP_Text debugTxt;
    public bool gps_ok = false;
    float PI = Mathf.PI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}


public class GPSloc
{
    public float longitude;
    public float latitude;

    public GPSloc()
    {
        latitude = 0;
        longitude = 0;
    }

}