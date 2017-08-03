/* Clipping Plane | Marko Sterbentz 7/11/2017 */

using UnityEngine;

/// <summary>
/// Contains the data for a plane that can be used to clip the volume.
/// </summary>
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
	/// <summary>
	/// Creates a new instance of a clipping plane.
	/// </summary>
	public ClippingPlane()
	{
		position = new Vector3(0.5f, 0.5f, 0.5f);
		normal = new Vector3(1, 0, 0);
		enabled = false;
	}

	/// <summary>
	/// Creates a new instance of a clipping plane.
	/// </summary>
	/// <param name="_position"></param>
	/// <param name="_normal"></param>
	/// <param name="_enabled"></param>
	public ClippingPlane(Vector3 _position, Vector3 _normal, bool _enabled)
	{
		position = _position;
		normal = _normal;
		enabled = _enabled;
	}
}
