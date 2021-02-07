using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spawnPawns : MonoBehaviour
{
    // Start is called before the first frame update
    public float columns;
    public float files;
    public Vector3 destinationVector;
    public GameObject original;
    public PathFindingSystem pfs;

    void OnEnable()
    {
        for (int i = 0; i < columns; i+=1)
        {
            for (int x = 0; x < files; x += 1)
            {
                Vector3 pos = transform.position +  new Vector3( i%columns * 2 , 0, -x%files * 2);
                Quaternion ori = new Quaternion(0, 0, 0, 0);

                GameObject copy = Instantiate(original, pos, ori);
                PawnController copyC = copy.GetComponent<PawnController>();
                //copyC.destination = destinationVector;
                copyC.pfs = this.pfs;
                copyC.maxSpeed = 1.0f;//Random.Range(1.5f, 1.5f);
                //copyC.waitTime = UnityEngine.Random.Range(0.25f, 1.0f);
            }
                
        }
    }

}
