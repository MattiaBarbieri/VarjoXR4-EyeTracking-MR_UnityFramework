using UnityEngine;
using Varjo.XR;

public class MixedReality : MonoBehaviour
{
    public bool isPassThroughActive = false;

    public void TogglePassThrough()
    {
        if (isPassThroughActive)
        {
            VarjoMixedReality.StopRender();
            VarjoRendering.SetOpaque(true);
            Debug.Log("Pass-through disattivato.");
        }
        else
        {
            VarjoRendering.SetOpaque(false);
            bool success = VarjoMixedReality.StartRender();
            Debug.Log("Pass-through attivato: " + success);
            isPassThroughActive = success;
            return;
        }

        isPassThroughActive = !isPassThroughActive;
    }

    public bool IsActive()
    {
        return isPassThroughActive;
    }
}
