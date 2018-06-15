/* Clipping Plane Input | Marko Sterbentz 7/11/17 */

using UnityEngine;

/// <summary>
/// Get input for the clipping tool and pass it to the VolumeController.
/// </summary>
public class ClippingPlaneInput : MonoBehaviour
{
	public Camera mainCamera;
	private VolumeController volumeController;

	/// <summary>
	/// Initialization function for the AlphaPanelHandler.
	/// </summary>
	void Start () {
		// Set up the reference to the VolumeController
		volumeController = (VolumeController)GameObject.Find("VolumeController").GetComponent(typeof(VolumeController));
	}

	/// <summary>
	/// Updates ClippingPlaneInput once per frame, after other updates have ran.
	/// </summary>
	private void LateUpdate()
	{
		// Right click events
		if (Input.GetKeyDown("c"))
		{
			volumeController.getClippingPlane().Enabled = true;
			volumeController.updateClippingPlaneAll();
		}
		if (Input.GetKeyUp("c"))
		{
			volumeController.getClippingPlane().Enabled = false;
			volumeController.updateClippingPlaneAll();
		}

		// Key Press Events
		if (Input.GetKey("up"))
		{
			volumeController.getClippingPlane().Position += new Vector3(0.0f, 0.01f, 0.0f);
			volumeController.updateClippingPlaneAll();
		}
		if (Input.GetKey("down"))
		{
			volumeController.getClippingPlane().Position += new Vector3(0.0f, -0.01f, 0.0f);
			volumeController.updateClippingPlaneAll();
		}
		if (Input.GetKey("right"))
		{
			volumeController.getClippingPlane().Position += new Vector3(0.01f, 0.0f, 0.0f);
			volumeController.updateClippingPlaneAll();
		}
		if (Input.GetKey("left"))
		{
			volumeController.getClippingPlane().Position += new Vector3(-0.01f, 0.0f, 0.0f);
			volumeController.updateClippingPlaneAll();
		}
		if (Input.GetKey("a"))
		{
			volumeController.getClippingPlane().Normal = Quaternion.AngleAxis(-1, Vector3.up) * volumeController.getClippingPlane().Normal;
			volumeController.updateClippingPlaneAll();
		}
		if (Input.GetKey("d"))
		{
			volumeController.getClippingPlane().Normal = Quaternion.AngleAxis(1, Vector3.up) * volumeController.getClippingPlane().Normal;
			volumeController.updateClippingPlaneAll();
		}
		if (Input.GetKey("s"))
		{
			volumeController.getClippingPlane().Normal = Quaternion.AngleAxis(-1, Vector3.right) * volumeController.getClippingPlane().Normal;
			volumeController.updateClippingPlaneAll();
		}
		if (Input.GetKey("w"))
		{
			volumeController.getClippingPlane().Normal = Quaternion.AngleAxis(1, Vector3.right) * volumeController.getClippingPlane().Normal;
			volumeController.updateClippingPlaneAll();
		}

		// Update the position of the clipping plane cube
		if (volumeController.clippingPlaneCube != null)
		{
			volumeController.clippingPlaneCube.transform.position = volumeController.getClippingPlane().Position;
			volumeController.clippingPlaneCube.transform.rotation = Quaternion.LookRotation(volumeController.getClippingPlane().Normal);
		}
	}
}
