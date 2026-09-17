using UnityEngine;

public class LookAtCamera : MonoBehaviour
{

    Camera theCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {   
        theCamera = Camera.main;
        transform.forward = theCamera.transform.position - transform.position;
    }
}
