using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransferFunction : MonoBehaviour {



	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {

	}
}

public class TransferControlPoint
{
    private Color color;
    private int isovalue;

    public TransferControlPoint(float r, float g, float b, int isovalue)
    {
        this.color.r = r;
        this.color.g = g;
        this.color.b = b;
        this.color.a = 1.0f;
        this.isovalue = isovalue;
    }

    public TransferControlPoint(float alpha, int isovalue)
    {
        this.color.r = 0.0f;
        this.color.g = 0.0f;
        this.color.b = 0.0f;
        this.color.a = alpha;
        this.isovalue = isovalue;
    }

}
