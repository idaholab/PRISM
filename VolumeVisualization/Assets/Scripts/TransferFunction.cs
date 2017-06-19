// Written by stermj

using System.Collections.Generic;
using UnityEngine;

public class TransferFunction : MonoBehaviour {

    // Lists to hold control points
    private List<ControlPoint> colorPoints;
    private List<ControlPoint> alphaPoints;

    // Texture that is sent to the GPU for sampling the transfer function
    private Texture2D transferTexture;
    private int isovalueRange;           // This width is equivalent to the range of the data (e.g. 8-bit raw has 256 distinct values)
    private bool transferFunctionChanged;

	// Use this for initialization
	void Start () {
        // Initialize the list of control points
        colorPoints = new List<ControlPoint>();
        alphaPoints = new List<ControlPoint>();

        // Initialize the range of the isovalues
        isovalueRange = 256;                                                                                                // TODO: Dynamically generate this, rather than baking it in...

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

        // Update the transfer function's texture
        generateTransferTexture();

        // Buffer over the transfer function's texture
        GetComponent<Renderer>().material.SetTexture("_TransferFunctionTex", transferTexture);

        transferFunctionChanged = false;
	}
	
	// Update is called once per frame
	void Update () {
        //if (transferFunctionChanged)
        //{
        //    // Update the transfer function texture
        //    updateTransferTexture();

        //    // Buffer over the new texture to the GPU
        //    GetComponent<Renderer>().material.SetTexture("_TransferFunctionTex", transferTexture);

        //    // Wait for any more changes
        //    transferFunctionChanged = false;
        //}
	}

    // Generates the transfer texture based on the control points in colorPoints and alphaPoints
    private void generateTransferTexture()
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
                transferColors[totalDistance] = Color.Lerp(colorPoints[i].color, colorPoints[i + 1].color, distance);
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
                transferColors[totalDistance].a = Mathf.Lerp(alphaPoints[i].color.a, alphaPoints[i + 1].color.a, distance);
                transferColors[totalDistance + isovalueRange].a = transferColors[totalDistance].a;
                totalDistance++;
            }
        }

        transferTexture = new Texture2D(isovalueRange, 2, TextureFormat.RGBA32, false);
        transferTexture.SetPixels(transferColors);
        transferTexture.Apply();
    }

    // Depending on where the user clicked, add a control point to the corresponding list with the desired values
    private void addControlPoint()
    {

    }

    // Determine which control point was clicked and update the value
    private void updateControlPoint()
    {
        
    }

    // Saves a pdf of the current transfer function texture.
    private void saveTransferTextureToFile()
    {
        System.IO.File.WriteAllBytes(Application.dataPath + @"\transferTextureCapture.png", transferTexture.EncodeToPNG());
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
}
