using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class PerspectiveSwitcher : MonoBehaviour
{
    public GameObject View2D;
    public FirstPersonController controller;
    public StarterAssetsInputs inputs;
    bool is2D = false;

    void Switch() {
        if (is2D) {
            View2D.SetActive(false);
            controller.enabled = true;
            inputs.enabled = true;
        } else {
            View2D.SetActive(true);
            controller.enabled = false;
            inputs.enabled = false;
        }
        is2D = !is2D;
    }

    public void OnSwitch(InputValue value) {
        Switch();
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Monitor for the "Switch" key (which using new Unity Input Package called "Switch")
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
