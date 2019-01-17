
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


/* Camera Controls | Nathan Morrical  (edited by Marko Sterbentz) 8/2/2017 */

using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Camera controls for orbiting around a given target object.
/// </summary>
public class CameraControls : MonoBehaviour {
    public GameObject target = null;
    public float zoom = 0.0f;
    public float zoomSpeed = 10.0f;
    public float zoomResistance = .1f;

    public float yaw = 0.0f;
    public float pitch = 0.0f;
    public float rotateSpeed = 10.0f;
    public float rotateResistance = .3f;

    Quaternion quat = new Quaternion();

	/// <summary>
	/// Updates CameraControls once per frame, after Update function has ran.
	/// </summary>
    void LateUpdate()
    {
        if (Input.GetMouseButton(1)) HandleRightMouse();
        if (Input.GetMouseButton(0)) HandleLeftMouse();
        if (Input.GetAxis("Mouse ScrollWheel") != 0) HandleScroll();
    }

	/// <summary>
	/// Updates zoom of the camera for the dolly effect based on input from the right mouse button.
	/// </summary>
    void HandleRightMouse() {
        int sign = Math.Sign(Input.GetAxis("Mouse Y"));
        zoom += sign * zoomSpeed * .25f;
    }

	/// <summary>
	/// Updates the yaw and pitch of the camera for rotation based on input from the mouse.
	/// </summary>
    void HandleLeftMouse() {
        yaw += Input.GetAxis("Mouse X") * rotateSpeed;
        pitch += -Input.GetAxis("Mouse Y") * rotateSpeed;
    }

	/// <summary>
	/// Updates the zoom of the camera based on input from the scrollwheel.
	/// </summary>
    void HandleScroll() {
        int sign = Math.Sign(Input.GetAxis("Mouse ScrollWheel"));
        zoom += sign * zoomSpeed;
    }

	/// <summary>
	/// Updates CameraControls once per frame.
	/// </summary>
    void Update()
    {
		if (target == null 
			|| EventSystem.current.currentSelectedGameObject != null
			|| EventSystem.current.IsPointerOverGameObject())
		{
			zoom = 0;
			yaw = 0;
			pitch = 0;
			return;
		}

        float zoomAmount = zoom * zoomResistance;
        float yawAmount = yaw * rotateResistance;
        float pitchAmount = pitch * rotateResistance;
        zoom -= zoomAmount;
        yaw -= yawAmount;
        pitch -= pitchAmount;

        transform.RotateAround(target.transform.position, transform.right, pitchAmount);
        transform.RotateAround(target.transform.position, transform.up, yawAmount);
        transform.Translate(new Vector3(0.0f, 0.0f, 1.0f) * zoomAmount);
    }
}


