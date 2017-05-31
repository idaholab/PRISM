// Adapted from a script at: http://wiki.unity3d.com/index.php?title=MouseOrbitImproved 
using UnityEngine;
using System.Collections;
 
[AddComponentMenu("Camera-Control/Mouse Orbit with zoom")]
public class ArcBallCamera : MonoBehaviour
{

    public Transform target;
    public float distance = 5.0f;
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;

    public float yMinLimit = -90f;
    public float yMaxLimit = 90f;

    public float distanceMin = .5f;
    public float distanceMax = 15f;

    private Rigidbody rigidbody;

    float x = 0.0f;
    float y = 0.0f;

    // Use this for initialization
    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        rigidbody = GetComponent<Rigidbody>();

        // Make the rigid body not change rotation
        if (rigidbody != null)
        {
            rigidbody.freezeRotation = true;
        }
    }

    void LateUpdate()
    {
        if (target && Input.GetMouseButton(0))
        {
            x += Input.GetAxis("Mouse X") * xSpeed * distance * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

            y = ClampAngle(y, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(y, x, 0);

            distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * 5, distanceMin, distanceMax);

            RaycastHit hit;
            if (Physics.Linecast(target.position, transform.position, out hit))
            {
                distance -= hit.distance;
            }
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + target.position;

            transform.rotation = rotation;
            transform.position = position;
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}

/******************** OLD SCRIPT **************/

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class ArcBallCamera : MonoBehaviour {

//    public GameObject target;
//    public float rotationSpeed = 5;

//    public float speedH = 2.0f;
//    public float speedV = 2.0f;

//    private float yaw = 0.0f;
//    private float pitch = 0.0f;

//	// Use this for initialization
//	void Start () {
//        transform.LookAt(target.transform.position);
//    }

//	// Update is called once per frame
//	void Update () {
//        //transform.RotateAround(target.transform.position, Vector3.up, 20 * Time.deltaTime);

//        if (Input.GetMouseButton(0))
//        {
//            transform.LookAt(target.transform.position);
//            transform.RotateAround(target.transform.position, Vector3.up, Input.GetAxis("Mouse X") * rotationSpeed);
//            transform.RotateAround(target.transform.position, Vector3.right, Input.GetAxis("Mouse Y") * rotationSpeed);
//        }
//        // first person camera
//        //if (Input.GetMouseButton(0))
//        //{
//        //    yaw += speedH * Input.GetAxis("Mouse X");
//        //    pitch -= speedV * Input.GetAxis("Mouse Y");

//        //    transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
//        //}
//    }
//}