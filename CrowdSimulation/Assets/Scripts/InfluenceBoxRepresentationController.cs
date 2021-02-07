using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfluenceBoxRepresentationController : MonoBehaviour
{
    public BoxCollider col;
    private MeshRenderer meshRenderer;
    private bool show;
    //protected Material mat;
    // Start is called before the first frame update
    void Start()
    {
        //col = GetComponentInParent<BoxCollider>();
        //mat = GetComponent<MeshRenderer>().material;

        SimulationController.current.onShowInfluenceBoxChange += ShowInfluenceBox;
        meshRenderer = GetComponent<MeshRenderer>();
        show = SimulationController.current.showInfluenceBox;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.localScale = col.size;
        this.transform.localPosition = col.center;

        //if (col.size.z < 2) mat.color = new Color(1, 0, 0, mat.color.a);
        //else mat.color = new Color(0, 1, 0, mat.color.a);
    }

    private void ShowInfluenceBox()
    {
        //meshRenderer.enabled = !meshRenderer.enabled;
        show = !show;

        if(show)
            meshRenderer.material.color = new Color(meshRenderer.material.color.r, meshRenderer.material.color.g, meshRenderer.material.color.b, 0.2f);
        else
            meshRenderer.material.color = new Color(meshRenderer.material.color.r, meshRenderer.material.color.g, meshRenderer.material.color.b, 0f);
    }

    private void OnDestroy()
    {
        SimulationController.current.onShowInfluenceBoxChange -= ShowInfluenceBox;
    }
}
