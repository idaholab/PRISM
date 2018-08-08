/* Transfer Function Handler | Marko Sterbentz 6/8/2017 */

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Provides functionality for setting up the transfer function based on user input.
/// If the transfer function is not being used, the shader must be changed to no longer sample from the texture this script generates.
/// </summary>
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

	/// <summary>
	/// Initialization function for the TransferFunctionHandler.
	/// </summary>
	void Start () {
		// Set up the reference to the VolumeController
		volumeController = (VolumeController)GameObject.Find("Main Camera").GetComponent(typeof(VolumeController));

		// Initialize the reference to the transfer function stored in the volume controller
		transferFunction = volumeController.TransferFunction;

		// Initialize the panel script references
		alphaPanelHandler = (AlphaPanelHandler)alphaPanel.GetComponent(typeof(AlphaPanelHandler));
		colorPanelHandler = (ColorPanelHandler)colorPanel.GetComponent(typeof(ColorPanelHandler));
		colorPaletteHandler = (ColorPalette)colorPalettePanel.GetComponent(typeof(ColorPalette));

		// Set up the transfer function loading drop down menu and load the first transfer function in the list
		if (transferFunction.IsovalueRange == 255)
			savedTransferFunctionFolderPath = "Assets/Resources/TransferFunctions8Bit/";
		else
			savedTransferFunctionFolderPath = "Assets/Resources/TransferFunctions16Bit/";
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
		transferFunction.TransferFunctionChanged = true;
	}
	
	/// <summary>
	/// Updates TransferFunctionHandler once per frame.
	/// </summary>
	void Update () {
		if (transferFunction.TransferFunctionChanged)
		{
			// Update the transfer function UI and shader texture
			updateTransferFunction();

			// Reset the flag
			transferFunction.TransferFunctionChanged = false;
		}
	}

	/*****************************************************************************
	* GENERAL UPDATE FUNCTION
	*****************************************************************************/
	/// <summary>
	/// Updates both the transfer function UI and buffers over the transfer function texture to the shader.
	/// </summary>
	private void updateTransferFunction()
	{
		// Update the transfer function texture
		transferFunction.TransferTexture = transferFunction.generateTransferTexture();

		// Buffer over the new transfer texture to each of the materials
		//volumeController.getCurrentVolume().updateMaterialPropTexture2DAll("_TransferFunctionTex", transferFunction.TransferTexture);
		volumeController.RenderingComputeShader.SetTexture(volumeController.RendererKernelID, "_TransferFunctionTexture", transferFunction.TransferTexture);

        // Generate the texture to display in the user interface
        //Color[] transferColors = new Color[transferFunction.IsovalueRange * 2];
        //transferFunction.generateTransferTextureColors(transferColors);

        //Texture2D colorTextureDisplay = new Texture2D(transferFunction.IsovalueRange, 2, TextureFormat.RGBA32, false);
  //      colorTextureDisplay.SetPixels(transferColors);
		//colorTextureDisplay.Apply();

        // Generate the texture to display in the user interface
		transferTextureDisplay.texture = transferFunction.generateDisplayTransferTexture();

		// Update the Vectrosity graph for the alpha panel
		alphaPanelHandler.updateAlphaLineRendererGraph();

		// Update the Vectrosity graph for the color panel
		colorPanelHandler.updateColorGraph();

		// Highlight the active point
		highlightActivePoint();
	}

	/*****************************************************************************
	* CONTROL POINT HANDLERS
	*****************************************************************************/
	/// <summary>
	/// Highlights the currently active point in the appropriate panel.
	/// </summary>
	public void highlightActivePoint()
	{
		if (transferFunction.ActiveControlPoint != null)
		{
			if (transferFunction.AlphaPoints.Contains(transferFunction.ActiveControlPoint))
			{
				alphaPanelHandler.highlightActivePoint();
			}
			else if (transferFunction.ColorPoints.Contains(transferFunction.ActiveControlPoint))
			{
				colorPanelHandler.highlightActivePoint();
			}
		}
	}

	/// <summary>
	/// Dehighlights points in both the color and alpha panels.
	/// </summary>
	public void dehighlightPoints()
	{
		alphaPanelHandler.dehighlightPoints();
		colorPanelHandler.dehighlightPoints();
	}

	/*****************************************************************************
	* COLOR PALETTE FUNCTIONS
	*****************************************************************************/
	/// <summary>
	/// Activates the color palette panel.
	/// </summary>
	public void openColorPalette()
	{
		colorPalettePanel.SetActive(true);
	}

	/// <summary>
	/// Deactivates the color palette panel.
	/// </summary>
	public void closeColorPalette()
	{
		colorPalettePanel.SetActive(false);
	}

	/*****************************************************************************
	* TRANSFER FUNCTION FILE IO
	*****************************************************************************/ 
	/// <summary>
	/// Saves the current transfer function to the currently selected file.
	/// This is a wrapper needed for the button click to work.
	/// </summary>
	public void saveTransferFunction()
	{
		string path = savedTransferFunctionFolderPath + currentTransferFunctionFile + transferFunctionFileExtension;
		transferFunction.saveTransferFunction(path);
	}

	/// <summary>
	/// Loads the currently selected transfer function file.
	/// This is a wrapper needed for the button click to work.
	/// </summary>
	public void loadTransferFunction()
	{
        // Create the path to the transfer function to load
		string path = savedTransferFunctionFolderPath + currentTransferFunctionFile + transferFunctionFileExtension;

        // Delete the old transfer function points
        alphaPanelHandler.deleteAllControlPointRenderers();
        colorPanelHandler.deleteAllControlPointRenderers();

        // Load in the new transfer function points from a file
		transferFunction.loadTransferFunction(path);

        // Create new control point renderers for the points that were loaded in
        alphaPanelHandler.createNewControlPointRenderers();
        colorPanelHandler.createNewControlPointRenderers();
	}

	/*****************************************************************************
	 * ACCESSORS AND MUTATORS
	 *****************************************************************************/
	/// <summary>
	/// Sets the current transfer function file to load in the currently selected directory.
	/// </summary>
	/// <param name="newFileName"></param>
	public void setCurrentTransferFunctionFile(string newFileName)
	{
		currentTransferFunctionFile = newFileName;
	}

	/// <summary>
	/// Updates the dropdown menu to display the option at the given index.
	/// </summary>
	/// <param name="index"></param>
	public void dropDownMenuChangeFile(int index)
	{
		setCurrentTransferFunctionFile(dropdownMenu.options[index].text);
	}

	/// <summary>
	/// Returns the script attached to the color palette panel.
	/// </summary>
	/// <returns></returns>
	public ColorPalette getColorPalette()
	{
		return colorPaletteHandler;
	}
}