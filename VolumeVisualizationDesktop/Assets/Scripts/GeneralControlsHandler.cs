/* General Controls Handler | Marko Sterbentz 6/6/2017 */

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

    /// <summary>
	/// Initialization function for the GeneralControlsHandler.
	/// </summary>
    void Start () {
		// Set up the reference to the VolumeController
		//volumeController = (VolumeController)GameObject.Find("VolumeController").GetComponent(typeof(VolumeController));
		volumeController = (VolumeController)GameObject.Find("Main Camera").GetComponent(typeof(VolumeController));

		// Initialize the user interface text fields
		maxStepsValueText.text = GameObject.Find("Max Steps Slider").GetComponent<Slider>().value.ToString();
        normPerRayValueText.text = GameObject.Find("Norm Per Ray Slider").GetComponent<Slider>().value.ToString();
        hzRenderLevelValueText.text = GameObject.Find("HZ Render Level Slider").GetComponent<Slider>().value.ToString();
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
		//volumeController.CurrentVolume.updateMaterialPropFloatAll("_NormPerRay", newVal);
		volumeController.RenderingComputeShader.SetFloat("_NormPerRay", newVal);
        normPerRayValueText.text = newVal.ToString("0.00");
    }

	/// <summary>
	/// HZ-order render level slider update function.
	/// </summary>
	/// <param name="newVal"></param>
	public void updateHZRenderLevel(float newVal)
    {
        MetaBrick brickToUpdate;
        for(int i= 0; i < 4; i++)
        {
            //volumeController.RenderingComputeShader.SetInt("_MetaBrickBuffer[i].currentZLevel", (int)newVal);

            //volumeController.CurrentVolume.Bricks[i].CurrentZLevel = (int) newVal; 
            volumeController.CurrentVolume.Bricks[i].CurrentZLevel =  (int)newVal;
            brickToUpdate = volumeController.CurrentVolume.Bricks[i].getMetaBrick();
            brickToUpdate.currentZLevel = (int)newVal;

            Debug.Log("This is the Z-level of metaBrick number " + i + ": " + brickToUpdate.currentZLevel); 

            Debug.Log("This is the value of brick " + i + ": " + volumeController.CurrentVolume.Bricks[i].CurrentZLevel); 
           // Debug.Log("Trying to set brick number " + i + " to the value " + newVal); 
            hzRenderLevelValueText.text = newVal.ToString();
            //We need to find a way to make these bricks push their info to the associated meta brick. 
        }
		//volumeController.CurrentVolume.updateMaterialPropIntAll("_HZRenderLevel", (int) newVal);
        
    }
}
