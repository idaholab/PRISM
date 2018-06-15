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
        //Color[] transferColors = new Color[isovalueRange * 1];
        //Color[] transferColors = new Color[255 * 255];
        int texWidth = 256;
        int texHeight = (int)Math.Round((isovalueRange / (float) texWidth));
        Color[] transferColors = new Color[texWidth * texHeight];

        // Generate the transfer texture's rgb color values
        generateTransferTextureColors(transferColors);

		// Generate the transfer texture's alpha values
		generateTransferTextureAlphas(transferColors);

        //Texture2D newTransferTexture = new Texture2D(isovalueRange, 1, TextureFormat.RGBA32, false);
        //Texture2D newTransferTexture = new Texture2D(255, 255, TextureFormat.RGBA32, false);
        Texture2D newTransferTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
        newTransferTexture.SetPixels(transferColors);
		newTransferTexture.Apply();

		return newTransferTexture;
	}

	/// <summary>
	/// Generates the color values for the transfer texture that will be sampled by the shader.
	/// The color's alpha values will be set 1.0 by this function.
	/// </summary>
	/// <param name="transferColors"></param>
	public void generateTransferTextureColors(Color[] transferColors)
	{
		// Sort the list in place by increasing isovalues
		colorPoints.Sort((x, y) => x.isovalue.CompareTo(y.isovalue));

        //// OLD: Generate the rgb color values
        //int totalDistance = 0;
        //for (int i = 0; i < colorPoints.Count - 1; i++)
        //{
        //	// Get the distance for the interpolation interval
        //	int distance = colorPoints[i + 1].isovalue - colorPoints[i].isovalue;
        //	for (int j = 0; j < distance; j++)
        //	{
        //		// Perform interpolation between the colors in the current interval
        //		transferColors[totalDistance] = Color.Lerp(colorPoints[i].color, colorPoints[i + 1].color, (j / (float)distance));
        //		transferColors[totalDistance].a = 1.0f;
        //		//transferColors[totalDistance + isovalueRange] = transferColors[totalDistance];
        //		totalDistance++;
        //	}
        //}

        ControlPoint start = colorPoints[0];
        ControlPoint end = colorPoints[1];
        int cpIndex = 1;
        for (int i = 0; i < isovalueRange + 1; i++)
        {
            // Check if the we've iterated past the end control point; update range if so
            if (i > end.isovalue && cpIndex < colorPoints.Count - 1)
            {
                cpIndex++;
                start = end;
                end = colorPoints[cpIndex]; 
            }

            float lerpPos = (i - start.isovalue) / (float) (end.isovalue - start.isovalue);
            transferColors[i] = Color.Lerp(start.color, end.color, lerpPos);
        }
	}

    /// <summary>
    /// Generates the alpha values for the transfer texture that will be sampled by the shader.
    /// </summary>
    /// <param name="transferColors"></param>
    public void generateTransferTextureAlphas(Color[] transferColors)
	{
		// Sort the list in place by increasing isovalues
		alphaPoints.Sort((x, y) => x.isovalue.CompareTo(y.isovalue));

        //// OLD: Generate the alpha values
        //int totalDistance = 0;
        //for (int i = 0; i < alphaPoints.Count - 1; i++)
        //{
        //	// Get the distance for the interpolation interval
        //	int distance = alphaPoints[i + 1].isovalue - alphaPoints[i].isovalue;
        //	for (int j = 0; j < distance; j++)
        //	{
        //		// Perform interpolation between the alphas in the current interval
        //		transferColors[totalDistance].a = Mathf.Lerp(alphaPoints[i].color.a, alphaPoints[i + 1].color.a, (j / (float)distance));
        //		//transferColors[totalDistance + isovalueRange].a = transferColors[totalDistance].a;
        //		totalDistance++;
        //	}
        //}

        ControlPoint start = alphaPoints[0];
        ControlPoint end = alphaPoints[1];
        int cpIndex = 1;
        for (int i = 0; i < isovalueRange + 1; i++)
        {
            // Check if the we've iterated past the end control point; update range if so
            if (i > end.isovalue && cpIndex < alphaPoints.Count - 1)
            {
                cpIndex++;
                start = end;
                end = alphaPoints[cpIndex];
            }

            float lerpPos = (i - start.isovalue) / (float)(end.isovalue - start.isovalue);
            transferColors[i].a = Mathf.Lerp(start.color.a, end.color.a, lerpPos);
        }
    }

    /// <summary>
    /// Generates a transfer texture with size 256 x 2 to be used for display to the Color Panel.
    /// TODO: NEEDS TO BE IMPLEMENTED
    /// </summary>
    /// <returns></returns>
    public Texture2D generateDisplayTransferTexture()
    {
        // Initialize the array of colors for the pixels
        Color[] transferColors = new Color[255 * 1];

        // Generate the transfer texture's rgb color values
        generateDisplayTransferTextureColors(transferColors);

        Texture2D newTransferTexture = new Texture2D(255, 1, TextureFormat.RGBA32, false);
        newTransferTexture.SetPixels(transferColors);
        newTransferTexture.Apply();

        return newTransferTexture;
    }

    /// <summary>
    /// Creates a set of colors based on the transfer function's color control points by normalizing the isovalues range to a texture that is 255 x 2 in size.
    /// </summary>
    /// <param name="transferColors"></param>
    public void generateDisplayTransferTextureColors(Color[] transferColors)
    {
        // Sort the list in place by increasing isovalues
        colorPoints.Sort((x, y) => x.isovalue.CompareTo(y.isovalue));

        // Generate the rgb color values
        int totalDistance = 0;
        for (int i = 0; i < colorPoints.Count - 1; i++)
        {
            // Get the distance for the interpolation interval
            int distance = (int) Math.Round((colorPoints[i + 1].isovalue - colorPoints[i].isovalue) / (double) isovalueRange * 255.0f);    // divide by isovalue range and multiply by texture width to normalize values in this range
            for (int j = 0; j < distance; j++)
            {
                // Perform interpolation between the colors in the current interval
                transferColors[totalDistance] = Color.Lerp(colorPoints[i].color, colorPoints[i + 1].color, (j / (float)distance));
                transferColors[totalDistance].a = 1.0f;
                //transferColors[totalDistance + isovalueRange] = transferColors[totalDistance];
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

			//// Scale the transfer function points to the 16-bit range (0-65535)
			//for (int i = 0; i < points.alphaPoints.Count; i++)
			//{
			//	points.alphaPoints[i].isovalue = (int)Mathf.Clamp((float)Math.Round((points.alphaPoints[i].isovalue / (float) isovalueRange) * 65535), 0, 65535);
			//}

			//for (int i = 0; i < points.colorPoints.Count; i++)
			//{
			//	points.colorPoints[i].isovalue = (int)Mathf.Clamp((float)Math.Round((points.colorPoints[i].isovalue / (float) isovalueRange) * 65535), 0, 65535);
			//}

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

			//// Scale the transfer function points to the current isovalue range (added for 16-bit support)
			//for (int i = 0; i < newPoints.alphaPoints.Count; i++)
			//{
			//	newPoints.alphaPoints[i].isovalue = (int) Mathf.Clamp((float)Math.Round((newPoints.alphaPoints[i].isovalue / 65535.0f) * isovalueRange), 0, isovalueRange);
			//}

			//for (int i = 0; i < newPoints.colorPoints.Count; i++)
			//{
			//	newPoints.colorPoints[i].isovalue = (int)Mathf.Clamp((float)Math.Round((newPoints.colorPoints[i].isovalue / 65535.0f) * isovalueRange), 0, isovalueRange);
			//}

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