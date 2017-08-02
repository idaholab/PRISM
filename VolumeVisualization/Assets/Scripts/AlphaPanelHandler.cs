using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Vectrosity;

/* Alpha Panel Handler | Marko Sterbentz 6/21/2017
 * This script provides functionality for handling user input for the alpha portion of the transfer function.
 * Important note for use: Ensure that the panel this script is attached to has its pivot set to (0,0).
 */
public class AlphaPanelHandler : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler {

	public Canvas alphaCanvas;								// The canvas that overlays the alpha panel and will be used to draw the alpha graph.
    private TransferFunctionHandler transferFunctionHandler;// The transfer function script that passes user input to the transfer function.
	private TransferFunction transferFunction;				// The transfer function that is used by the current volume.
	private VolumeController volumeController;				// A reference to the VolumeController that manages the renderer.

	private RectTransform panelRectTransform;				// The RectTransform of the alpha panel.
    private float maxWidth, maxHeight;						// The local maximum width and height of the alpha panel.
	private float minWidth, minHeight;						// The local minimum width and height of the alpha panel.

    private VectorLine alphaGraphLine;						// The Vectrosity line that will be drawn for the graph.
	private VectorLine alphaGraphPoints;					// The Vectrosity points that will be drawn for the graph.
	private VectorLine alphaGraphHighlightedPoint;			// The Vectrosity point that will be draw on top of the currently active point in the alpha panel.

	private float pointRadius = 10.0f;						// Radius of the Vectrosity points in the graph.
	private float borderSize = 0.0f;						// Size of the border around the panel. Padding on the internal edges of the panel is added. CAUSES BUGS WHEN NOT 0.0

    // Use this for initialization
    void Start()
    {
		panelRectTransform = transform as RectTransform;
		maxWidth = panelRectTransform.rect.width - borderSize;
		maxHeight = panelRectTransform.rect.height - borderSize;
		minWidth =  borderSize;
		minHeight = borderSize;
        transferFunctionHandler = (TransferFunctionHandler)GameObject.Find("Transfer Function Panel").GetComponent(typeof(TransferFunctionHandler));
		volumeController = (VolumeController)GameObject.Find("VolumeController").GetComponent(typeof(VolumeController));
		transferFunction = volumeController.getTransferFunction();

		// Initialize the alpha graph points VectorLine
		alphaGraphPoints = new VectorLine("alphaGraphPoints", new List<Vector2>(), pointRadius, LineType.Points);
		alphaGraphPoints.color = Color.white;
		alphaGraphPoints.SetCanvas(alphaCanvas, false);

		// Initialize the alpha graph line VectorLine
		alphaGraphLine = new VectorLine("alphaGraphLine", new List<Vector2>(), 2.0f, LineType.Continuous, Joins.Weld);
		alphaGraphLine.color = Color.red;
		alphaGraphLine.SetCanvas(alphaCanvas, false);

		// Initialize the alpha graph highlight point
		alphaGraphHighlightedPoint = new VectorLine("alphaGraphHighlightedPoint", new List<Vector2>(), pointRadius, LineType.Points);
		alphaGraphHighlightedPoint.color = Color.black;
		alphaGraphHighlightedPoint.SetCanvas(alphaCanvas, false);
    }

    // Update is called once per frame
    void Update()
	{

	}

	// Create a temporary alpha control point that is sent to the transfer function.
	public void OnPointerDown(PointerEventData data)
    {
		// Close the color palette (if it is open)
		transferFunctionHandler.closeColorPalette();

		// Get the local position within the alpha panel
		Vector2 localPosition = getLocalPositionFromScreenPosition(data.position, data.pressEventCamera);

		// Check if there is a control point at the localPosition, and return it if there is one
		transferFunction.ActiveControlPoint = getClickedPoint(localPosition);

		transferFunctionHandler.dehighlightPoints();

		// Left click
		if (Input.GetMouseButton(0))
		{
			if (transferFunction.ActiveControlPoint == null)
			{
				// If no point is clicked, generate a new point to be added to the transfer function
				ControlPoint newActivePoint = getAlphaPointFromLocalPosition(localPosition);
				transferFunction.addAlphaPoint(newActivePoint);
				transferFunction.ActiveControlPoint = newActivePoint;
			}
		}
		// Right click
		else if (Input.GetMouseButton(1))
		{
			if (transferFunction.ActiveControlPoint != null)
			{
				transferFunction.removeAlphaPoint(transferFunction.ActiveControlPoint);
			}
		}
    }

	// Updates the current active alpha point.
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

	// Finalizes the active alpha point
    public void OnPointerUp(PointerEventData data)
    {
		transferFunction.finalizeActivePoint();
    }

	// Updates the alpha Vectrosity graph in the user interface by updating and drawing the points.
	// Note: Assumes the transfer function's alpha points are already sorted.
	public void updateAlphaVectrosityGraph()
	{
		// Update the alpha points
		List<Vector2> updatedPoints = new List<Vector2>();
		List<ControlPoint> currentAlphaPoints = transferFunction.AlphaPoints;
		for (int i = 0; i < currentAlphaPoints.Count; i++)
		{
			// Convert alpha control points to local space points and store them in the Vectrosity line
			updatedPoints.Add(getLocalPositionFromAlphaPoint(currentAlphaPoints[i]));
		}

		// Have the Vectrosity lines and points reference the same control point positions
		alphaGraphLine.points2 = updatedPoints;
		alphaGraphPoints.points2 = updatedPoints;

		// Draw Vectrosity graph
		alphaGraphLine.Draw();
		alphaGraphPoints.Draw();
	}

	// Highlights the current active point in the transfer function .
	public void highlightActivePoint()
	{
		List<Vector2> highlightedPoint = new List<Vector2>();

		Vector2 activePointPosition = getLocalPositionFromAlphaPoint(transferFunction.ActiveControlPoint);
		highlightedPoint.Add(activePointPosition);
		alphaGraphHighlightedPoint.points2 = highlightedPoint;

		alphaGraphHighlightedPoint.Draw();
	}

	// De-highlights the current active point in the transfer function.
	public void dehighlightPoints()
	{
		alphaGraphHighlightedPoint.points2 = new List<Vector2>();
		alphaGraphHighlightedPoint.Draw();
	}

	// Converts the given alpha control point to the local space within the alpha panel.
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

	// Converts the given screen position to a local space within the alpha panel.
	private Vector2 getLocalPositionFromScreenPosition(Vector2 screenPosition, Camera cam)
	{
		Vector2 localPosition;

		// Convert to a local position within the alpha panel
		RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, screenPosition, cam, out localPosition);

		// Clamp the position to stay within the alpha panel's borders
		localPosition = clampToAlphaPanel(localPosition);

		return localPosition;
	}

	// Generates an alpha control point to be sent to the transfer function.
	// Note: The given offset must be clamped to [0, 1]
	private ControlPoint getAlphaPointFromLocalPosition(Vector2 localPosition)
	{
		ControlPoint cp = new ControlPoint(
			(localPosition.y) / maxHeight,																// alpha value
			Mathf.FloorToInt(transferFunction.IsovalueRange * ((localPosition.x) / maxWidth))       // isovalue index
			);
		return cp;
	}

	// Clamps the given point to be within the borders of the alpha panel.
	private Vector2 clampToAlphaPanel(Vector2 localPoint)
	{
		localPoint.x = Mathf.Clamp(localPoint.x, minWidth, maxWidth);
		localPoint.y = Mathf.Clamp(localPoint.y, minHeight, maxHeight);
		return localPoint;
	}

	// Returns the alpha control point that was clicked by the user. Returns null if no point was clicked.
	// Note: The given localClickPosition is assumed to be in the local coordinates of the panel.
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
}
