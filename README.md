<p align="center">
  <img width="700" align="center" alt="uub_01" src="https://user-images.githubusercontent.com/42884387/216822248-f43c7da4-a94e-4540-8b77-1151e6321b99.png">
</p>


## Unified Universal Blur - URP Blur effect for Unity

Unified Universal Blur allows you to display blurred version of the screen, usually for translucent UI effects.

Currently intended use cases include (other scenarios may not work):
- UI image component with blur material, displaying blurred 3D world (Canvas set to 'Screen Space - Overlay').

Features:
- Kawase blur
- Blurs both opaque and transparent objects (make sure correct setting is selected)
- Blurs Post-Processing and any other image effect which is rendered before blur (based on render feature order)

Tested and working for unity versions:
- 2022.2
- 2021.3
- 2020.3


### Installation

This repository works with upm. 
<br>Simply add it via package manager (get the link from <>Code button in top right corner or view releases).
<br>To specify any version, add #version number like this: giturl#1.0.0 
<br>(more information using upm at: https://docs.unity3d.com/Manual/upm-git.html)

Manual: It is also possible to download zip and put its content anywhere in the project.
<br>

### Setup

- Add "Universal Blur Feature" renderer feature in every renderer data that is being used by project.
<br>(more information about using universal renderer: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@15.0/manual/urp-universal-renderer.html).
- Assign "KawaseBlurMat" material to passMaterial if not present already.
- (Optional) Play with settings.
- Assign BlurForUI material to any UI image component.
- Done.
