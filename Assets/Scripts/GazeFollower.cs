using UnityEngine;

public class GazeFollower : MonoBehaviour
{
    public enum GazeSource
    {
        Left,
        Right,
        Center
    }

    [Header("Reference to FaceTracking")]
    public FaceTracking faceTracking;

    [Header("Sorgente da seguire")]
    public GazeSource gazeSource;

    void Update()
    {
        if (faceTracking == null) return;

        Vector3 target = Vector3.zero;

        switch (gazeSource)
        {
            case GazeSource.Left:
                target = faceTracking.gaze_contingency_L;
                break;
            case GazeSource.Right:
                target = faceTracking.gaze_contingency_R;
                break;
            case GazeSource.Center:
                target = faceTracking.gaze_contingency_C;
                break;
        }

        transform.SetPositionAndRotation(target, faceTracking.XR_head.transform.rotation);
    }
}
