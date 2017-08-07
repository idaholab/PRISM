/* Transfer Function | Marko Sterbentz 7/5/2017 */

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The transfer function maps the isovalues of the volume to specific colors dependent on the control points.
/// </summary>
public class TransferFunction
{
	/* Member variables */
	private List<ControlPoint> colorPoints;          // The list of control points used for the color of the transfer function
	private List<ControlPoint> alphaPoints;          // The list of control points used for the alpha of the transfer function
	private ControlPoint activeControlPoint;         // The control point currently selected by the user
	private int isovalueRange;                       // The range of possible values allowed in the transfer function
	private bool transferFunctionChanged;            // Flag that is used to determine when to update the transfer function and send it to the shader
	private Texture2D transferTexture;               // The texture that represents the transfer function and is buffered to the shader to be sampled

	/* Properties */
	public List<ControlPoint> ColorPoints
	{
		get
		{
			return colorPoints;
		}
		set
		{
			colorPoints = value;
		}
	}
	public List<ControlPoint> AlphaPoints
	{
		get
		{
			return alphaPoints;
		}
		set
		{
			alphaPoints = value;
		}
	}
	public ControlPoint ActiveControlPoint
	{
		get
		{
			return activeControlPoint;
		}
		set
		{
			activeControlPoint = value;
			transferFunctionChanged = true;
		}
	}
	public int IsovalueRange
	{
		get
		{
			return isovalueRange;
		}
		set
		{
			isovalueRange = value;
		}
	}
	public bool TransferFunctionChanged
	{
		get
		{
			return transferFunctionChanged;
		}
		set
		{
			transferFunctionChanged = value;
		}
	}
	public Texture2D TransferTexture
	{
		get
		{
			return transferTexture;
		}
		set
		{
			transferTexture = value;
		}
	}

	/* Constructor */
	/// <summary>
	/// Creates a new instance of a transfer function.
	/// </summary>
	/// <param name="_isovalueRange"></param>
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
	/// <summary>
	/// Generate the transfer texture based on the control points in colorPoints and alphaPoints.
	/// </summary>
	/// <returns></returns>
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

	/// <summary>
	/// Generates the color values for the transfer texture.
	/// The color's alpha values will be set 1.0 by this function.
	/// </summary>
	/// <param name="transferColors"></param>
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

	/// <summary>
	/// Generates the alpha values for the transfer texture.
	/// </summary>
	/// <param name="transferColors"></param>
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
	/// <summary>
	/// Adds two default points for both the color and alpha points.
	/// To ensure behavior of the transfer function is defined, endpoints must be placed at 0 and isovalueRange.
	/// </summary>
	private void addDefaultPoints()
	{
		colorPoints.Add(new ControlPoint(0.0f, 0.0f, 0.0f, 0));
		colorPoints.Add(new ControlPoint(1.0f, 1.0f, 0.85f, isovalueRange));

		alphaPoints.Add(new ControlPoint(0.0f, 0));
		alphaPoints.Add(new ControlPoint(1.0f, isovalueRange));
	}

	/// <summary>
	/// Adds the given alpha control point to the list of alpha control points.
	/// </summary>
	/// <param name="ap"></param>
	public void addAlphaPoint(ControlPoint ap)
	{
		alphaPoints.Add(ap);
		transferFunctionChanged = true;
	}

	/// <summary>
	/// Adds the given color control point to the list of color control points.
	/// </summary>
	/// <param name="cp"></param>
	public void addColorPoint(ControlPoint cp)
	{
		colorPoints.Add(cp);
		transferFunctionChanged = true;
	}

	/// <summary>
	/// Removes the given alpha control point from the list of alpha control points.
	/// </summary>
	/// <param name="ap"></param>
	public void removeAlphaPoint(ControlPoint ap)
	{
		alphaPoints.Remove(ap);
		transferFunctionChanged = true;
	}

	/// <summary>
	/// Removes the given color control points from the list of color control points.
	/// </summary>
	/// <param name="cp"></param>
	public void removeColorPoint(ControlPoint cp)
	{
		colorPoints.Remove(cp);
		transferFunctionChanged = true;
	}

	/// <summary>
	/// Sets the activeControlPoint reference to null so that this point is no longer modified.
	/// </summary>
	public void finalizeActivePoint()
	{
		activeControlPoint = null;
	}

	/// <summary>
	/// Updates the all fields in activeControlPoint by reference.
	/// </summary>
	/// <param name="newActivePoint"></param>
	public void updateActivePoint(ControlPoint newActivePoint)
	{
		activeControlPoint.updateColor(newActivePoint.color);
		activeControlPoint.isovalue = newActivePoint.isovalue;
		transferFunctionChanged = true;
	}

	/// <summary>
	/// Updates the color field in activeControlPoint by reference.
	/// </summary>
	/// <param name="newColor"></param>
	public void updateActivePoint(Color newColor)
	{
		activeControlPoint.updateColor(newColor);
		transferFunctionChanged = true;
	}

	/*****************************************************************************
	* TRANSFER FUNCTION FILE IO
	*****************************************************************************/
	/// <summary>
	/// Saves the current control points to the given path.
	/// </summary>
	/// <param name="filePath"></param>
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

	/// <summary>
	/// Loads the transfer function file at the given file path, if there is one.
	/// </summary>
	/// <param name="filePath"></param>
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

	/// <summary>
	/// Saves a png of the current transfer function texture.
	/// </summary>
	private void saveTransferTextureToFile()
	{
		System.IO.File.WriteAllBytes(Application.dataPath + @"\transferTextureCapture.png", transferTexture.EncodeToPNG());
	}
}