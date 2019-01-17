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


/* Drag Panel | Marko Sterbentz 6/6/2017 
 * Note: Code taken from the tutorial at: https://unity3d.com/learn/tutorials/modules/intermediate/live-training-archive/panels-panes-windows?playlist=17111
 */

using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// This script allows the panel to which it is attached to be dragged on click.
/// </summary>
public class DragPanel : MonoBehaviour, IPointerDownHandler, IDragHandler
{

    private Vector2 pointerOffset;
    private RectTransform canvasRectTransform;
    private RectTransform panelRectTransform;

	/// <summary>
	/// Initializes the DragPanel before the other Start functions.
	/// </summary>
    void Awake()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRectTransform = canvas.transform as RectTransform;
            panelRectTransform = transform as RectTransform;
        }
    }

	/// <summary>
	/// Unity OnPointerDown event handler for the drag panel.
	/// </summary>
	/// <param name="data"></param>
    public void OnPointerDown(PointerEventData data)
    {
        panelRectTransform.SetAsLastSibling();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, data.position, data.pressEventCamera, out pointerOffset);
    }

	/// <summary>
	/// Unity OnDrag event handler for the drag panel.
	/// </summary>
	/// <param name="data"></param>
    public void OnDrag(PointerEventData data)
    {
        if (panelRectTransform == null)
            return;

        Vector2 pointerPostion = ClampToWindow(data);

        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform, pointerPostion, data.pressEventCamera, out localPointerPosition
        ))
        {
            panelRectTransform.localPosition = localPointerPosition - pointerOffset;
        }
    }

	/// <summary>
	/// Clamps the button click to be within the game window.
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
    Vector2 ClampToWindow(PointerEventData data)
    {
        Vector2 rawPointerPosition = data.position;

        Vector3[] canvasCorners = new Vector3[4];
        canvasRectTransform.GetWorldCorners(canvasCorners);

        float clampedX = Mathf.Clamp(rawPointerPosition.x, canvasCorners[0].x, canvasCorners[2].x);
        float clampedY = Mathf.Clamp(rawPointerPosition.y, canvasCorners[0].y, canvasCorners[2].y);

        Vector2 newPointerPosition = new Vector2(clampedX, clampedY);
        return newPointerPosition;
    }
}