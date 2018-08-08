/* Control Point Renderer | Marko Sterbentz 6/1/2018 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A wrapper class that contains ControlPoint data and its associated GameObject needed for rendering.
/// </summary>
public class ControlPointRenderer {

    /* Member Variables */
    private ControlPoint cp;
    private GameObject image;

    /* Properties */
    public ControlPoint CP
    {
        get { return cp; }
        set { cp = value; }
    }

    public GameObject Image
    {
        get { return image; }
        set { image = value; }
    }

    /* Contructors */
    public ControlPointRenderer()
    {
        CP = null;
        Image = null;
    }

    public ControlPointRenderer(ControlPoint _cp, GameObject _image)
    {
        CP = _cp;
        Image = _image;
    }

    /* Methods */
    /// <summary>
    /// Properly destoys the image GameObject and prepares the object for deletion.
    /// </summary>
    public void destruct()
    {
        UnityEngine.Object.Destroy(Image);
        Image = null;
        CP = null;
    }
}
