/* 
 * Copyright 2019 Idaho National Laboratory.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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
