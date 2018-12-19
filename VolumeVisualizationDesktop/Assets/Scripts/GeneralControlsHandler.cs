/* General Controls Handler | Marko Sterbentz 6/6/2017 | Randall Reese 08/29/2018 */

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This script provides functionality for updating the shader with general values provided by the user.
/// </summary>
public class GeneralControlsHandler : MonoBehaviour {

	private VolumeController volumeController;      // The main controller used to synchronize data input, user input, and visualization

	// General controls slider text
	public Text maxStepsValueText;
    public Text normPerRayValueText;
    public Text hzRenderLevelValueText;
    public Text lambdaValueText;

    /// <summary>
	/// Initialization function for the GeneralControlsHandler.
	/// </summary>
    void Start () {
		// Set up the reference to the VolumeController
		volumeController = (VolumeController)GameObject.Find("Main Camera").GetComponent(typeof(VolumeController));

        GameObject.Find("HZ Render Level Slider").GetComponent<Slider>().minValue = 0;
        GameObject.Find("HZ Render Level Slider").GetComponent<Slider>().maxValue = volumeController.CurrentVolume.MaxZLevel;//Also can be found by using the log_2 function. 
       

        // Initialize the user interface text fields
        maxStepsValueText.text = GameObject.Find("Max Steps Slider").GetComponent<Slider>().value.ToString();
        normPerRayValueText.text = GameObject.Find("Norm Per Ray Slider").GetComponent<Slider>().value.ToString();
        hzRenderLevelValueText.text = GameObject.Find("HZ Render Level Slider").GetComponent<Slider>().value.ToString();
        lambdaValueText.text = (GameObject.Find("Lambda Slider").GetComponent<Slider>().value/100).ToString();
        //We divide by 100 here because the slider is set up on a scale from 0 to 100, incrementing by whole numbers. 
        //Unity only allows sliders to increment by 0.1 using the arrows. I felt it would be better to increment by 0.01 using the arrows keys. Hence the slightly awkward work-around. 

    }
	

	/// <summary>
	/// Steps slider update function.
	/// </summary>
	/// <param name="newVal"></param>
    public void updateStepsValue(float newVal)
    {
		volumeController.RenderingComputeShader.SetInt("_Steps", (int) newVal);
        maxStepsValueText.text = newVal.ToString();
    }

	/// <summary>
	/// Norm per ray slider update function.
	/// </summary>
	/// <param name="newVal"></param>
	public void updateNormPerRay(float newVal)
    {
		
		volumeController.RenderingComputeShader.SetFloat("_NormPerRay", newVal);
        normPerRayValueText.text = newVal.ToString("0.00");
    }

	/// <summary>
	/// HZ-order render level slider update function.
	/// </summary>
	/// <param name="newVal"></param>
	public void updateHZRenderLevel(float newVal)
    {
    
        volumeController.updateMetaBrickBuffer((int)newVal);

        hzRenderLevelValueText.text = newVal.ToString(); 


    }

    /// <summary>
	/// Lambda slider update function.
	/// </summary>
	/// <param name="newVal"></param>
	public void updateLambda(float newVal)
    {
        
        volumeController.RenderingComputeShader.SetFloat("_Lambda", newVal/100);
        lambdaValueText.text = (newVal/100).ToString("0.00");
    }

}
