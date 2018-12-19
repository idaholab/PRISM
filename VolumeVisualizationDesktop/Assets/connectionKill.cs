using UnityEngine;
using System.Collections;

public class connectionKill : MonoBehaviour {

    public SievasController sievasController;

    // Use this for initialization
    void Awake() {
       // UnityEditor.EditorApplication.playmodeStateChanged += terminateConnection;
    }

    // Update is called once per frame
    void Update() {

    }
    void OnDisable()
    {
        Debug.Log("Application ending after " + Time.time + " seconds");
    }
    void terminateConnection()
    {
        Debug.Log("TERMINATING APP");
        if (sievasController.connection != null)
        {
            StopAllCoroutines();
            sievasController.connection.Stop();
            sievasController.connection.Close();
            sievasController.connection.Dispose();
        }
    }

}


