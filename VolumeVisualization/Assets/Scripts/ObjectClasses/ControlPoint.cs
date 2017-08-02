using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * Control Point | Marko Sterbentz 6/8/2017
 * Specifies the color and alpha value at a specific isovalue in the transfer function.
 * Note: Can be either a color or alpha control point depending on how they're used.
 */
[Serializable]
public class ControlPoint
{
	/* Member variables */
	public Color color;
	public int isovalue;

	/* Constructors */
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

	/* Functions */
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