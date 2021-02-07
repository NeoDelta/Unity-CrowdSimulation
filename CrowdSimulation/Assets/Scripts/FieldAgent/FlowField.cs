using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct FieldCell
{
    public Vector3 position;
    public Vector3 direction;
    public float cost;
}
public class FlowField
{
    protected List<FieldCell> field;
    protected int fieldSizeX { get; set; }
    protected int fieldSizeZ { get; set; }
    protected Vector3 offset { get; set; }
    protected int goalIdx { get; set; }

    public FlowField()
    {
        this.fieldSizeX = 1;
        this.fieldSizeZ = 1;
        this.offset = Vector3.zero;
        this.goalIdx = positionToIndex(Vector3.zero);
    }
    public FlowField(int sizeX, int sizeZ, Vector3 offset, Vector3 goalPosition)
    {
        fieldSizeX = sizeX;
        fieldSizeZ = sizeZ;
        this.offset = offset;
        goalIdx = positionToIndex(goalPosition);

        //GenerateField();
        //CreateFlow();
    }

    public void SetField(List<FieldCell> f)
    {
        field = new List<FieldCell>();

        foreach (FieldCell c in f)
            field.Add(new FieldCell { 
                position = new Vector3(c.position.x, c.position.y, c.position.z),
                direction = new Vector3(c.direction.x, c.direction.y, c.direction.z),
                cost = c.cost
            });
    }

    public List<FieldCell> GetField()
    {
        return field;
    }

    public FieldCell GetFieldValues(Vector3 p)
    {
        int idx = positionToIndex(p);
        return field[idx];
    }

    protected virtual int positionToIndex(Vector3 p)
    {
        Vector3 pos = p - offset;
        int idx = Mathf.RoundToInt(pos.x) * fieldSizeZ + Mathf.RoundToInt(pos.z);

        return idx;
    }

    public virtual void GenerateField()
    {
        field = new List<FieldCell>();

        for(int x = 0; x < fieldSizeX; x++)
        {
            for(int z = 0; z < fieldSizeZ; z++)
            {
                FieldCell newCell = new FieldCell {
                    position = new Vector3(x, 1f, z) + offset, 
                    direction = Vector3.zero, 
                    cost = CheckCellObstacle(new Vector3(x,1.0f,z) + offset)
                };

                field.Add(newCell);
            }
        }
    }

    protected float CheckCellObstacle(Vector3 p)
    {
        int layerMask = 1 << 8;
        Collider[] colliders = Physics.OverlapSphere(p, 0.5f, layerMask);

        //if (colliders.Length > 0) Debug.Log("Obstructed");

        if (colliders.Length > 0) 
            return -1.0f;           
        else
            return float.MaxValue;
    }

    protected virtual void GetNeighbours(int idx, out List<int> neigh)
    {
        neigh = new List<int>();

        int maxIdx = fieldSizeX * fieldSizeZ;

        //N, S
        if (idx + fieldSizeZ < maxIdx) neigh.Add(idx + fieldSizeZ);
        if (idx - fieldSizeZ >= 0) neigh.Add(idx - fieldSizeZ);

        //W, SW, NW
        if (idx % fieldSizeZ != fieldSizeZ - 1)
        {
            neigh.Add(idx + 1);

            if (idx + 1 + fieldSizeZ < maxIdx)  neigh.Add(idx + 1 + fieldSizeZ);
            if (idx + 1 - fieldSizeZ >= 0) neigh.Add(idx + 1 - fieldSizeZ);
        }

        //E, NE, SE
        if (idx % fieldSizeZ != 0)
        {
            neigh.Add(idx - 1);

            if (idx - 1 + fieldSizeZ < maxIdx)  neigh.Add(idx - 1 + fieldSizeZ);
            if (idx - 1 - fieldSizeZ >= 0) neigh.Add(idx - 1 - fieldSizeZ);
        }
    }

    public void CreateFlow()
    {
        List<int> openList = new List<int>();
        List<int> closeList = new List<int>();

        FieldCell cell = field[goalIdx];
        cell.cost = 0.0f;
        field[goalIdx] = cell;

        openList.Add(goalIdx);

        while(openList.Count > 0)
        {
            List<int> neighbours;
            int currentIdx = GetLowestFCost(openList);

            openList.Remove(currentIdx);
            closeList.Add(currentIdx);

            GetNeighbours(currentIdx, out neighbours);

            foreach(int idx in neighbours)
            {
                FieldCell c = field[idx];

                if (closeList.Contains(idx)) continue;
                if (c.cost == -1.0f)
                {
                    if (!closeList.Contains(idx))
                        closeList.Add(idx);
                    continue;
                }
                
                float newCost = field[currentIdx].cost + (field[currentIdx].position - c.position).sqrMagnitude;
                if (newCost < c.cost)
                {
                    c.cost = newCost;
                    c.direction = (field[currentIdx].position - c.position).normalized;
                    field[idx] = c;
                }
                            
                if (!openList.Contains(idx))
                    openList.Add(idx);
            }
        }

        for(int idx = 0; idx < field.Count; idx++)
        {
            if (field[idx].cost == -1)
            {
                List<int> neighbours;
                GetNeighbours(idx, out neighbours);

                int bestNeigh = GetLowestFCost(neighbours);

                if (bestNeigh != 0)
                {
                    FieldCell c = field[idx];
                    c.direction = (field[bestNeigh].position - c.position).normalized;
                    field[idx] = c;
                }

            }
        }
    }

    protected int GetLowestFCost(List<int> openList)
    {
        int lowestCostNodeIndx = 0;
        float lowestCost = float.MaxValue;

        foreach (int idx in openList)
        {
            if (field[idx].cost < lowestCost && field[idx].cost >= 0)
            {
                lowestCostNodeIndx = idx;
                lowestCost = field[idx].cost;
            }               
        }

        return lowestCostNodeIndx;
    }

}
