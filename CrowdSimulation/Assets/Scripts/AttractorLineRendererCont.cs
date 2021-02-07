using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttractorLineRendererCont : MonoBehaviour
{
    // Start is called before the first frame update
    private PawnController controller;
    private LineRenderer lineRenderer;
    private SimulationController simCont;
    void Start()
    {
        controller = GetComponentInParent<PawnController>();
        lineRenderer = GetComponent<LineRenderer>();
        simCont = controller.simCont;

        SimulationController.current.onShowAttractorVectorChange += ShowLineRenderer;
        if (!SimulationController.current.showAttractorVector) ShowLineRenderer();
    }

    // Update is called once per frame
    void Update()
    {
        //if (!simCont.showAttractorVector) lineRenderer.enabled = false;
        lineRenderer.SetPosition(0, controller.transform.position);
        lineRenderer.SetPosition(1, controller.getCurrentCorner());
    }

    private void ShowLineRenderer()
    {
        lineRenderer.enabled = !lineRenderer.enabled;
    }

    private void OnDestroy()
    {
        SimulationController.current.onShowAttractorVectorChange -= ShowLineRenderer;
    }
}
