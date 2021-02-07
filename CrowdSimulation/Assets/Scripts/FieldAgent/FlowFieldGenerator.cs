using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FlowFieldGenerator : MonoBehaviour
{
    [HideInInspector] public List<FlowFieldSemi2D> ff;

    public Vector3 size;
    public Vector3 goal;

    private List<Vector3> goals;
    // Start is called before the first frame update
    void OnEnable()
    {
        goals = new List<Vector3>();
        ff = new List<FlowFieldSemi2D>();
        goals.Add(goal);
        //goals.Add(new Vector3(-42, 7, 45));
        //goals.Add(new Vector3(4, 12, -3));
        //goals.Add(new Vector3(-60, 10, -170));
        //goals.Add(new Vector3(0, 2, -28));
        //goals.Add(new Vector3(13.7f, 1, 18));
        //goals.Add(new Vector3(9, 20, -110));
        //goals.Add(new Vector3(0, 1, 0));
        //goals.Add(new Vector3(0, 1, 110));
        //goals.Add(new Vector3(110, 1, 110));

        //Instantiete flow fields
        foreach (Vector3 g in goals)
            ff.Add(new FlowFieldSemi2D((int)size.x, (int)size.z, (int)size.y, transform.position, g));

        ff[0].GenerateField();

        for(int i = 1; i < ff.Count; i++)
        {
            ff[i].SetField(ff[0].GetField());
            //ff[i].GenerateField();
            ff[i].CreateFlow();
        }

        ff[0].CreateFlow();

        //Debug.Log(ff[1].GetField().Count);

    }

    public void GetRandomGoal(out FlowFieldSemi2D f, out Vector3 g)
    {
        int goal = Random.Range(0, goals.Count);
        g = goals[goal];
        f = ff[goal];

        FieldCell2 c;
        bool b = f.positionToCell(g, out c);
        g = c.position;
        
    }

    private void Update()
    {
        //Handles.Label(transform.position, "Text");
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.z; z++)
            {
                Vector3 p = new Vector3(x, 0, z);

                List<FieldCell2> b = ff[0].positionToColumn(p+transform.position);
                //FieldCell fc = ff[0].GetFieldValues(p + transform.position);
                foreach(FieldCell2 c2 in b)
                {
                    //if (c2.cost != -1) Handles.Label(c2.position, c2.cost.ToString());
                    Debug.DrawLine(c2.position, c2.position + c2.direction * 0.5f);
                }

            }
         }
    }
}
