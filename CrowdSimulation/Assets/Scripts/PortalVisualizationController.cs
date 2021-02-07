using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalVisualizationController : MonoBehaviour
{
    LineRenderer lr;
    void Start()
    {
        SimulationController.current.onShowPortalsChange += ShowPortal;
        lr = this.GetComponent<LineRenderer>();
    }

    void ShowPortal()
    {
        lr.enabled = !lr.enabled;
    }

    private void OnDestroy()
    {
        SimulationController.current.onShowPortalsChange -= ShowPortal;
    }
}
