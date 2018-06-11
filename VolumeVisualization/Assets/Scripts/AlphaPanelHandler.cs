/* Alpha Panel Handler | Marko Sterbentz 6/21/2017 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

/// <summary>
/// Provides functionality for handling user input for the alpha portion of the transfer function.
/// Important note for use: Ensure that the panel this script is attached to has its pivot set to (0,0).
/// </summary>
public class AlphaPanelHandler : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler {

	public Canvas alphaCanvas;								// The canvas that overlays the alpha panel and will be used to draw the alpha graph.
    private TransferFunctionHandler transferFunctionHandler;// The transfer function script that passes user input to the transfer function.
	private TransferFunction transferFunction;				// The transfer function that is used by the current volume.
	private VolumeController volumeController;				// A reference to the VolumeController that manages the renderer.

    private RectTransform drawCanvasRectTransform;          // The RectTransform of the alpha drawing canvas.
	//private RectTransform panelRectTransform;				// The RectTransform of the alpha panel.
    private float maxWidth, maxHeight;						// The local maximum width and height of the alpha panel.
	private float minWidth, minHeight;                      // The local minimum width and height of the alpha panel.
        
    public UILineRenderer alphaUILineRenderer;              // The LineRenderer that will be used to display the alpha graph.
    public GameObject controlPointImagePrefab;              // A prefab that will represent the control points visually in the alpha graph.
    private List<ControlPointRenderer> controlPointRenderers; // A reference to all of the wrappers for rendering the control points in the alpha graph.
    public Text maxIsovalueLabel;                           // A reference to the max isovalue label in the alpha panel area.

	private float pointRadius = 10.0f;						// Radius of the control points when interacting with them in the graph.
	private float borderSize = 0.0f;                        // Size of the border around the panel. Padding on the internal edges of the panel is added. CAUSES BUGS WHEN NOT 0.0

    public Color pointColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    public Color pointHighlightColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
    
    // Use this for initialization
	/// <summary>
	/// Initialization function for the AlphaPanelHandler.
	/// </summary>
    void Start()
    {
		//panelRectTransform = transform as RectTransform;
        drawCanvasRectTransform = alphaCanvas.transform as RectTransform;
        //maxWidth = panelRectTransform.rect.width - borderSize;
        //maxHeight = panelRectTransform.rect.height - borderSize;
        //minWidth =  borderSize;
        //minHeight = borderSize;
        maxWidth = drawCanvasRectTransform.rect.width;
        maxHeight = drawCanvasRectTransform.rect.height;
        minWidth = 0.0f;
        minHeight = 0.0f;
        transferFunctionHandler = (TransferFunctionHandler)GameObject.Find("Transfer Function Panel").GetComponent(typeof(TransferFunctionHandler));
		volumeController = (VolumeController)GameObject.Find("VolumeController").GetComponent(typeof(VolumeController));
		transferFunction = volumeController.getTransferFunction();
        maxIsovalueLabel.text = transferFunction.IsovalueRange.ToString();

        // Initialize the control point renderers
        controlPointRenderers = new List<ControlPointRenderer>();
        for (int i = 0; i < transferFunction.AlphaPoints.Count; i++)
        {
            controlPointRenderers.Add(new ControlPointRenderer(transferFunction.AlphaPoints[i], 
                                                               createControlPointImage(transferFunction.AlphaPoints[i]))
                                     );
        }
    }

	/// <summary>
	/// Updates AlphaPanelHandler once per frame.
	/// </summary>
	void Update()
	{

	}

	/// <summary>
	/// Create a temporary alpha control point that is sent to the transfer function.
	/// </summary>
	/// <param name="data"></param>
	public void OnPointerDown(PointerEventData data)
    {
		// Close the color palette (if it is open)
		transferFunctionHandler.closeColorPalette();

		// Get the local position within the alpha panel
		Vector2 localPosition = getLocalPositionFromScreenPosition(data.position, data.pressEventCamera);

        transferFunctionHandler.dehighlightPoints();

        // Check if there is a control point at the localPosition, and return it if there is one
        transferFunction.ActiveControlPoint = getClickedPoint(localPosition);

		// Left click
		if (Input.GetMouseButton(0))
		{
			if (transferFunction.ActiveControlPoint == null)
			{
				// If no point is clicked, generate a new point to be added to the transfer function
				ControlPoint newActivePoint = getAlphaPointFromLocalPosition(localPosition);
				transferFunction.addAlphaPoint(newActivePoint);
                controlPointRenderers.Add(new ControlPointRenderer(newActivePoint, createControlPointImage(newActivePoint)));
				transferFunction.ActiveControlPoint = newActivePoint;
			}
		}
		// Right click
		else if (Input.GetMouseButton(1))
		{
			if (transferFunction.ActiveControlPoint != null)
			{
				if (transferFunction.AlphaPoints.Count > 2)
				{
					// Delete the active point that was clicked
					transferFunction.removeAlphaPoint(transferFunction.ActiveControlPoint);

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
	/// Updates the current active alpha point.
	/// </summary>
	/// <param name="data"></param>
	public void OnDrag(PointerEventData data)
    {
		// Left click
		if (Input.GetMouseButton(0))
		{
			// Get the current position of the mouse in the local coordinates of the alpha panel
			Vector2 localPosition = getLocalPositionFromScreenPosition(data.position, data.pressEventCamera);

			// Update the current active point in the transfer function
			transferFunction.updateActivePoint(getAlphaPointFromLocalPosition(localPosition));
		}
    }

	/// <summary>
	/// Finalizes the active alpha point
	/// </summary>
	/// <param name="data"></param>
	public void OnPointerUp(PointerEventData data)
    {
		transferFunction.finalizeActivePoint();
    }

    /// <summary>
    /// Updates the alpha LineRenderer graph in the user interface by updating and drawing the points.
    /// Assumes the transfer function's alpha points are already sorted.
    /// </summary>
    public void updateAlphaLineRendererGraph()
    {
        // Update the alpha points
        List<Vector2> updatedPoints = new List<Vector2>();
        List<ControlPoint> currentAlphaPoints = transferFunction.AlphaPoints;
        for (int i = 0; i < currentAlphaPoints.Count; i++)
        {
            // Convert alpha control points to local space points
            updatedPoints.Add(getLocalPositionFromAlphaPoint(currentAlphaPoints[i]));
        }

        // Use the Unity UI Extensions Line renderer to replace the old LineRenderer positions with the newly updated points
        alphaUILineRenderer.Points = updatedPoints.ToArray();

        // Update the positions of the control point renderers
        for (int i = 0; i < controlPointRenderers.Count; i++)
        {
            controlPointRenderers[i].Image.transform.localPosition = getLocalPositionFromAlphaPoint(controlPointRenderers[i].CP);
        }
	}

	/// <summary>
	/// Highlights the current active point in the transfer function.
	/// </summary>
	public void highlightActivePoint()
	{
        ControlPointRenderer activePointRenderer = getControlPointRenderer(transferFunction.ActiveControlPoint);
        if (activePointRenderer != null)
        {
            activePointRenderer.Image.GetComponent<Image>().color = pointHighlightColor;
        }
    }

    /// <summary>
    /// De-highlights the current active point in the transfer function.
    /// </summary>
    public void dehighlightPoints()
	{
        for (int i = 0; i < controlPointRenderers.Count; i++)
        {
            controlPointRenderers[i].Image.GetComponent<Image>().color = pointColor;
        }
    }

	/// <summary>
	/// Converts the given alpha control point to the local space within the alpha panel.
	/// </summary>
	/// <param name="alphaPoint"></param>
	/// <returns></returns>
	private Vector2 getLocalPositionFromAlphaPoint(ControlPoint alphaPoint)
	{
		Vector2 localPosition = new Vector2();

		// Convert to a local position within the alpha panel
		localPosition.x = (alphaPoint.isovalue / (float) transferFunction.IsovalueRange) * maxWidth;
		localPosition.y = alphaPoint.color.a * maxHeight;

		// Clamp the position to stay within the alpha panel's borders
		localPosition = clampToAlphaPanel(localPosition);

		return localPosition;
	}

	/// <summary>
	/// Converts the given screen position to a local space within the alpha panel.
	/// </summary>
	/// <param name="screenPosition"></param>
	/// <param name="cam"></param>
	/// <returns></returns>
	private Vector2 getLocalPositionFromScreenPosition(Vector2 screenPosition, Camera cam)
	{
		Vector2 localPosition;

		// Convert to a local position within the alpha panel
		RectTransformUtility.ScreenPointToLocalPointInRectangle(drawCanvasRectTransform, screenPosition, cam, out localPosition);

		// Clamp the position to stay within the alpha panel's borders
		localPosition = clampToAlphaPanel(localPosition);

		return localPosition;
	}

	/// <summary>
	/// Generates an alpha control point to be sent to the transfer function.
	/// The given offset must be clamped to [0, 1]
	/// </summary>
	/// <param name="localPosition"></param>
	/// <returns></returns>
	private ControlPoint getAlphaPointFromLocalPosition(Vector2 localPosition)
	{
		ControlPoint cp = new ControlPoint(
			(localPosition.y) / maxHeight,																// alpha value
			Mathf.FloorToInt(transferFunction.IsovalueRange * ((localPosition.x) / maxWidth))       // isovalue index
			);
		return cp;
	}

	/// <summary>
	/// Clamps the given point to be within the borders of the alpha panel.
	/// </summary>
	/// <param name="localPoint"></param>
	/// <returns></returns>
	private Vector2 clampToAlphaPanel(Vector2 localPoint)
	{
		localPoint.x = Mathf.Clamp(localPoint.x, minWidth, maxWidth);
		localPoint.y = Mathf.Clamp(localPoint.y, minHeight, maxHeight);
		return localPoint;
	}

	/// <summary>
	/// Returns the alpha control point that was clicked by the user. Returns null if no point was clicked.
	/// The given localClickPosition is assumed to be in the local coordinates of the panel.
	/// </summary>
	/// <param name="localClickPosition"></param>
	/// <returns></returns>
	private ControlPoint getClickedPoint(Vector2 localClickPosition)
	{
		List<ControlPoint> alphaPoints = transferFunction.AlphaPoints;
		for (int i = 0; i < alphaPoints.Count; i++)
		{
			if (Vector2.Distance(localClickPosition, getLocalPositionFromAlphaPoint(alphaPoints[i])) < pointRadius)
			{
				return alphaPoints[i];
			}
		}
		return null;
	}

    /// <summary>
    /// Creates a new control point image GameObject to use for rendering the given control point on the alpha canvas.
    /// </summary>
    /// <param name="cp"></param>
    /// <returns></returns>
    private GameObject createControlPointImage(ControlPoint cp)
    {
        GameObject newControlPointImage = Instantiate(controlPointImagePrefab);
        newControlPointImage.transform.SetParent(alphaCanvas.transform);
        newControlPointImage.transform.localPosition = getLocalPositionFromAlphaPoint(cp);
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
    /// Deletes all of the alpha control point renderers.
    /// </summary>
    public void deleteAllControlPointRenderers()
    {
        // Delete associated rendererers, if there are any
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
    /// Creates a brand new set of control point renderers for each of the alpha control points.
    /// </summary>
    public void createNewControlPointRenderers()
    {
        if (controlPointRenderers != null)
        {
            for (int i = 0; i < transferFunction.AlphaPoints.Count; i++)
            {
                controlPointRenderers.Add(new ControlPointRenderer(transferFunction.AlphaPoints[i],
                                                                   createControlPointImage(transferFunction.AlphaPoints[i]))
                                         );
            }
        }
    }
}
