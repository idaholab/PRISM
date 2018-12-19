using UnityEngine;
using System.Collections;

public class quit : MonoBehaviour {
    public SievasController sievasController;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
    void OnApplicationQuit()
    {
        if(sievasController.connection != null) { 
            sievasController.connection.Stop();
            sievasController.connection.Close();
            sievasController.connection.Dispose();
        }
        Debug.Log("quiting");
    }
}
