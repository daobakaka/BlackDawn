using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Griphic : MonoBehaviour
{
    // Start is called before the first frame update
    public bool enableSRP = true;



    private void OnEnable()
    {
        Debug.Log(" OnEnable ����  SRP: " + enableSRP);
    }

    private void Awake()
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = enableSRP;
        Debug.Log("����SRP: " + GraphicsSettings.useScriptableRenderPipelineBatching);
    }
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
