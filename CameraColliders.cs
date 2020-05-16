using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraColliders : MonoBehaviour
{
    ///Minimum Distance is 1
    public float minDistance = 1.0f;
    ///Maximum Distance is 3
    public float maxDistance = 3.0f;
    public float smooth = 10.0f;
    Vector3 dollyDir;
    public Vector3 dollyDirAdjusted;
    public float distance;


    
    private void Awake()
    {
        dollyDir = transform.localPosition.normalized;
        distance = transform.localPosition.magnitude;
    }

    void Update()
    {
        Vector3 desiredCameraPos = transform.parent.TransformPoint(dollyDir * maxDistance);
        RaycastHit hit;

        if (Physics.Linecast(transform.parent.position, desiredCameraPos, out hit))
        {
            distance = Mathf.Clamp((hit.distance * 0.87f), minDistance, maxDistance);
        }
        
        else
        {
            distance = maxDistance;
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, dollyDir * distance, Time.deltaTime * smooth);

        }

    /// Following function is used to test

    void TestFunction()
    {
        Debug.Log("Test");
    }
                
    }



