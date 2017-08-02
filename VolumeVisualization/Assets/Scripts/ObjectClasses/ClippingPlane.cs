using UnityEngine;

/* Clipping Plane | Marko Sterbentz 7/11/2017
 * This class contains the data for a plane that can be used to clip the volume.
 */
public class ClippingPlane
{
	/* Member variables */
	private Vector3 position;
	private Vector3 normal;
	private bool enabled;

	/* Properties */
	public Vector3 Position
	{
		get
		{
			return position;
		}
		set
		{
			position = value;
		}
	}

	public Vector3 Normal
	{
		get
		{
			return normal;
		}
		set
		{
			normal = value;
		}
	}

	public bool Enabled
	{
		get
		{
			return enabled;
		}
		set
		{
			enabled = value;
		}
	}

	/* Constructors */
	public ClippingPlane()
	{
		position = new Vector3(0.5f, 0.5f, 0.5f);
		normal = new Vector3(1, 0, 0);
		enabled = false;
	}

	public ClippingPlane(Vector3 _position, Vector3 _normal, bool _enabled)
	{
		position = _position;
		normal = _normal;
		enabled = _enabled;
	}
}
