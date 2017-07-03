/* Transfer Function | Marko Sterbentz 6/8/2017
 * This script provides functionality for setting up the transfer function based on user input.
 * Note: If the transfer function is not being used, the shader must be changed to no longer sample from the texture this script generates.
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
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
	private ColorPalette colorPaletteHandler;		// The script that runs the color palette panel

    // Lists to hold control points
    private List<ControlPoint> colorPoints;			// The list of control points used for the color of the transfer function
    private List<ControlPoint> alphaPoints;         // The list of control points used for the alpha of the transfer function
	private ControlPoint activeControlPoint;        // The control point currently selected by the user

    private Texture2D transferTexture;				// The texture that represents the transfer function and is buffered to the shader to be sampled
    private int isovalueRange;						// This width is equivalent to the range of the data (e.g. 8-bit raw has 256 distinct values)
    private bool transferFunctionChanged;           // Flag that is used to determine when to update the transfer function and send it to the shader

	// Transfer function IO variables
	public Dropdown dropdownMenu;                   // The drop down menu that will be used for loading and saving transfer functions
	private string currentTransferFunctionFile;     // The file that is currently selected to be loaded
	private string savedTransferFunctionFolderPath; // The path to the assets/resource folder that holds the saved transfer functions
	private string transferFunctionFileExtension;	// The extension that is the transfer function JSON files use
	private Button loadButton;						// The button used to load the current transfer function
	private Button saveButton;						// The button used to save the current transfer function

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
		colorPaletteHandler = (ColorPalette)colorPalettePanel.GetComponent(typeof(ColorPalette));

        // Add the default points (need two endpoints for each list)
        colorPoints.Add(new ControlPoint(0.0f, 0.0f, 0.0f, 0));
        colorPoints.Add(new ControlPoint(1.0f, 1.0f, 0.85f, isovalueRange));

        alphaPoints.Add(new ControlPoint(0.0f, 0));
        alphaPoints.Add(new ControlPoint(1.0f, isovalueRange));

		// Set up the transfer function loading drop down menu and load the first transfer function in the list
		savedTransferFunctionFolderPath = "Assets/Resources/TransferFunctions/";
		transferFunctionFileExtension = ".txt";
		Button loadButton = (GameObject.Find("Load Button").GetComponent<Button>());
		Button saveButton = (GameObject.Find("Save Button").GetComponent<Button>());

		// Load "Assets/Resources/TransferFunctions" files into a list of strings
		List<string> fileNames = new List<string>(Directory.GetFiles(savedTransferFunctionFolderPath, "*.txt"));

		// Trim the directories from the file names
		for (int i = 0; i < fileNames.Count; i++)
		{
			fileNames[i] = Path.GetFileNameWithoutExtension(fileNames[i]);
		}

		// Populate the dropdown menu with these OptionData objects
		if (fileNames.Count > 0)
		{
			// Add the list of files to the dropdown menu
			dropdownMenu.AddOptions(fileNames);

			// Set the currentTransferFunctionFile to the first one in the list
			currentTransferFunctionFile = fileNames[0];

			// Load the control points from the currentTransferFunctionFile, if there is one
			loadTransferFunction();
		}
		else
		{
			// Populate the dropdown menu with
			dropdownMenu.AddOptions(new List<string>(new string[]{ "New Transfer Function"}));

			// Deactivate the load button
			loadButton.interactable = false;
		}

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

	/*****************************************************************************
	* GENERAL UPDATE FUNCTION
	*****************************************************************************/
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

		// Generate the texture to display in the user interface
		Color[] transferColors = new Color[isovalueRange * 2];
		generateTransferTextureColors(transferColors);

		Texture2D colorTextureDisplay = new Texture2D(isovalueRange, 2, TextureFormat.RGBA32, false);
		colorTextureDisplay.SetPixels(transferColors);
		colorTextureDisplay.Apply();

		transferTextureDisplay.texture = colorTextureDisplay;

		// Update the Vectrosity graph for the alpha panel
		alphaPanelHandler.updateAlphaVectrosityGraph();

		// Update the Vectrosity graph for the color panel
		colorPanelHandler.updateColorVectrosityGraph();

		// Highlight the active point
		highlightActivePoint();
	}

	/*****************************************************************************
	* TRANSFER FUNCTION TEXTURE METHODS
	*****************************************************************************/
	// Generate the transfer texture based on the control points in colorPoints and alphaPoints
	private Texture2D generateTransferTexture()
	{		
		// Initialize the array of colors for the pixels
		Color[] transferColors = new Color[isovalueRange * 2];

		// Generate the transfer texture's rgb color values
		generateTransferTextureColors(transferColors);

		// Generate the transfer texture's alpha values
		generateTransferTextureAlphas(transferColors);

		Texture2D newTransferTexture = new Texture2D(isovalueRange, 2, TextureFormat.RGBA32, false);
		newTransferTexture.SetPixels(transferColors);
		newTransferTexture.Apply();

		return newTransferTexture;
	}
	
	// Generates the color values for the transfer texture.
	// Note: The color's alpha values will be set 1.0 by this function.
	private void generateTransferTextureColors(Color[] transferColors)
	{
		// Sort the list in place by increasing isovalues
		colorPoints.Sort((x, y) => x.isovalue.CompareTo(y.isovalue));

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
				transferColors[totalDistance].a = 1.0f;
				transferColors[totalDistance + isovalueRange] = transferColors[totalDistance];
				totalDistance++;
			}
		}
	}

	// Generates the alpha values for the transfer texture
	private void generateTransferTextureAlphas(Color[] transferColors)
	{
		// Sort the list in place by increasing isovalues
		alphaPoints.Sort((x, y) => x.isovalue.CompareTo(y.isovalue));

		// Generate the alpha values
		int totalDistance = 0;
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
	}

	/*****************************************************************************
	* CONTROL POINT HANDLERS
	*****************************************************************************/
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

	// Highlights the currently active point in the appropriate panel.
	public void highlightActivePoint()
	{
		if (activeControlPoint != null)
		{
			if (alphaPoints.Contains(activeControlPoint))
			{
				alphaPanelHandler.highlightActivePoint();
			}
			else if (colorPoints.Contains(activeControlPoint))
			{
				colorPanelHandler.highlightActivePoint();
			}
		}
	}

	// Dehighlights points in both the color and alpha panels.
	public void dehighlightPoints()
	{
		alphaPanelHandler.dehighlightPoints();
		colorPanelHandler.dehighlightPoints();
	}

	/*****************************************************************************
	* COLOR PALETTE FUNCTIONS
	*****************************************************************************/
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

	/*****************************************************************************
	* TRANSFER FUNCTION FILE IO
	*****************************************************************************/
	// Saves the current control points to the currently selected file
	public void saveTransferFunction()
	{
		try
		{
			string path = savedTransferFunctionFolderPath + currentTransferFunctionFile + transferFunctionFileExtension;

			// Create a temporary serializeable object for saving
			ControlPointLists points = new ControlPointLists();
			points.alphaPoints = alphaPoints;
			points.colorPoints = colorPoints;

			// Write the points object to JSON
			string jsonString = JsonUtility.ToJson(points, true);

			// Write the JSON to the file specified by the path
			StreamWriter writer = new StreamWriter(path, false);
			writer.Write(jsonString);
			writer.Close();

			Debug.Log("Transfer function saved.");

		}
		catch (Exception e)
		{
			Debug.Log("Failed to save transfer function due to exception: " + e);
		}
		
	}

	// Loads the currently selected transfer function
	public void loadTransferFunction()
	{
		try
		{
			string path = savedTransferFunctionFolderPath + currentTransferFunctionFile + transferFunctionFileExtension;

			// Read the JSON file specified by the path
			StreamReader reader = new StreamReader(path);
			string textFromFile = reader.ReadToEnd();
			reader.Close();

			// Create a temporary serializeable object to store the points
			ControlPointLists newPoints = JsonUtility.FromJson<ControlPointLists>(textFromFile);
			alphaPoints = newPoints.alphaPoints;
			colorPoints = newPoints.colorPoints;

			// Tell the transfer function update
			transferFunctionChanged = true;

			Debug.Log("Transfer function loaded.");
		}
		catch (Exception e)
		{
			Debug.Log("Failed to load transfer function due to exception: " + e);
		}
	}

	// Saves a png of the current transfer function texture.
	private void saveTransferTextureToFile()
	{
		System.IO.File.WriteAllBytes(Application.dataPath + @"\transferTextureCapture.png", transferTexture.EncodeToPNG());
	}

	/*****************************************************************************
	 * ACCESSORS AND MUTATORS
	 *****************************************************************************/
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

	// Sets the current transfer function file to load in the currently selected directory.
	public void setCurrentTransferFunctionFile(string newFileName)
	{
		currentTransferFunctionFile = newFileName;
	}

	// Updates the dropdown menu to display the option at the given index.
	public void dropDownMenuChangeFile(int index)
	{
		setCurrentTransferFunctionFile(dropdownMenu.options[index].text);
	}

	// Returns the script attached to the color palette panel.
	public ColorPalette getColorPalette()
	{
		return colorPaletteHandler;
	}

	// Set activeControlPoint reference to point to the given newActivePoint.
	public void setActivePoint(ControlPoint newActivePoint)
	{
		activeControlPoint = newActivePoint;
		transferFunctionChanged = true;
	}

	// Returns the currently active control point.
	public ControlPoint getActivePoint()
	{
		return activeControlPoint;
	}
}

/*
 * Control Point Lists | Marko Sterbentz 6/30/2017
 * This class is used for saving and loading the lists of control points the transfer function uses.
 */
[Serializable]
public class ControlPointLists
{
	public List<ControlPoint> alphaPoints;
	public List<ControlPoint> colorPoints;
}

/*
 * Control Point | Marko Sterbentz 6/8/2017
 * Specifies the color and alpha value at a specific isovalue in the transfer function.
 * Note: Can be either a color or alpha control point depending on how they're used.
 */ 
 [Serializable]
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
