﻿
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


