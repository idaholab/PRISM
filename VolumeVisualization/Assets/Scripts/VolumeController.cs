using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeController : MonoBehaviour {

	private Brick[] bricks;
	//private TransferFunction transferFunction;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void loadVolume()
	{

	}

	private void readMetadata()
	{

	}

	public Brick generateBrick(string file, Vector3 position)
	{
		return new Brick();
	}
}

public class Brick
{
	private int hzRenderLevel;
	private Material material;
	private string file;
	private Vector3 position;

	public Brick()
	{

	}
}
