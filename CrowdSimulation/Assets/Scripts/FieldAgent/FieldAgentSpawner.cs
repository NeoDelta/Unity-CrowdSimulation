using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldAgentSpawner : MonoBehaviour
{
    public float columns;
    public float files;
    public Vector3 destinationVector;
    public GameObject original;
    public FlowFieldGenerator ffg;

    void OnEnable()
    {
        for (int i = 0; i < columns; i += 1)
        {
            for (int x = 0; x < files; x += 1)
            {
                Vector3 pos = transform.position + new Vector3(i % columns * 2, 0, -x % files * 2);
                Quaternion ori = new Quaternion(0, 0, 0, 0);

                GameObject copy = Instantiate(original, pos, ori);
                FieldAgent copyC = copy.GetComponent<FieldAgent>();
                copyC.fieldGenerator = this.ffg;
                copyC.acceleration = 10f;
                copyC.maxSpeed = 1.0f;//Random.Range(0.75f, 1.5f);
                copyC.waitTime = Random.Range(0.25f, 1.0f);
            }

        }
    }

    public void SpeedChange(string speed)
    {

    }
}
