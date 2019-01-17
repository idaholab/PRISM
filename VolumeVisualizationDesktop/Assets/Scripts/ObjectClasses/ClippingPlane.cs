/* 
 * Copyright 2019 Idaho National Laboratory.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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
