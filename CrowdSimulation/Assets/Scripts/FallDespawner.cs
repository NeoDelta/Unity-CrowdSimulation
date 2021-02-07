using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallDespawner : MonoBehaviour
{
    public SimulationController simController;
    // Start is called before the first frame update
    void Start()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Agent")
        {

        }
    }
}
