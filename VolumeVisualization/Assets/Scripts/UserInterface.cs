using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserInterface : MonoBehaviour {

    public GameObject volume;
    public Text maxStepsValueText;
    public Text normPerStepValueText;
    public Text normPerRayValueText;
    public Text hzRenderLevelValueText;

    // Use this for initialization
    void Start () {
        // Initialize the user interface text fields
        maxStepsValueText.text = GameObject.Find("Max Steps Slider").GetComponent<Slider>().value.ToString();
        normPerStepValueText.text = GameObject.Find("Norm Per Step Slider").GetComponent<Slider>().value.ToString();
        normPerRayValueText.text = GameObject.Find("Norm Per Ray Slider").GetComponent<Slider>().value.ToString();
        hzRenderLevelValueText.text = GameObject.Find("HZ Render Level Slider").GetComponent<Slider>().value.ToString();
    }
	
	// Update is called once per frame
	void Update () {

    }


    /* Slider Update Functions */
    public void updateStepsValue(float newVal)
    {
        volume.GetComponent<Renderer>().material.SetFloat("_Steps", newVal);
        maxStepsValueText.text = newVal.ToString();
    }

    public void updateNormPerStep(float newVal)
    {
        volume.GetComponent<Renderer>().material.SetFloat("_NormPerStep", newVal);
        normPerStepValueText.text = newVal.ToString();
    }

    public void updateNormPerRay(float newVal)
    {
        volume.GetComponent<Renderer>().material.SetFloat("_NormPerRay", newVal);
        normPerRayValueText.text = newVal.ToString();
    }

    public void updateHZRenderLevel(float newVal)
    {
        volume.GetComponent<Renderer>().material.SetInt("_HZRenderLevel", (int) newVal);
        hzRenderLevelValueText.text = newVal.ToString();
    }
}
