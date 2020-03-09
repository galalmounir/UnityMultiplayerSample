using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CanvasManager : MonoBehaviour
{
    public Toggle prediction;
    public Toggle reconciliation;
    public Toggle interpolation;
    public TMP_InputField lagTimeField;
    public static CanvasManager Instance { get; private set; } = null;

    private void Awake()
    {
        if( Instance != null && Instance != this )
            Destroy( gameObject );
        else
            Instance = this;
    }

    void Start()
    {
        interpolation.isOn = true;
    }

    public void OnLagChanged()
    {
        float newLag;
        if( float.TryParse( lagTimeField.text, out newLag ) )
        {
            NetworkMan.Instance.estimatedLag = newLag;
        }
        
    }
}
