using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/* Transfer Function Handler | Marko Sterbentz 6/8/2017
 * This script provides functionality for setting up the transfer function based on user input.
 * Note: If the transfer function is not being used, the shader must be changed to no longer sample from the texture this script generates.
 */
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
		transferFunction.TransferFunctionChanged = true;
	}
	
	// Update is called once per frame
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
	// Updates both the transfer function UI and buffers over the transfer function texture to the shader.
	private void updateTransferFunction()
	{
		// Update the transfer function texture
		transferFunction.TransferTexture = transferFunction.generateTransferTexture();

		// Buffer over the new transfer texture to each of the materials
		volumeController.getCurrentVolume().updateMaterialPropTexture2DAll("_TransferFunctionTex", transferFunction.TransferTexture);

		// Generate the texture to display in the user interface
		Color[] transferColors = new Color[transferFunction.IsovalueRange * 2];
		transferFunction.generateTransferTextureColors(transferColors);

		Texture2D colorTextureDisplay = new Texture2D(transferFunction.IsovalueRange, 2, TextureFormat.RGBA32, false);
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