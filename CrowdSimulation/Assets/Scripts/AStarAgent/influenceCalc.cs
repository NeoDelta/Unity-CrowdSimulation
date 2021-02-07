﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class influenceCalc : MonoBehaviour
{
    private PawnController parentController;
    //private FieldAgent parentController;
    public void Start()
    {
        parentController = this.GetComponentInParent<PawnController>();
        //parentController = this.GetComponentInParent<FieldAgent>();
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.GetType() == typeof(CapsuleCollider) && other.gameObject.tag == "Agent")
        {
            PawnController otherAgent = other.gameObject.GetComponent<PawnController>();
            //FieldAgent otherAgent = other.gameObject.GetComponent<FieldAgent>();
            if(otherAgent.velocity.magnitude == 0f) parentController.AddAgentAvoidanceForce(otherAgent.transform.position, otherAgent.transform.forward/10f);
            else parentController.AddAgentAvoidanceForce(otherAgent.transform.position, otherAgent.velocity);
        }
        if (other.GetType() == typeof(CapsuleCollider) && other.gameObject.tag == "Player")
        {
            FirstPersonController player = other.gameObject.GetComponent<FirstPersonController>();
            parentController.AddAgentAvoidanceForce(player.transform.position, player.GetMovementDirection());
        }

        //this.GetComponentInParent<PawnController>().addAgentColliderToList(other);
    }

    /*private void OnTriggerExit(Collider other)
    {
        if (other.GetType() == typeof(CapsuleCollider) && other.gameObject.tag == "Agent")
            this.GetComponentInParent<PawnController>().removeAgentColliderToList(other);
    }*/
}
