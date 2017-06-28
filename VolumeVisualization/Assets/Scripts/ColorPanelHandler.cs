using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Vectrosity;

public class ColorPanelHandler : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
	public Canvas colorCanvas;                                  // The canvas that overlays the color panel and will be used to draw the color graph.
	//public GameObject colorPalettePanel;						// The panel that will be used to select a color for a control point. 

    private TransferFunction transferFunction;                  // The transfer function that modifies the volume when visualizing.
	//private ColorPaletteHandler colorPaletteHandler;			// The color palette that modifies the color of the current active point in the color panel.

    private RectTransform panelRectTransform;					// The RectTransform of the color panel.
    private float maxWidth;                                     // The local maximum width of the color panel.
	private float minWidth, minHeight;							// The local minimum width and height of the color panel.
    private int isovalueRange;									// The range of possible isovalues for the current volume.							// TODO: Dynamically generate this, rather than baking it in...

	private VectorLine colorGraphPoints;                        // The Vectrosity points that will be drawn for the color points.

	private float pointRadius = 10.0f;							// Radius of the Vectrosity points in the graph.
	private float borderSize = 10.0f;							// Size of the border around the panel. Padding on the internal edges of the panel is added. CAUSES BUGS WHEN NOT 0.0

	//public Color defaultColor = Color.red;						// The default color choice when creating a new color control point.

    // Use this for initialization
    void Start () {
        panelRectTransform = transform as RectTransform;
        maxWidth = panelRectTransform.rect.width - borderSize;
		minWidth = borderSize;
		minHeight = borderSize;
        transferFunction = (TransferFunction)GameObject.Find("Transfer Function Panel").GetComponent(typeof(TransferFunction));
        isovalueRange = 255;

		// Initialize the color graph points VectorLine
		colorGraphPoints = new VectorLine("colorGraphPoints", new List<Vector2>(), pointRadius, LineType.Points);
		colorGraphPoints.color = Color.white;
		colorGraphPoints.SetCanvas(colorCanvas, false);
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnPointerDown(PointerEventData data)
    {
		// Get the local position within the color panel
		Vector2 localPosition = getLocalPositionFromScreenPosition(data.position, data.pressEventCamera);

		// Check if there is a control point at the localPosition, and return it if there is one
		transferFunction.setActivePoint(getClickedPoint(localPosition));

		// Left click
		if (Input.GetMouseButton(0))
		{
			if (transferFunction.getActivePoint() == null)
			{
				// If no point is clicked, generate a new point to be added to the transfer function
				ControlPoint newActivePoint = getColorPointFromOffset(localPosition, transferFunction.getColorPalette().getCurrentColor());
				transferFunction.addColorPoint(newActivePoint);
				transferFunction.setActivePoint(newActivePoint);
			}

			// Open the color palette
			transferFunction.openColorPalette();
			transferFunction.getColorPalette().setCurrentColor(transferFunction.getActivePoint().color);
			transferFunction.getColorPalette().setSliders();
		}
		// Right click
		else if (Input.GetMouseButton(1))
		{
			if (transferFunction.getActivePoint() != null)
			{
				transferFunction.removeColorPoint(transferFunction.getActivePoint());
			}
		}
    }

    public void OnDrag(PointerEventData data)
    {
		// Left click
		if (Input.GetMouseButton(0))
		{
			// Get the current position of the mouse in the local coordinates of the color panel
			Vector2 localPosition = getLocalPositionFromScreenPosition(data.position, data.pressEventCamera);

			// Update the current active point in the transfer function
			transferFunction.updateActivePoint(getColorPointFromOffset(localPosition, transferFunction.getActivePoint().color));
		}
	}

    public void OnPointerUp(PointerEventData data)
    {
		// Finalizes the temporary color point
		//transferFunction.finalizeActivePoint();
		Debug.Log("Color panel pointer up activated!");
	}

	// Updates the color Vectrosity graph in the user interface by updating and drawing the points.
	// Note: Assumes the transfer function's color points are already sorted.
	public void updateColorVectrosityGraph()
	{
		List<Vector2> updatedPoints = new List<Vector2>();
		List<ControlPoint> currentColorPoints = transferFunction.getColorPoints();
		for (int i = 0; i < currentColorPoints.Count; i++)
		{
			// Convert color control points to local space points and store them in the Vectrosity line
			updatedPoints.Add(getLocalPositionFromColorPoint(currentColorPoints[i]));
		}

		// Have the Vectrosity points reference the control point positions
		colorGraphPoints.points2 = updatedPoints;

		// Draw Vectrosity graph
		colorGraphPoints.Draw();
	}

	// Converts the given color control point to the local space within the color panel
	public Vector2 getLocalPositionFromColorPoint(ControlPoint colorPoint)
	{
		Vector2 localPosition = new Vector2();

		// Convert to a local position within the color panel
		localPosition.x = (colorPoint.isovalue / (float)isovalueRange) * maxWidth;
		localPosition.y = 0.0f;

		// Clamp the position to stay within the color panel's borders
		localPosition = clampToColorPanel(localPosition);

		return localPosition;
	}

	// Converts the given screen position to a local space within the color panel.
	private Vector2 getLocalPositionFromScreenPosition(Vector2 screenPosition, Camera cam)
	{
		Vector2 localPosition;

		// Convert to a local position within the color panel
		RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, screenPosition, cam, out localPosition);

		// Clamp the position to stay within the color panel's borders
		localPosition = clampToColorPanel(localPosition);

		return localPosition;
	}

	// Generates a color control point to be sent to the transfer function.
	private ControlPoint getColorPointFromOffset(Vector2 offset, Color color)
	{
		ControlPoint cp = new ControlPoint(
			color,												// color value
			Mathf.FloorToInt(isovalueRange * (offset.x / maxWidth))		// isovalue index
			);
		return cp;
	}

	// Clamps the given point to be within the borders of the color panel.
	private Vector2 clampToColorPanel(Vector2 localPoint)
	{
		localPoint.x = Mathf.Clamp(localPoint.x, minWidth, maxWidth);
		localPoint.y = 0;
		return localPoint;
	}

	// Returns the color control point that was clicked by the user. Returns null if no point was clicked.
	// Note: The given localClickPosition is assumed to be in the local coordinates of the panel.
	private ControlPoint getClickedPoint(Vector2 localClickPosition)
	{
		List<ControlPoint> colorPoints = transferFunction.getColorPoints();
		for (int i = 0; i < colorPoints.Count; i++)
		{
			if (Vector2.Distance(localClickPosition, getLocalPositionFromColorPoint(colorPoints[i])) < pointRadius)
			{
				return colorPoints[i];
			}
		}
		return null;
	}

	public TransferFunction getTransferFunction()
	{
		return transferFunction;
	}
}
