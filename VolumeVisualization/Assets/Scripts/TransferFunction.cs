// Written by stermj

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TransferFunction : MonoBehaviour {

	// References to other Unity objects
    public GameObject volume;						// Game object to be modified by the transfer function
    public RawImage transferTextureDisplay;			// Texture that provides a preview of the transfer function
    public GameObject alphaPanel;					// Panel that captures user input when changing the transfer function
    public GameObject colorPanel;                   // Panel that captures user input when changing the transfer function
	public GameObject colorPalettePanel;			// Panel that captures user input when modifying the active point's color

	// Scripts that the alpha and color panels use
	private AlphaPanelHandler alphaPanelHandler;	// The script that runs the alpha panel
	private ColorPanelHandler colorPanelHandler;    // The script that runs the color panel
	private ColorPaletteHandler colorPaletteHandler;// The script that runs the color palette panel

    // Lists to hold control points
    private List<ControlPoint> colorPoints;			// The list of control points used for the color of the transfer function
    private List<ControlPoint> alphaPoints;         // The list of control points used for the alpha of the transfer function
	private ControlPoint activeControlPoint;		// The control point currently selected by the user

    private Texture2D transferTexture;				// The texture that represents the transfer function and is buffered to the shader to be sampled
    private int isovalueRange;						// This width is equivalent to the range of the data (e.g. 8-bit raw has 256 distinct values)
    private bool transferFunctionChanged;			// Flag that is used to determine when to update the transfer function and send it to the shader

	// Use this for initialization
	void Start () {
        // Initialize the list of control points
        colorPoints = new List<ControlPoint>();
        alphaPoints = new List<ControlPoint>();

        // Initialize the range of the isovalues
        isovalueRange = 255;                                                                                                // TODO: Dynamically generate this, rather than baking it in...

		// Initialize the panel script references
		alphaPanelHandler = (AlphaPanelHandler)alphaPanel.GetComponent(typeof(AlphaPanelHandler));
		colorPanelHandler = (ColorPanelHandler)colorPanel.GetComponent(typeof(ColorPanelHandler));
		colorPaletteHandler = (ColorPaletteHandler)colorPalettePanel.GetComponent(typeof(ColorPaletteHandler));

        // Add the default points (need two endpoints for each list)
        colorPoints.Add(new ControlPoint(0.0f, 0.0f, 0.0f, 0));
        colorPoints.Add(new ControlPoint(1.0f, 1.0f, 0.85f, isovalueRange));

        alphaPoints.Add(new ControlPoint(0.0f, 0));
        alphaPoints.Add(new ControlPoint(1.0f, isovalueRange));

        // Add some extra points for testing the transfer function
        colorPoints.Add(new ControlPoint(.91f, .7f, .61f, 80));
        colorPoints.Add(new ControlPoint(1.0f, 1.0f, .85f, 82));

        alphaPoints.Add(new ControlPoint(0.0f, 40));
        alphaPoints.Add(new ControlPoint(0.2f, 60));
        alphaPoints.Add(new ControlPoint(0.05f, 63));
        alphaPoints.Add(new ControlPoint(0.0f, 80));
        alphaPoints.Add(new ControlPoint(0.9f, 82));

		// Reset the flag
		transferFunctionChanged = true;
	}
	
	// Update is called once per frame
	void Update () {
        if (transferFunctionChanged)
        {
			// Update the transfer function UI and shader texture
			updateTransferFunction();

            // Reset the flag
            transferFunctionChanged = false;
        }
    }

	// Updates both the transfer function UI and buffers over the transfer function texture to the shader.
	private void updateTransferFunction()
	{
		// Update the transfer function texture
		transferTexture = generateTransferTexture();

		// Buffer over the new texture to the GPU
		if (volume != null)
		{
			volume.GetComponent<Renderer>().material.SetTexture("_TransferFunctionTex", transferTexture);
		}

		// Set the texture to display in the user interface
		transferTextureDisplay.texture = transferTexture;

		// Update the Vectrosity graph for the alpha panel
		alphaPanelHandler.updateAlphaVectrosityGraph();

		// Update the Vectrosity graph for the color panel
		colorPanelHandler.updateColorVectrosityGraph();

	}

    // Generates the transfer texture based on the control points in colorPoints and alphaPoints
    private Texture2D generateTransferTexture()
    {
        // Sort the lists in place by increasing isovalues
        colorPoints.Sort((x, y) => x.isovalue.CompareTo(y.isovalue));
        alphaPoints.Sort((x, y) => x.isovalue.CompareTo(y.isovalue));

        // Linearly interpolate between control points to generate the texture
        Color[] transferColors = new Color[isovalueRange * 2];

        // Generate the rgb color values
        int totalDistance = 0;
        for (int i = 0; i < colorPoints.Count - 1; i++)
        {
            // Get the distance for the interpolation interval
            int distance = colorPoints[i + 1].isovalue - colorPoints[i].isovalue;
            for (int j = 0; j < distance; j++)
            {
                // Perform interpolation between the colors in the current interval
                transferColors[totalDistance] = Color.Lerp(colorPoints[i].color, colorPoints[i + 1].color, (j / (float)distance));
                transferColors[totalDistance + isovalueRange] = transferColors[totalDistance];
                totalDistance++;
            }
        }

        // Generate the alpha values
        totalDistance = 0;
        for (int i = 0; i < alphaPoints.Count - 1; i++)
        {
            // Get the distance for the interpolation interval
            int distance = alphaPoints[i + 1].isovalue - alphaPoints[i].isovalue;
            for (int j = 0; j < distance; j++)
            {
                // Perform interpolation between the alphas in the current interval
                transferColors[totalDistance].a = Mathf.Lerp(alphaPoints[i].color.a, alphaPoints[i + 1].color.a, (j / (float)distance));
                transferColors[totalDistance + isovalueRange].a = transferColors[totalDistance].a;
                totalDistance++;
            }
        }

        Texture2D newTransferTexture = new Texture2D(isovalueRange, 2, TextureFormat.RGBA32, false);
        newTransferTexture.SetPixels(transferColors);
        newTransferTexture.Apply();

		return newTransferTexture;
    }

    // Returns the offset within the canvas for the given control point
    private Vector2 getLocalPositionOnAlphaCanvas(ControlPoint alphaPoint)
    {
        return new Vector2();
    }

    private Vector2 getLocalPositionOnColorCanvas(ControlPoint colorPoint)
    {
        return new Vector2();
    }

	// Adds the given alpha control point to the list of alpha control points.
    public void addAlphaPoint(ControlPoint ap)
    {
        alphaPoints.Add(ap);
        transferFunctionChanged = true;
    }

	// Adds the given color control point to the list of color control points.
    public void addColorPoint(ControlPoint cp)
    {
        colorPoints.Add(cp);
        transferFunctionChanged = true;
    }

	// Removes the given alpha control point from the list of alpha control points.
    public void removeAlphaPoint(ControlPoint ap)
    {
        alphaPoints.Remove(ap);
		transferFunctionChanged = true;
    }

	// Removes the given alpha control points from the list of color control points.
    public void removeColorPoint(ControlPoint cp)
    {
        colorPoints.Remove(cp);
		transferFunctionChanged = true;
    }

	// Sets the activeControlPoint reference to null so that this point is no longer modified.
	public void finalizeActivePoint()
    {
		activeControlPoint = null;
    }

	// Set activeControlPoint reference to point to the given newActivePoint.
	public void setActivePoint(ControlPoint newActivePoint)
	{
		activeControlPoint = newActivePoint;
	}

	// Returns the currently active control point.
	public ControlPoint getActivePoint()
	{
		return activeControlPoint;
	}

	// Updates the all fields in activeControlPoint by reference.
	public void updateActivePoint(ControlPoint newActivePoint)
	{
		this.activeControlPoint.color = newActivePoint.color;
		this.activeControlPoint.isovalue = newActivePoint.isovalue;
		transferFunctionChanged = true;
	}

	// Updates the color field in activeControlPoint by reference.
	public void updateActivePoint(Color newColor)
	{
		this.activeControlPoint.color = newColor;
		transferFunctionChanged = true;
	}

	// Returns the list of current alpha control points.
    public List<ControlPoint> getAlphaPoints()
    {
        return alphaPoints;
    }

	// Returns the list of current color control points.
    public List<ControlPoint> getColorPoints()
    {
        return colorPoints;
    }

	// Returns the isovalueRange used this transfer function.
    public int getIsovalueRange()
    {
        return isovalueRange;
    }

    // Saves a png of the current transfer function texture.
    private void saveTransferTextureToFile()
    {
        System.IO.File.WriteAllBytes(Application.dataPath + @"\transferTextureCapture.png", transferTexture.EncodeToPNG());
    }

	// Activates the color palette panel
	public void openColorPalette()
	{
		colorPalettePanel.SetActive(true);
	}

	// Deactivates the color palette panel
	public void closeColorPalette()
	{
		colorPalettePanel.SetActive(false);
	}

	public ColorPaletteHandler getColorPalette()
	{
		return colorPaletteHandler;
	}
}

public class ControlPoint
{
    public Color color;
    public int isovalue;

    public ControlPoint(float r, float g, float b, int isovalue)
    {
        update(r, g, b, isovalue);
    }

    public ControlPoint(float alpha, int isovalue)
    {
        update(alpha, isovalue);
    }

    public ControlPoint(Color newColor, int newIsovalue)
    {
        update(newColor, newIsovalue);
    }

    public void update(Color newColor, int newIsovalue)
    {
        this.color = newColor;
        this.isovalue = newIsovalue;
    }

    public void update(float r, float g, float b, int isovalue)
    {
        this.color.r = r;
        this.color.g = g;
        this.color.b = b;
        this.color.a = 1.0f;
        this.isovalue = isovalue;
    }

    public void update(float alpha, int isovalue)
    {
        this.color.r = 0.0f;
        this.color.g = 0.0f;
        this.color.b = 0.0f;
        this.color.a = alpha;
        this.isovalue = isovalue;
    }

	public void updateColor(float r, float g, float b)
	{
		this.color.r = r;
		this.color.g = g;
		this.color.b = b;
	}

	public void updateColor(Color newColor)
	{
		this.color = newColor;
	}
}
