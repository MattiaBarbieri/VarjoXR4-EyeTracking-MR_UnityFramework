# VarjoXR4-EyeTracking-MR_UnityFramework
Unity framework for real-time eye tracking with Varjo XR-4 in Mixed Reality. Includes gaze-contingent object control, fixation point tracking, and data logging for ophthalmic and XR research.tion studies.


## 🧠 Purpose

- Provide a robust starting point for experimental studies involving eye tracking in immersive environments.
- Enable real-time visualization and logging of eye movement data.
- Offer tools for gaze-contingent object manipulation in 3D space.

## 🛠 Requirements

- **Unity**: 2022.3.32f1
- **Render Pipeline**: Built-in Render Pipeline
- **SDK**: Varjo XR SDK (latest version)
- **Hardware**: Varjo XR-4 headset

## 🎮 Key Features

- Real-time tracking of head and eye movements (left, right, and combined gaze).
- Calculation of **focus distance** and **fixation point**.
- Three spheres in the scene follow different gaze-contingent vectors:
  - `Left`: left eye
  - `Right`: right eye
  - `Center`: combined gaze
- **Keyboard controls**:
  - `C`: trigger gaze calibration
  - `S`: start eye data recording
  - `E`: stop eye data recording
  - `Space`: toggle Mixed Reality mode (recommended)

## 👁 Gaze Contingency

To make any object in the scene gaze-contingent:

1. Attach the `GazeFollower.cs` script to the GameObject.
2. In the Inspector, select the gaze source: `Left`, `Right`, or `Center`.

## 🎯 Fixation Point and Depth

The system computes a **fixation point** based on the combined gaze vector and updates the **focus distance** in real time. This can be used to simulate visual attention or dynamic depth-of-field effects.

## 🧩 Scene Structure

- `XR_Head`: follows head movement.
- `XR_Left_Eye` and `XR_Right_Eye`: updated based on raw eye origin, translated relative to `XR_Head`.
- Gaze-contingent objects: follow the selected gaze vector in real time.

## 📁 Output

Eye tracking data is saved in `.txt` files with full headers, suitable for post-processing and analysis.
