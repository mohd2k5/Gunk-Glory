using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KatamariStick : MonoBehaviour
{
    //This class was supposed to be called something else, but I got some things mixed up when creating it

    public float size;
    private Collider objCollider;
    public string objName = "placeholder";

    void Start()
    {
        objCollider = GetComponent<Collider>();

        if (!GetComponent<Rigidbody>())
        {
            gameObject.AddComponent<Rigidbody>();
        }
        Rigidbody rb = GetComponent<Rigidbody>();
        size = (objCollider.bounds.size.x + objCollider.bounds.size.y + objCollider.bounds.size.z)/3;
    }

    
    void Update()
    {
        
    }
}
