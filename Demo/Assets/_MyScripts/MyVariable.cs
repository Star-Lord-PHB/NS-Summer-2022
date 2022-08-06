using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyVariable : MonoBehaviour
{
    public int floorNum = 1;
    public String id;
    public double x;
    public double y;
    public double height;

    // Start is called before the first frame update
    void Start()
    {
        this.id = gameObject.name;
        this.x = gameObject.transform.position.x; 
        this.y = gameObject.transform.position.z;
        this.height = gameObject.transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
