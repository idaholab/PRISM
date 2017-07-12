/* Transfer Function | Marko Sterbentz 6/8/2017
 * This script provides functionality for setting up the transfer function based on user input.
 * Note: If the transfer function is not being used, the shader must be changed to no longer sample from the texture this script generates.
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TransferFunctionHandler : MonoBehaviour {

	private VolumeController volumeController;      // The main controller used to synchronize data input, user input, and visualization
	private TransferFunction transferFunction;      // Reference to the transfer function that is in the volumeController

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

	// Transfer function IO variables
	public Dropdown dropdownMenu;                   // The drop down menu that will be used for loading and saving transfer functions
	private string currentTransferFunctionFile;     // The file that is currently selected to be loaded
	private string savedTransferFunctionFolderPath; // The path to the assets/resource folder that holds the saved transfer functions
	private string transferFunctionFileExtension;	// The extension that is the transfer function JSON files use
	private Button loadButton;						// The button used to load the current transfer function
	private Button saveButton;						// The button used to save the current transfer function

	// Use this for initialization
	void Start () {
		// Set up the reference to the VolumeController
		volumeController = (VolumeController) GameObject.Find("VolumeController").GetComponent(typeof(VolumeController));

		// Initialize the reference to the transfer function stored in the volume controller
		transferFunction = volumeController.getTransferFunction();

		// Initialize the panel script references
		alphaPanelHandler = (AlphaPanelHandler)alphaPanel.GetComponent(typeof(AlphaPanelHandler));
		colorPanelHandler = (ColorPanelHandler)colorPanel.GetComponent(typeof(ColorPanelHandler));
		colorPaletteHandler = (ColorPalette)colorPalettePanel.GetComponent(typeof(ColorPalette));

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
		transferFunction.transferFunctionChanged = true;
	}
	
	// Update is called once per frame
	void Update () {
		if (transferFunction.transferFunctionChanged)
		{
			// Update the transfer function UI and shader texture
			updateTransferFunction();

			// Reset the flag
			transferFunction.transferFunctionChanged = false;
		}
	}

	/*****************************************************************************
	* GENERAL UPDATE FUNCTION
	*****************************************************************************/
	// Updates both the transfer function UI and buffers over the transfer function texture to the shader.
	private void updateTransferFunction()
	{
		// Update the transfer function texture
		transferFunction.transferTexture = transferFunction.generateTransferTexture();

		// Buffer over the new texture to the GPU
		//if (volume != null)
		//{
		//	volume.GetComponent<Renderer>().material.SetTexture("_TransferFunctionTex", transferFunction.transferTexture);
		//}

		// TODO: Tell the transfer function to buffer over the new transfer texture to each of the materials
		volumeController.updateMaterialPropTexture2DAll("_TransferFunctionTex", transferFunction.transferTexture);
		//volumeController.testBrick.getGameObject().GetComponent<Renderer>().material.SetTexture("_TransferFunctionTex", transferFunction.transferTexture);

		// Generate the texture to display in the user interface
		Color[] transferColors = new Color[transferFunction.getIsovalueRange() * 2];
		transferFunction.generateTransferTextureColors(transferColors);

		Texture2D colorTextureDisplay = new Texture2D(transferFunction.getIsovalueRange(), 2, TextureFormat.RGBA32, false);
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
	* CONTROL POINT HANDLERS
	*****************************************************************************/
	// Highlights the currently active point in the appropriate panel.
	public void highlightActivePoint()
	{
		if (transferFunction.getActivePoint() != null)
		{
			if (transferFunction.alphaPoints.Contains(transferFunction.getActivePoint()))
			{
				alphaPanelHandler.highlightActivePoint();
			}
			else if (transferFunction.colorPoints.Contains(transferFunction.getActivePoint()))
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
	// Saves the current transfer function to the currently selected file.
	// Note: This is a wrapper needed for the button click to work.
	public void saveTransferFunction()
	{
		string path = savedTransferFunctionFolderPath + currentTransferFunctionFile + transferFunctionFileExtension;
		transferFunction.saveTransferFunction(path);
	}

	// Loads the currently selected transfer function file.
	// Note: This is a wrapper needed for the button click to work.
	public void loadTransferFunction()
	{
		string path = savedTransferFunctionFolderPath + currentTransferFunctionFile + transferFunctionFileExtension;
		transferFunction.loadTransferFunction(path);
	}

	/*****************************************************************************
	 * ACCESSORS AND MUTATORS
	 *****************************************************************************/
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
}

/* Transfer Function | Marko Sterbentz 7/5/2017
 * This class is the transfer function that maps the isovalues of the volume to a specific color dependent on the control points.
 */ 
public class TransferFunction
{
	public List<ControlPoint> colorPoints;          // The list of control points used for the color of the transfer function
	public List<ControlPoint> alphaPoints;          // The list of control points used for the alpha of the transfer function
	private ControlPoint activeControlPoint;        // The control point currently selected by the user
	private int isovalueRange;                      // The range of possible values allowed in the transfer function
	public bool transferFunctionChanged;            // Flag that is used to determine when to update the transfer function and send it to the shader
	public Texture2D transferTexture;              // The texture that represents the transfer function and is buffered to the shader to be sampled

	public TransferFunction(int _isovalueRange)
	{
		colorPoints = new List<ControlPoint>();
		alphaPoints = new List<ControlPoint>();
		activeControlPoint = null;
		isovalueRange = _isovalueRange;
		transferFunctionChanged = false;
		addDefaultPoints();
	}

	/*****************************************************************************
	* TRANSFER FUNCTION TEXTURE METHODS
	*****************************************************************************/
	// Generate the transfer texture based on the control points in colorPoints and alphaPoints
	public Texture2D generateTransferTexture()
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
	public void generateTransferTextureColors(Color[] transferColors)
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
	public void generateTransferTextureAlphas(Color[] transferColors)
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
	// Adds two default points for both the color and alpha points.
	// Note: To ensure behavior of the transfer function is defined, endpoints must be placed at 0 and isovalueRange.
	private void addDefaultPoints()
	{
		colorPoints.Add(new ControlPoint(0.0f, 0.0f, 0.0f, 0));
		colorPoints.Add(new ControlPoint(1.0f, 1.0f, 0.85f, isovalueRange));

		alphaPoints.Add(new ControlPoint(0.0f, 0));
		alphaPoints.Add(new ControlPoint(1.0f, isovalueRange));
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

	// Updates the all fields in activeControlPoint by reference.
	public void updateActivePoint(ControlPoint newActivePoint)
	{
		activeControlPoint.color = newActivePoint.color;
		activeControlPoint.isovalue = newActivePoint.isovalue;
		transferFunctionChanged = true;
	}

	// Updates the color field in activeControlPoint by reference.
	public void updateActivePoint(Color newColor)
	{
		activeControlPoint.color = newColor;
		transferFunctionChanged = true;
	}

	/*****************************************************************************
	 * ACCESSORS AND MUTATORS
	 *****************************************************************************/
	// Returns the range of possible isovalues for this transfer function.
	public int getIsovalueRange()
	{
		return isovalueRange;
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

	// Returns the currently active control point.
	public ControlPoint getActivePoint()
	{
		return activeControlPoint;
	}

	// Set activeControlPoint reference to point to the given newActivePoint.
	public void setActivePoint(ControlPoint newActivePoint)
	{
		activeControlPoint = newActivePoint;
		transferFunctionChanged = true;
	}

	/*****************************************************************************
	* TRANSFER FUNCTION FILE IO
	*****************************************************************************/
	// Saves the current control points to the given path
	public void saveTransferFunction(string filePath)
	{
		try
		{
			// Create a temporary serializeable object for saving
			ControlPointLists points = new ControlPointLists();
			points.alphaPoints = alphaPoints;
			points.colorPoints = colorPoints;

			// Write the points object to JSON
			string jsonString = JsonUtility.ToJson(points, true);

			// Write the JSON to the file specified by the path
			StreamWriter writer = new StreamWriter(filePath, false);
			writer.Write(jsonString);
			writer.Close();

			Debug.Log("Transfer function saved.");

		}
		catch (Exception e)
		{
			Debug.Log("Failed to save transfer function due to exception: " + e);
		}
	}

	// Loads the transfer function file at the given file path, if there is one.
	public void loadTransferFunction(string filePath)
	{
		try
		{
			// Read the JSON file specified by the path
			StreamReader reader = new StreamReader(filePath);
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
