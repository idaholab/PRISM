// written by stermj
// Important note for use: Ensure that the panel this script is attached to has its pivot set to (0,0).

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Vectrosity;

public class AlphaPanelHandler : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler {

	public Canvas alphaCanvas;						// The canvas that overlays the alpha panel and will be used to draw the alpha graph.

    private TransferFunction transferFunction;		// The transfer function that modifies the volume when visualizing.

    private RectTransform panelRectTransform;		// The RectTransform of the alpha panel.
    private float maxWidth, maxHeight;              // The local maximum width and height of the alpha panel.
	private float minWidth, minHeight;				// The local minimum width and height of the alpha panel.
    private int isovalueRange;						// The range of possible isovalues for the current volume.								// TODO: Dynamically generate this, rather than baking it in...

    private VectorLine alphaGraphLine;				// The Vectrosity line that will be drawn for the graph.
	private VectorLine alphaGraphPoints;			// The Vectrosity points that will be drawn for the graph.

	private float pointRadius = 10.0f;              // Radius of the Vectrosity points in the graph.
	private float borderSize = 10.0f;				// Size of the border around the panel. Padding on the internal edges of the panel is added. CAUSES BUGS WHEN NOT 0.0

    // Use this for initialization
    void Start()
    {
        panelRectTransform = transform as RectTransform;
		maxWidth = panelRectTransform.rect.width - borderSize;
		maxHeight = panelRectTransform.rect.height - borderSize;
		minWidth = borderSize;
		minHeight = borderSize;
        isovalueRange = 255;
        transferFunction = (TransferFunction)GameObject.Find("Transfer Function Panel").GetComponent(typeof(TransferFunction));
		

		// Initialize the alpha graph points VectorLine
		alphaGraphPoints = new VectorLine("alphaGraphPoints", new List<Vector2>(), pointRadius, LineType.Points);
		alphaGraphPoints.color = Color.white;
		alphaGraphPoints.SetCanvas(alphaCanvas, false);

		// Initialize the alpha graph line VectorLine
		alphaGraphLine = new VectorLine("alphaGraphLine", new List<Vector2>(), 2.0f, LineType.Continuous, Joins.Weld);
		alphaGraphLine.color = Color.red;
		alphaGraphLine.SetCanvas(alphaCanvas, false);
    }

    // Update is called once per frame
    void Update()
	{

	}

	// Create a temporary alpha control point that is sent to the transfer function.
	public void OnPointerDown(PointerEventData data)
    {
		// Close the color palette (if it is open)
		transferFunction.closeColorPalette();

		// Get the local position within the alpha panel
		Vector2 localPosition = getLocalPositionFromScreenPosition(data.position, data.pressEventCamera);

		// Check if there is a control point at the localPosition, and return it if there is one
		transferFunction.setActivePoint(getClickedPoint(localPosition));

		// Left click
		if (Input.GetMouseButton(0))
		{
			if (transferFunction.getActivePoint() == null)
			{
				// If no point is clicked, generate a new point to be added to the transfer function
				ControlPoint newActivePoint = getAlphaPointFromOffset(localPosition);
				transferFunction.addAlphaPoint(newActivePoint);
				transferFunction.setActivePoint(newActivePoint);
			}
		}
		// Right click
		else if (Input.GetMouseButton(1))
		{
			if (transferFunction.getActivePoint() != null)
			{
				transferFunction.removeAlphaPoint(transferFunction.getActivePoint());
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
			transferFunction.updateActivePoint(getAlphaPointFromOffset(localPosition));
		}

    }

    public void OnPointerUp(PointerEventData data)
    {
        // Finalizes the active alpha point
        transferFunction.finalizeActivePoint();
		Debug.Log("Alpha panel pointer up activated!");
    }

	// Updates the alpha Vectrosity graph in the user interface by updating and drawing the points.
	// Note: Assumes the transfer function's alpha points are already sorted.
	public void updateAlphaVectrosityGraph()
	{
		List<Vector2> updatedPoints = new List<Vector2>();
		List<ControlPoint> currentAlphaPoints = transferFunction.getAlphaPoints();
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

	// Converts the given alpha control point to the local space within the alpha panel.
	private Vector2 getLocalPositionFromAlphaPoint(ControlPoint alphaPoint)
	{
		Vector2 localPosition = new Vector2();

		// Convert to a local position within the alpha panel
		localPosition.x = (alphaPoint.isovalue / (float) isovalueRange) * maxWidth;
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
	private ControlPoint getAlphaPointFromOffset(Vector2 offset)
	{
		ControlPoint cp = new ControlPoint(
			offset.y / maxHeight,                                            // alpha value
			Mathf.FloorToInt(isovalueRange * (offset.x / maxWidth))          // isovalue index
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
		List<ControlPoint> alphaPoints = transferFunction.getAlphaPoints();
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
