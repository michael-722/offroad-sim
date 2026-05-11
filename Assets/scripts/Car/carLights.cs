using UnityEngine;

public class carLights : MonoBehaviour
{
    [SerializeField] private GameObject lightsRoot;

    public void enableLights() => lightsRoot.SetActive(true);
    public void disableLights() => lightsRoot.SetActive(false);
    
}