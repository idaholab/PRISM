/* User Interface | Marko Sterbentz 6/6/2017
 * This script provides functionality for updating the shader with general values provided by the user.
 */ 

using UnityEngine;
using UnityEngine.UI;

public class GeneralControlsHandler : MonoBehaviour {

	private VolumeController volumeController;      // The main controller used to synchronize data input, user input, and visualization

	// General controls slider text
	public Text maxStepsValueText;
    public Text normPerRayValueText;
    public Text hzRenderLevelValueText;

    // Use this for initialization
    void Start () {
		// Set up the reference to the VolumeController
		volumeController = (VolumeController)GameObject.Find("VolumeController").GetComponent(typeof(VolumeController));

		// Initialize the user interface text fields
		maxStepsValueText.text = GameObject.Find("Max Steps Slider").GetComponent<Slider>().value.ToString();
        normPerRayValueText.text = GameObject.Find("Norm Per Ray Slider").GetComponent<Slider>().value.ToString();
        hzRenderLevelValueText.text = GameObject.Find("HZ Render Level Slider").GetComponent<Slider>().value.ToString();
	}
	
	// Update is called once per frame
	void Update () {

    }

    /* General Slider Update Functions */
    public void updateStepsValue(float newVal)
    {
		volumeController.updateMaterialPropFloatAll("_Steps", newVal);
        maxStepsValueText.text = newVal.ToString();
    }

    public void updateNormPerRay(float newVal)
    {
		volumeController.updateMaterialPropFloatAll("_NormPerRay", newVal);
        normPerRayValueText.text = newVal.ToString("0.00");
    }

    public void updateHZRenderLevel(float newVal)
    {
		volumeController.updateMaterialPropIntAll("_HZRenderLevel", (int) newVal);
        hzRenderLevelValueText.text = newVal.ToString();
    }
}
