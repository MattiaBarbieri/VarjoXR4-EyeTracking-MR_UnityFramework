using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Reference to FaceTracking")]
    public FaceTracking faceTracking;
    public MixedReality mixedReality;

    void Update()
    {
        if (faceTracking == null)
        {
            Debug.LogWarning("FaceTracking non assegnato nel SystemManager.");
            return;
        }

        // 
        if (Input.GetKeyDown(KeyCode.C))
        {
            faceTracking.TriggerGazeCalibration();
        }

        // 
        if (Input.GetKeyDown(KeyCode.S))
        {
            faceTracking.StartDataRecord();
        }

        //  
        if (Input.GetKeyDown(KeyCode.E))
        {
            faceTracking.StopDataRecord();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
           mixedReality?.TogglePassThrough();
        }
    }
}
