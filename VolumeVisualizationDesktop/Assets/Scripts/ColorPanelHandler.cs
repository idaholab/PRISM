/* Color Panel Handler | Marko Sterbentz 6/22/2017 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles user input for the color portion of the transfer function.
/// </summary>
public class ColorPanelHandler : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
	// External variables
	public Canvas colorCanvas;                                  // The canvas that overlays the color panel and will be used to draw the color graph.
    private TransferFunctionHandler transferFunctionHandler;    // The transfer function script that passes user input to the transfer function.
	private VolumeController volumeController;                  // A reference to the VolumeController that manages the renderer.
	private TransferFunction transferFunction;                  // The transfer function that is used by the current volume.

	// Internal variables
	private RectTransform panelRectTransform;					// The RectTransform of the color panel.
    private RectTransform drawCanvasRectTransform;              // The RectTransform of the color panel.
    private float maxWidth;                                     // The local maximum width of the color panel.
	private float minWidth, minHeight;                          // The local minimum width and height of the color panel.

    public GameObject controlPointImagePrefab;                  // A prefab that will represent the control points visually in the color graph.
    private List<ControlPointRenderer> controlPointRenderers;   // A reference to all of the wrappers for rendering the control points in the color graph.

    private float pointRadius = 10.0f;							// Radius of the points in the graph.
	private float borderSize = 0.0f;                            // Size of the border around the panel. Padding on the internal edges of the panel is added. CAUSES BUGS WHEN NOT 0.0

    public Color pointColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    public Color pointHighlightColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);

    /// <summary>
	/// Initialization function for the ColorPanelHandler.
	/// </summary>
    void Start () {
		panelRectTransform = transform as RectTransform;
        drawCanvasRectTransform = colorCanvas.transform as RectTransform;
        //maxWidth = panelRectTransform.rect.width - borderSize;
        //minWidth = borderSize;
        //minHeight = borderSize;
        maxWidth = drawCanvasRectTransform.rect.width;
        minWidth = 0.0f;
        minHeight = 0.0f;
        transferFunctionHandler = (TransferFunctionHandler)GameObject.Find("Transfer Function Panel").GetComponent(typeof(TransferFunctionHandler));
		//volumeController = (VolumeController)GameObject.Find("VolumeController").GetComponent(typeof(VolumeController));
		volumeController = (VolumeController)GameObject.Find("Main Camera").GetComponent(typeof(VolumeController));
		transferFunction = volumeController.getTransferFunction();

        // Initialize the control point renderers
        controlPointRenderers = new List<ControlPointRenderer>();
        for (int i = 0; i < transferFunction.ColorPoints.Count; i++)
        {
            controlPointRenderers.Add(new ControlPointRenderer(transferFunction.ColorPoints[i],
                                                               createControlPointImage(transferFunction.ColorPoints[i]))
                                     );
        }
    }
	
	/// <summary>
	/// Updates the ColorPanelHandler once per frame.
	/// </summary>
	void Update () {
		
	}

	/// <summary>
	/// Unity OnPointerDown event handler for the ColorPanelHandler.
	/// </summary>
	/// <param name="data"></param>
    public void OnPointerDown(PointerEventData data)
    {
		// Get the local position within the color panel
		Vector2 localPosition = getLocalPositionFromScreenPosition(data.position, data.pressEventCamera);

		// Check if there is a control point at the localPosition, and return it if there is one
		transferFunction.ActiveControlPoint = getClickedPoint(localPosition);

		// Deselect any highlighted points and close the color palette
		transferFunctionHandler.dehighlightPoints();
		transferFunctionHandler.closeColorPalette();

		// Left click
		if (Input.GetMouseButton(0))
		{
			if (transferFunction.ActiveControlPoint == null)
			{
				// If no point is clicked, generate a new point to be added to the transfer function
				ControlPoint newActivePoint = getColorPointFromOffset(localPosition, transferFunctionHandler.getColorPalette().getCurrentColor());
				transferFunction.addColorPoint(newActivePoint);
                controlPointRenderers.Add(new ControlPointRenderer(newActivePoint, createControlPointImage(newActivePoint)));
                transferFunction.ActiveControlPoint = newActivePoint;
			}

			// Open the color palette
			transferFunctionHandler.openColorPalette();
			transferFunctionHandler.getColorPalette().setCurrentColor(transferFunction.ActiveControlPoint.color);
			transferFunctionHandler.getColorPalette().setSliders();
		}
		// Right click
		else if (Input.GetMouseButton(1))
		{
			if (transferFunction.ActiveControlPoint != null)
			{
				if (transferFunction.ColorPoints.Count > 2)
				{
					// Delete the active point that was clicked
					transferFunction.removeColorPoint(transferFunction.ActiveControlPoint);

					// Delete its associated renderer
					for (int i = 0; i < controlPointRenderers.Count; i++)
					{
						if (controlPointRenderers[i].CP.Equals(transferFunction.ActiveControlPoint))
						{
							controlPointRenderers[i].destruct();
							controlPointRenderers.Remove(controlPointRenderers[i]);
						}
					}
				}
            }
		}
    }

	/// <summary>
	/// Unity OnDrag event handler for the ColorPanelHandler.
	/// </summary>
	/// <param name="data"></param>
    public void OnDrag(PointerEventData data)
    {
		// Left click
		if (Input.GetMouseButton(0))
		{
			// Get the current position of the mouse in the local coordinates of the color panel
			Vector2 localPosition = getLocalPositionFromScreenPosition(data.position, data.pressEventCamera);

			// Update the current active point in the transfer function
			transferFunction.updateActivePoint(getColorPointFromOffset(localPosition, transferFunction.ActiveControlPoint.color));
		}
	}

	/// <summary>
	/// Unity OnPointerUp event handler for the ColorPanelHandler.
	/// </summary>
	/// <param name="data"></param>
    public void OnPointerUp(PointerEventData data)
    {
		// Finalizes the temporary color point
		//transferFunction.finalizeActivePoint();
	}

	/// <summary>
	/// Updates the color graph in the user interface by updating and drawing the points.
	/// Assumes the transfer function's color points are already sorted.
	/// </summary>
	public void updateColorGraph()
	{
        // Update the positions of the control point renderers
        for (int i = 0; i < controlPointRenderers.Count; i++)
        {
            controlPointRenderers[i].Image.transform.localPosition = getLocalPositionFromColorPoint(controlPointRenderers[i].CP);
        }
    }

	/// <summary>
	/// Highlights the current active point in the transfer function.
	/// </summary>
	public void highlightActivePoint()
	{
        //colorGraphHighlightedPoint.Draw();
        ControlPointRenderer activePointRenderer = getControlPointRenderer(transferFunction.ActiveControlPoint);
        if (activePointRenderer != null)
        {
            activePointRenderer.Image.GetComponent<Image>().color = pointHighlightColor;
        }
    }

	/// <summary>
	///  De-highlights the color points in the transfer function.
	/// </summary>
	public void dehighlightPoints()
	{
        for (int i = 0; i < controlPointRenderers.Count; i++)
        {
            controlPointRenderers[i].Image.GetComponent<Image>().color = pointColor;
        }
    }

	/// <summary>
	/// Converts the given color control point to the local space within the color panel.
	/// </summary>
	/// <param name="colorPoint"></param>
	/// <returns></returns>
	private Vector2 getLocalPositionFromColorPoint(ControlPoint colorPoint)
	{
		Vector2 localPosition = new Vector2();

		// Convert to a local position within the color panel
		localPosition.x = (colorPoint.isovalue / (float) transferFunction.IsovalueRange) * maxWidth;
		localPosition.y = 0.0f;

		// Clamp the position to stay within the color panel's borders
		localPosition = clampToColorPanel(localPosition);

		return localPosition;
	}

	/// <summary>
	/// Converts the given screen position to a local space within the color panel.
	/// </summary>
	/// <param name="screenPosition"></param>
	/// <param name="cam"></param>
	/// <returns></returns>
	private Vector2 getLocalPositionFromScreenPosition(Vector2 screenPosition, Camera cam)
	{
		Vector2 localPosition;

        // Convert to a local position within the color panel
        //RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, screenPosition, cam, out localPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(drawCanvasRectTransform, screenPosition, cam, out localPosition);

        // Clamp the position to stay within the color panel's borders
        localPosition = clampToColorPanel(localPosition);

		return localPosition;
	}
	 
	/// <summary>
	/// Generates a color control point to be sent to the transfer function.
	/// </summary>
	/// <param name="offset"></param>
	/// <param name="color"></param>
	/// <returns></returns>
	private ControlPoint getColorPointFromOffset(Vector2 offset, Color color)
	{
		ControlPoint cp = new ControlPoint(
			color,																			// color value
			Mathf.FloorToInt(transferFunction.IsovalueRange * (offset.x / maxWidth))	// isovalue index
			);
		return cp;
	}

	/// <summary>
	/// Clamps the given point to be within the borders of the color panel.
	/// </summary>
	/// <param name="localPoint"></param>
	/// <returns></returns>
	private Vector2 clampToColorPanel(Vector2 localPoint)
	{
		localPoint.x = Mathf.Clamp(localPoint.x, minWidth, maxWidth);
		localPoint.y = 0;
		return localPoint;
	}

	/// <summary>
	/// Returns the color control point that was clicked by the user. Returns null if no point was clicked.
	/// The given localClickPosition is assumed to be in the local coordinates of the panel.
	/// </summary>
	/// <param name="localClickPosition"></param>
	/// <returns></returns>
	private ControlPoint getClickedPoint(Vector2 localClickPosition)
	{
		List<ControlPoint> colorPoints = transferFunction.ColorPoints;
		for (int i = 0; i < colorPoints.Count; i++)
		{
			if (Vector2.Distance(localClickPosition, getLocalPositionFromColorPoint(colorPoints[i])) < pointRadius)
			{
				return colorPoints[i];
			}
		}
		return null;
	}

    /// <summary>
    /// Creates a new control point image GameObject to use for rendering the given control point on the color canvas.
    /// </summary>
    /// <param name="cp"></param>
    /// <returns></returns>
    private GameObject createControlPointImage(ControlPoint cp)
    {
        GameObject newControlPointImage = Instantiate(controlPointImagePrefab);
        newControlPointImage.transform.SetParent(colorCanvas.transform);
        newControlPointImage.transform.localPosition = getLocalPositionFromColorPoint(cp);
        newControlPointImage.transform.rotation = Quaternion.identity;
        return newControlPointImage;
    }

    /// <summary>
    /// Returns the ControlPointRenderer associated with the given ControlPoint
    /// </summary>
    /// <returns></returns>
    private ControlPointRenderer getControlPointRenderer(ControlPoint cp)
    {
        for (int i = 0; i < controlPointRenderers.Count; i++)
        {
            if (controlPointRenderers[i].CP.Equals(transferFunction.ActiveControlPoint))
            {
                return controlPointRenderers[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Deletes all of the color control point renderers.
    /// </summary>
    public void deleteAllControlPointRenderers()
    {
        // Delete associated renderers
        if (controlPointRenderers != null)
        {
            for (int i = 0; i < controlPointRenderers.Count; i++)
            {
                controlPointRenderers[i].destruct();
            }
            controlPointRenderers.Clear();
        }
    }

    /// <summary>
    /// Creates a brand new set of control point renderers for each of the color control points.
    /// </summary>
    public void createNewControlPointRenderers()
    {
        if (controlPointRenderers != null)
        {
            for (int i = 0; i < transferFunction.ColorPoints.Count; i++)
            {
                controlPointRenderers.Add(new ControlPointRenderer(transferFunction.ColorPoints[i],
                                                                   createControlPointImage(transferFunction.ColorPoints[i]))
                                         );
            }
        }
    }
}
