Volume Visualization Tool (PRISM)

Copyright 2019 Idaho National Laboratory.

Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.


This document is to provide documentation as to how to use PRISM. 

## Scope and Software Versions

* Users will need the latest versions of SIEVAS and PRISM. 
* Unity 2018.2.1f1 (64 bit) was used to develop the latest version of PRISM. 
* The Frames Per Second prefab may or may not work on older versions of Unity.  
* VisualStudio 2017 was used for the C#/HLSL development.
* Netbeans 8.2 and Java 8 were used for the Java development. 
* All development was done on Windows 10.
* Git Bash is the prefered console of use. 


## Starting

Begin by opening Unity and open the VolumeVisualizerDesktop project. In the Game view, you should see the SIEVAS login screen. Each field of the SIEVAS login screen should be auto populated. Depending on your usage, you may need to change these fields. To change these fields in Unity (and store them for further re-use), change

cvsLogin ->
          Panel ->
                 ifUsername -> (Or ifPassword or ifServerURL)
                 [Inspector]-> 
                             Input Field (Script) ->
                                                   Text



The default credentials are Username: `user` and Password: `password`. 
The name of the volume to render can be changed under `Main Camera -> Volume Controller (Script) -> Volume Name`. This field is not case sensitive. The `Data Path` field can also be altered as necessary.  

Now start the backend to SIEVAS using the following Maven make command (It is assumed that SIEVAS has already been installed and set up properly):

[@path ~SIEVAS/backend] $ mvn spring-boot:run


If all of your dependencies are compliant with the SIEVAS backend POM file, then you should see a line in the console that states "HELLO HANDLER" followed by about 20 lines of INFO. The last line of info should be something along the lines of
```
<timestamp>  INFO 10956 --- [  restartedMain] gov.inl.SIEVAS.Application: Started Application in 16.22 seconds (JVM running for 17.7)
```

If the make command fails due to an inability to create beans with the bean maker, the dependencies for SIEVAS and its backend may be out of date. 

## Volume Visualizer Tool Controls


Assuming that the make command has executed successfully, you can now press "play" in Unity. SIEVAS should be set to auto login. The volume should now be rendered. Note that some volumes are surrounded by padding that obstructs the view of the volume until the transfer function is appropriately manipulated. (See PiggyBank for an example). 

#### Volume manipulation
The volume itself is manipulated by clicking and pulling/pushing intuitively with the mouse. To zoom in/out, use the scroll wheel on the mouse. Most movement of the volume should be pretty instinctive if you know how to use a computer mouse. 

#### 'Volume Visualization' Panel Controls

There are four sliders on this panel. 

* Max Steps: Defaults to 128. This controls the maximun number of steps that each ray will make into the volume. Once this number is reached by a ray, the ray trace terminates, even if the aggregated alpha value for the associated pixel is less than one.  

* Norm Per Ray: Defaults to one (1). This controls the intensity scaling of the RGBa values associated with each ray. Larger values lead to more color saturation.

* Min HZ Level: Defaults to one (1). This controls the minimum HZ level that a voxel may be rendered at. This value will often be overridden by the alpha-based LOD culling that occurs.  

* Lambda: Defaults to 0.5. This controls the lambda value for the LOD culling detailed in VolumeVizTool.pdf. 


#### 'Transfer Function Menu' Controls
There are several components to this panel.

* Color: Defaults to black transitioning to white. This color spectrum bar is for use as a reference for the tranfer function control points. Users can add more colors to the spectrum by clicking on the color spectrum bar and using the RGB sliders on the color palette control panel to set and adjust the colors. To alter an existing color bar control point, click on the control point itself.

* Alpha-Isovalue 2-way Control: Defaults (under most transfer functions) to a diagonal line of slope one. Click and drag control points to adjust the emphasis of certain isovalues. 

* Transfer Function Selector: Defaults to `Black_To_White`. Select the transfer function you wish to use and load it. Take note that the save button will overwrite the current transfer function file with the current values of the color spectrum bar and the transfer function controls. If you do not wish to overwrite the predefined transfer functions with new values, first create a separate transfer function file that can be safely written over. 


## Author and Contact
* *Author:* Randall Reese
* *Email:* randall.reese@inl.gov
* *PRISM Developers:* Randall Reese, James Money, Marko Sterbentz, Nathan Morrical, Landon Woolley, Thomas Szewczyk. 
