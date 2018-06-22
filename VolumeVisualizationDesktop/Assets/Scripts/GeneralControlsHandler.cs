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
		volumeController.getCurrentVolume().updateMaterialPropFloatAll("_Steps", newVal);
        maxStepsValueText.text = newVal.ToString();
    }

	/// <summary>
	/// Norm per ray slider update function.
	/// </summary>
	/// <param name="newVal"></param>
	public void updateNormPerRay(float newVal)
    {
		volumeController.getCurrentVolume().updateMaterialPropFloatAll("_NormPerRay", newVal);
        normPerRayValueText.text = newVal.ToString("0.00");
    }

	/// <summary>
	/// HZ-order render level slider update function.
	/// </summary>
	/// <param name="newVal"></param>
	public void updateHZRenderLevel(float newVal)
    {
		volumeController.getCurrentVolume().updateMaterialPropIntAll("_HZRenderLevel", (int) newVal);
        hzRenderLevelValueText.text = newVal.ToString();
    }
}
