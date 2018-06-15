/*****************************************************************
* FPSDisplay | Landon Woolley | September 13th 2017
* http://wiki.unity3d.com/index.php?title=FramesPerSecond
* Displays the current frames per second more accuratley
* than the biult in frames per second counter in unity
******************************************************************/ 

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FPSDisplay : MonoBehaviour
{
    private float   frequency   = 0.5F; // The update frequency of the fps
    private int     nbDecimal   = 1;    // How many decimal do you want to display
    private float   accum       = 0f;   // FPS accumulated over the interval
    private int     frames      = 0;    // Frames drawn over the interval
    private string  sFPS        = "";   // The fps formatted into a string.
    private Text    instruction;        // Set the UI text to sFPS

    // Need Coroutine when using IEnumerator and Time
    void Start() {
        instruction = GetComponent<Text>();
        StartCoroutine(FPS());
    }

    // Updates each frame count 
    void Update() {
        accum += Time.timeScale / Time.deltaTime;
        ++frames;
        instruction.text = sFPS;
    }

    // Caclulates the frames per second
    IEnumerator FPS() {
        while (true) {
            float fps = accum / frames;
            sFPS = fps.ToString("f" + Mathf.Clamp(nbDecimal, 0, 10));
            instruction.color = (fps >= 30) ? Color.green : ((fps > 10) ? Color.yellow : Color.red);
            accum = 0.0F;
            frames = 0;
            yield return new WaitForSeconds(frequency);
        }
    }
}