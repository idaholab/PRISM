using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ifPasswordScript : MonoBehaviour
{
	public btnLoginScript script;
	// Update is called once per frame
	public void OnEndEdit( string value )
	{
		if (Input.GetKeyDown (KeyCode.KeypadEnter) || Input.GetKeyDown (KeyCode.Return))
			script.onClick ();
	}
}
