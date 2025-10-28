using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR;
using Varjo.XR;

public class FaceTracking : MonoBehaviour
{
    [Header("Head Tracking")]
    public GameObject XR_head;
    public Vector3 head_position;
    public Quaternion head_rotation;
    private InputDevice headDevice;

    [Header("Eye Tracking")]
    public GameObject XR_left_eye, XR_right_eye;
    public Vector3 gaze_origin_L, gaze_origin_R, gaze_origin_C;
    public Vector3 gaze_direct_L, gaze_direct_R, gaze_direct_C;
    public Vector3 gaze_origin_L_3Dworld, gaze_origin_R_3Dworld, gaze_origin_C_3Dworld;
    public Vector3 gaze_direct_L_3Dworld, gaze_direct_R_3Dworld, gaze_direct_C_3Dworld;
    public Vector3 gaze_contingency_L, gaze_contingency_R, gaze_contingency_C, fixationPoint;
    public bool gazeCalibrated = false;
    public float ipd_mm;
    public float left_pupil_mm, left_iris_mm, left_ratio;
    public float right_pupil_mm, right_iris_mm, right_ratio;
    public float focus_distance, focus_stability;
    public long capture_time_ns;
    public long frame_number;

    [Header("Visual Debug")]
    public GameObject greenSphere;

    [Header("Eye Tracking Raw Data Logging")]
    public bool start_printing = false;
    public string file_path = Directory.GetCurrentDirectory();
    public string file_name = "FaceTrackingData";
    public int file_number = 1;


    private static Queue<Vector3> gazeOriginLHistory = new Queue<Vector3>(); // code per memorizzare i dati storici delle posizioni e delle direzioni dello aguardo. Le code tengono traccia dei valori  raccolti nei frame precedenti
    private static Queue<Vector3> gazeOriginRHistory = new Queue<Vector3>();
    private static Queue<Vector3> gazeDirectLHistory = new Queue<Vector3>();
    private static Queue<Vector3> gazeDirectRHistory = new Queue<Vector3>();
    private static Queue<Vector3> gazeOriginCHistory = new Queue<Vector3>();
    private static Queue<Vector3> gazeDirectCHistory = new Queue<Vector3>();      // Code per filtrare anche il gaze combinato (centrale)
    private  static int filterSize = 3; // Dimensione del filtro di media mobile



    void Start()
    {
        ConfigureEyeTracking();
        StartCoroutine(GazePollingLoop()); // ← avvia la raccolta dati a 200 Hz
    }

    void Update()
    {
        SmoothGazeData();
    }

    void LateUpdate()
    {
       SetPosition();
    }

    private void SetPosition()
    {
        // Set Eyes and Head Position in 3D World
        if (XR_head != null) XR_head.transform.SetPositionAndRotation(head_position, head_rotation);
        if (XR_left_eye != null)XR_left_eye.transform.SetPositionAndRotation(gaze_origin_L_3Dworld, head_rotation);
        if (XR_right_eye != null)XR_right_eye.transform.SetPositionAndRotation(gaze_origin_R_3Dworld, head_rotation);

        if (greenSphere != null) greenSphere.transform.position = fixationPoint;
    }

    IEnumerator GazePollingLoop()
    {
        while (true)
        {
            EyeTracking(); // con foreach su gazeList
            yield return new WaitForSecondsRealtime(0.005f); // 200 Hz
        }
    }

    private void ConfigureEyeTracking()
    {
        // Set Eye Tracking at maximum performance
        VarjoEyeTracking.SetGazeOutputFrequency(VarjoEyeTracking.GazeOutputFrequency.MaximumSupported);
        VarjoEyeTracking.SetGazeOutputFilterType(VarjoEyeTracking.GazeOutputFilterType.Standard);

        // Check if Calibration is OK
        if (!VarjoEyeTracking.IsGazeCalibrated())
        {
            Debug.LogWarning("Calibrazione non completata.");
        }
        else
        {
            Debug.Log("Calibrazione OK.");
        }
    }

    public void TriggerGazeCalibration()
    {
        if (!gazeCalibrated && VarjoEyeTracking.IsGazeAllowed())
        {
            VarjoEyeTracking.RequestGazeCalibration(VarjoEyeTracking.GazeCalibrationMode.Fast);
            gazeCalibrated = true;
            Debug.Log("Calibrazione dello sguardo avviata manualmente.");
        }
    }

    
    // Low pass filter to reduce noise
    private void SmoothGazeData()
    {
        // Aggiungi i nuovi valori alle code
        gazeOriginLHistory.Enqueue(gaze_origin_L_3Dworld);
        gazeOriginRHistory.Enqueue(gaze_origin_R_3Dworld);
        gazeOriginCHistory.Enqueue(gaze_origin_C_3Dworld);

        gazeDirectLHistory.Enqueue(gaze_direct_L_3Dworld);
        gazeDirectRHistory.Enqueue(gaze_direct_R_3Dworld);
        gazeDirectCHistory.Enqueue(gaze_direct_C_3Dworld);

        // Rimuovi i valori più vecchi se la coda supera la dimensione del filtro
        if (gazeOriginLHistory.Count > filterSize)
        {
            gazeOriginLHistory.Dequeue();
            gazeOriginRHistory.Dequeue();
            gazeOriginCHistory.Dequeue();

            gazeDirectLHistory.Dequeue();
            gazeDirectRHistory.Dequeue();
            gazeDirectCHistory.Dequeue();
        }

        // Calcola la media dei valori nella coda
        gaze_origin_L_3Dworld = AverageVector3(gazeOriginLHistory);
        gaze_origin_R_3Dworld = AverageVector3(gazeOriginRHistory);
        gaze_origin_C_3Dworld = AverageVector3(gazeOriginCHistory);

        gaze_direct_L_3Dworld = AverageVector3(gazeDirectLHistory);
        gaze_direct_R_3Dworld = AverageVector3(gazeDirectRHistory);
        gaze_direct_C_3Dworld = AverageVector3(gazeDirectCHistory);
    }



        // Calcolo Media dei valori delle code per ottenere una posizione ed una direzione più stabile e meno rumorosa. 
        //Si sommano tutti i valori nella coda e si dividono per il numero degli elementi.


        private Vector3 AverageVector3(Queue<Vector3> queue)
        {
            Vector3 sum = Vector3.zero;
            foreach (var value in queue)
            {
                sum += value;
            }
            return sum / queue.Count;
        }

    private void EyeTracking()
    {

        // TRACKING HEAD MOVEMENTS FROM UNITY.ENGINE.XR 
        if (!headDevice.isValid)
        {
            headDevice = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (!headDevice.isValid) return;
        }

        if (headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
        {
            head_position = position;
        }

        if (headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
        {
            head_rotation = rotation;
        }


        // TRACKING EYES MOVEMENTS FROM VARJO SDK
        if (!VarjoEyeTracking.IsGazeAllowed()) return;

        List<VarjoEyeTracking.GazeData> gazeList;
        VarjoEyeTracking.GetGazeList(out gazeList);
        if (gazeList == null || gazeList.Count == 0) return;

        foreach (var gazeData in gazeList)
        {
            // Validità
            if (gazeData.leftStatus == VarjoEyeTracking.GazeEyeStatus.Invalid ||
                gazeData.rightStatus == VarjoEyeTracking.GazeEyeStatus.Invalid ||
                gazeData.status == VarjoEyeTracking.GazeStatus.Invalid)
            {
                continue; // salta frame non validi
            }

            // Raw Data about Gaze Origin and Gaze Direction
            gaze_origin_L = gazeData.left.origin;
            gaze_origin_R = gazeData.right.origin;
            gaze_origin_C = gazeData.gaze.origin;

            gaze_direct_L = gazeData.left.forward;
            gaze_direct_R = gazeData.right.forward;
            gaze_direct_C = gazeData.gaze.forward;

            // Biometric Measurment
            focus_distance = gazeData.focusDistance;
            focus_stability = gazeData.focusStability;
            capture_time_ns = gazeData.captureTime;
            frame_number = gazeData.frameNumber;

            VarjoEyeTracking.EyeMeasurements eyeMeasurements = VarjoEyeTracking.GetEyeMeasurements();
            ipd_mm = eyeMeasurements.interPupillaryDistanceInMM;
            left_pupil_mm = eyeMeasurements.leftPupilDiameterInMM;
            left_iris_mm = eyeMeasurements.leftIrisDiameterInMM;
            left_ratio = eyeMeasurements.leftPupilIrisDiameterRatio;
            right_pupil_mm = eyeMeasurements.rightPupilDiameterInMM;
            right_iris_mm = eyeMeasurements.rightIrisDiameterInMM;
            right_ratio = eyeMeasurements.rightPupilIrisDiameterRatio;


            // Convert local data (origin and direction) in 3D world coordinate
            gaze_origin_L_3Dworld = XR_head.transform.TransformPoint(gaze_origin_L);
            gaze_origin_R_3Dworld = XR_head.transform.TransformPoint(gaze_origin_R);
            gaze_origin_C_3Dworld = XR_head.transform.TransformPoint(gaze_origin_C);

            gaze_direct_L_3Dworld = XR_left_eye.transform.TransformDirection(gaze_direct_L);
            gaze_direct_R_3Dworld = XR_right_eye.transform.TransformDirection(gaze_direct_R);
            gaze_direct_C_3Dworld = XR_head.transform.TransformDirection(gaze_direct_C);

            //SmoothGazeData();

            // Develop Vectors for Gaze Contingency
            float defaultFocusDistance = 1.0f;
            gaze_contingency_L = gaze_origin_L_3Dworld + gaze_direct_L_3Dworld * defaultFocusDistance;
            gaze_contingency_R = gaze_origin_R_3Dworld + gaze_direct_R_3Dworld * defaultFocusDistance;
            gaze_contingency_C = gaze_origin_C_3Dworld + gaze_direct_C_3Dworld * defaultFocusDistance;
            fixationPoint = gaze_origin_C_3Dworld + gaze_direct_C_3Dworld * gazeData.focusDistance;


            // Saving Data
            if (start_printing == true)
            {
                // Validità
                bool validL = gazeData.leftStatus != VarjoEyeTracking.GazeEyeStatus.Invalid;
                bool validR = gazeData.rightStatus != VarjoEyeTracking.GazeEyeStatus.Invalid;
                bool validC = gazeData.status != VarjoEyeTracking.GazeStatus.Invalid;

                // Safe fallback
                Vector3 safe(Vector3 v, bool valid) => valid ? v : Vector3.one * float.NaN;
                float safeF(float f, bool valid) => valid ? f : float.NaN;

                string line =
                    Time.time.ToString("F4") + "\t" +
                    capture_time_ns.ToString() + "\t" +
                    frame_number.ToString() + "\t" +

                    ((int)gazeData.leftStatus).ToString() + "\t" +
                    ((int)gazeData.rightStatus).ToString() + "\t" +
                    ((int)gazeData.status).ToString() + "\t" +

                    safe(gaze_origin_L, validL).x.ToString("F5") + "\t" +
                    safe(gaze_origin_L, validL).y.ToString("F5") + "\t" +
                    safe(gaze_origin_L, validL).z.ToString("F5") + "\t" +

                    safe(gaze_origin_R, validR).x.ToString("F5") + "\t" +
                    safe(gaze_origin_R, validR).y.ToString("F5") + "\t" +
                    safe(gaze_origin_R, validR).z.ToString("F5") + "\t" +

                    safe(gaze_origin_C, validC).x.ToString("F5") + "\t" +
                    safe(gaze_origin_C, validC).y.ToString("F5") + "\t" +
                    safe(gaze_origin_C, validC).z.ToString("F5") + "\t" +

                    safe(gaze_direct_L, validL).x.ToString("F5") + "\t" +
                    safe(gaze_direct_L, validL).y.ToString("F5") + "\t" +
                    safe(gaze_direct_L, validL).z.ToString("F5") + "\t" +

                    safe(gaze_direct_R, validR).x.ToString("F5") + "\t" +
                    safe(gaze_direct_R, validR).y.ToString("F5") + "\t" +
                    safe(gaze_direct_R, validR).z.ToString("F5") + "\t" +

                    safe(gaze_direct_C, validC).x.ToString("F5") + "\t" +
                    safe(gaze_direct_C, validC).y.ToString("F5") + "\t" +
                    safe(gaze_direct_C, validC).z.ToString("F5") + "\t" +

                    safeF(focus_distance, validC).ToString("F5") + "\t" +
                    focus_stability.ToString("F5") + "\t" +
                    safeF(ipd_mm, validC).ToString("F5") + "\t" +

                    safeF(left_pupil_mm, validL).ToString("F5") + "\t" +
                    safeF(left_iris_mm, validL).ToString("F5") + "\t" +
                    safeF(left_ratio, validL).ToString("F5") + "\t" +

                    safeF(right_pupil_mm, validR).ToString("F5") + "\t" +
                    safeF(right_iris_mm, validR).ToString("F5") + "\t" +
                    safeF(right_ratio, validR).ToString("F5") + "\t" +

                    safe(fixationPoint, validC).x.ToString("F5") + "\t" +
                    safe(fixationPoint, validC).y.ToString("F5") + "\t" +
                    safe(fixationPoint, validC).z.ToString("F5") + "\t" +

                    safe(gaze_contingency_L, validL).x.ToString("F5") + "\t" +
                    safe(gaze_contingency_L, validL).y.ToString("F5") + "\t" +
                    safe(gaze_contingency_L, validL).z.ToString("F5") + "\t" +

                    safe(gaze_contingency_R, validR).x.ToString("F5") + "\t" +
                    safe(gaze_contingency_R, validR).y.ToString("F5") + "\t" +
                    safe(gaze_contingency_R, validR).z.ToString("F5") + "\t" +

                    safe(gaze_contingency_C, validC).x.ToString("F5") + "\t" +
                    safe(gaze_contingency_C, validC).y.ToString("F5") + "\t" +
                    safe(gaze_contingency_C, validC).z.ToString("F5") +
                    Environment.NewLine;

                File.AppendAllText(file_name + file_number + ".txt", line);
            }

        }
    }


    private void WriteHeader(string filePath)
    {
        //if (headerWritten) return;

        string header =
        "unity_time\tcapture_time_ns\tframe_number\t" +
        "gaze_validity_L\tgaze_validity_R\tgaze_validity_C\t" +
        "gaze_origin_L.x\tgaze_origin_L.y\tgaze_origin_L.z\t" +
        "gaze_origin_R.x\tgaze_origin_R.y\tgaze_origin_R.z\t" +
        "gaze_origin_C.x\tgaze_origin_C.y\tgaze_origin_C.z\t" +
        "gaze_direct_L.x\tgaze_direct_L.y\tgaze_direct_L.z\t" +
        "gaze_direct_R.x\tgaze_direct_R.y\tgaze_direct_R.z\t" +
        "gaze_direct_C.x\tgaze_direct_C.y\tgaze_direct_C.z\t" +
        "focus_distance\tfocus_stability\tIPD_mm\t" +
        "left_pupil_mm\tleft_iris_mm\tleft_ratio\t" +
        "right_pupil_mm\tright_iris_mm\tright_ratio\t" +
        "fixationPoint.x\tfixationPoint.y\tfixationPoint.z\t" +
        "gaze_contingency_L.x\tgaze_contingency_L.y\tgaze_contingency_L.z\t" +
        "gaze_contingency_R.x\tgaze_contingency_R.y\tgaze_contingency_R.z\t" +
        "gaze_contingency_C.x\tgaze_contingency_C.y\tgaze_contingency_C.z" +
        Environment.NewLine;


        File.AppendAllText(file_name + file_number + ".txt", header);
        //headerWritten = true;
    }



    public void StartDataRecord()
    {
        Debug.Log("START PRINT");
        start_printing = true;
        file_number += 1;
        WriteHeader(file_path); 
    }

    public void StopDataRecord()
    {
        Debug.Log("STOP PRINT");
        start_printing = false;
    }
}
