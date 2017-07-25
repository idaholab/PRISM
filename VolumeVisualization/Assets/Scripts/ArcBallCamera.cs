/* Arc Ball Camera | Marko Sterbentz 5/31/2017
 * This script provides simple (albeit flawed) controls for orbiting the camera around a fixed center point.
 * Note: Adapted from a script at: http://wiki.unity3d.com/index.php?title=MouseOrbitImproved 
 */
using UnityEngine;
using UnityEngine.EventSystems;

public class ArcBallCamera : MonoBehaviour
{
	private Vector3 targetPosition;

	public float distance = 5.0f;
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;

    public float yMinLimit = -90f;
    public float yMaxLimit = 90f;

    public float distanceMin = .5f;
    public float distanceMax = 15f;

    private Rigidbody rigidbody;

    private float x = 0.0f;
    private float y = 0.0f;


    // Use this for initialization
    void Start()
    {
		targetPosition = new Vector3(0.5f, 0.5f, 0.5f);
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
		if (Input.GetMouseButton(0) 
            && EventSystem.current.currentSelectedGameObject == null
            && !EventSystem.current.IsPointerOverGameObject())
        {
            x += Input.GetAxis("Mouse X") * xSpeed * distance * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

            y = ClampAngle(y, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(y, x, 0);

            distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * 5, distanceMin, distanceMax);

            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + targetPosition;

            transform.rotation = rotation;
            transform.position = position;
        }

        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            y = ClampAngle(y, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(y, x, 0);

            distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * 5, distanceMin, distanceMax);

            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + targetPosition;

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