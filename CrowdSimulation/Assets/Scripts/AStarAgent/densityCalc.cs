using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class densityCalc : MonoBehaviour
{
    private float numAgents;
    private PawnController parentController;
    //private FieldAgent parentController;
    private BoxCollider densityBox;

    private void Start()
    {
        numAgents = 0;
        parentController = this.GetComponentInParent<PawnController>();
        //parentController = this.GetComponentInParent<FieldAgent>();
        densityBox = this.GetComponent<BoxCollider>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetType() == typeof(CapsuleCollider) && other.gameObject.tag == "Agent")
        {
            numAgents += 1;
            parentController.setDensity(calcDensity());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetType() == typeof(CapsuleCollider) && other.gameObject.tag == "Agent")
        {
            numAgents -= 1;
            parentController.setDensity(calcDensity());
        }
    }

    private float calcDensity()
    {
        float x = densityBox.size.x;
        float z = densityBox.size.z;

        float d = (numAgents + 1) / (x * z);

        return d;
    }
}
