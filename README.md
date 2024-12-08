# This repository contains the supplementary material for the GazeMolVR project

## Research Paper
Rajkumar Darbar, Hubert Santuz, Antoine Taly, and Marc Baaden. 2024. **GazeMolVR: Sharing Eye-Gaze Cues in a Collaborative VR Environment for Molecular Visualization.** In *International Conference on Mobile and Ubiquitous Multimedia (MUM '24)*, December 01–04, 2024, Stockholm, Sweden. ACM, New York, NY, USA, 17 pages. [https://doi.org/10.1145/3701571.3701599](https://doi.org/10.1145/3701571.3701599)

## Demo Videos
All videos are available [here](https://www.youtube.com/playlist?list=PLfK4VqkC4I484LIevYw7FwlmeQUoW2XHY).


## Prerequisite 
- Our GazeMolVR system is built on [UnityMol](https://github.com/LBT-CNRS/UnityMol-Releases), a Unity3D-based biomolecule visualization tool. Visit [this website](https://unity.mol3d.tech/) for more details about UnityMol.
- Unity 2019.4.40f1 and Steam VR 2.6.2
- [Unity Photon PUN 2 library](https://assetstore.unity.com/packages/tools/network/pun-2-free-119922) is used for multiplayer setup.
- Two HTC VIVE Pro Eye headsets
- Set up your headset following the [VIVE Pro Eye Setup guide](https://business.vive.com/eu/setup/vive-pro/).
- [SRanipal SDK (version 1.3.6.8)](https://developer-express.vive.com/resources/vive-sense/eye-and-facial-tracking-sdk/) to capture eye-tracking data from the headset.
- SR Runtime 1.3.6.12 
- Perform [eye tracking calibration](https://www.vive.com/ca/support/vive-pro-eye/category_howto/calibrating-eye-tracking.html). [HTC VIVE Pro Eye Development Guide](https://developer.tobii.com/xr/develop/xr-sdk/getting-started/vive-pro-eye/#step-3-download-and-import-the-vive-sranipal-sdk) might be helpful here.



## How do I play the pre-recorded tutorial as described in User Study-I in the paper?

First, ensure that your HTC Vive Pro Eye headset is connected to your desktop, and complete the room setup and eye calibration. Then, follow these steps sequentially:  

1. **In Unity:**
   - Go to **'Assets -> Scenes -> UnityMolEyeGaze - US01'** to load and play the scene.   
   - Select the **'Record-Replay Manager'** game-object as shown in [this image](./unity-screenshots/user-study-I-screenshots/user-study-1-setup-desktop.png).
   - In the Inspector panel:
     - Set the **Participant Name** (e.g., P1).
     - Set the **Current Eye Gaze** from the drop-down list (e.g., GazeTrail).
     - Set the **Current PDB** from the drop-down list (e.g., Cartoon_PvdRTOpmQ).

2. **In the Virtual Environment:**
   - Put on your HTC VIVE Pro Eye headset.
   - You will see two menu panels in the VR scene: the **Multiplayer Menu** and the **Eye-Gaze Menu** as shown [here](./unity-screenshots/user-study-I-screenshots/user-study-1-setup-vr-view.png).
     - To interact with these menu panels, point the controller ray at them and press the controller trackpad button to make a selection.
     - Select the **Player 02** button from the **Multiplayer Menu** panel. Don't click the **Player 01** button.
     - Select **PvdRT** from the drop-down protein list under **Cartoon** in the **Multiplayer Menu**. This action will load the protein into the scene. Make sure you choose the same protein that you selected earlier in the **Current PDB** drop-down list under **'In Unity'**.
     - Similarly, select the **GazeTrail** button from the **Eye-Gaze Menu** to see your gaze visualization in blue on the protein structure. Don't forget to choose the same eye-gaze visualization that you selected earlier in the **Current Eye Gaze** drop-down list under **'In Unity'**. 
     - Finally, click the **Replay** button to watch the pre-recorded tutorial. The instructor avatar will appear in the scene and describe the loaded protein, with the instructor's eye-gaze shown in red. 



## How do I start a real-time collaborative session between a pair as described in User Study-II?

To make the setup process easier to understand, we'll refer to the two users in a pair as Player 01 and Player 02. On each user's side, the HTC VIVE Pro Eye headset is connected to a desktop. After completing the room setup and eye calibration, follow these steps sequentially:

1. Go to **'Assets -> Scenes -> UnityMolEyeGaze - US02.2'** in Unity on both players' computers to load and play the scene.
2. Both players will see [this menu panel](./unity-screenshots/user-study-II-screenshots/user-study-II-vr-menu.png) in their VR view. First, Player 01 will click the **Player 01** button on the **Multiplayer Menu** panel. Then, Player 02 will select the **Player 02** button. This sequence of actions will designate Player 01 as the master player, and both players' avatars will appear in the scene [like this](./unity-screenshots/user-study-II-screenshots/user-study-II-both-avatar.png).
3. In Unity on Player 01's side, select the **'ExperimentManager'** game object as shown [here](./unity-screenshots/user-study-II-screenshots/user-study-II-experiment-manager.png). In the Inspector panel:
   - Set the **Participant Name** (e.g., P10).
   - Set the **Exp Type** from the drop-down list (e.g., Controller_Spotlight).
   - Set the **Protein Representation Type** from the drop-down list (e.g., Surface).
   - Set the **Current PDB** from the drop-down list (e.g., Jonathan_6b3r).
4. In VR, Player 01 will click the **Load PDB** button on the **Multiplayer Menu** panel, making the protein visible to both players in the scene, as shown [here](./unity-screenshots/user-study-II-screenshots/user-study-II-both-avatar-protein-surface.png). By default, Player 01, as the master player, can manipulate the protein (e.g., changing its position, rotation, and zoom). Control can switch between Player 01 and Player 02 using the **Ownership Transfer** button, allowing either player to take control at any time.
5. Either player can select the **GazeSpotlight** button from the **Eye-Gaze Menu** to view their bi-directional eye-gaze visualization on the protein structure. Player 01's gaze will be shown in red, while Player 02's gaze will be in blue.
6. That's it—everything is ready to start a collaborative discussion session about the protein structure and its functions.

## Contributors
For inquiries or feedback, feel free to contact the contributors:
- **Rajkumar Darbar**  
  **Email**: rajdarbar.r@gmail.com  
  **Website**: [https://rajkdarbar.github.io/](https://rajkdarbar.github.io/)

- **Marc Baaden**  
  **Email**: baaden@smplinux.de  
  **Website**: [https://www.baaden.ibpc.fr/](https://www.baaden.ibpc.fr/)
  
