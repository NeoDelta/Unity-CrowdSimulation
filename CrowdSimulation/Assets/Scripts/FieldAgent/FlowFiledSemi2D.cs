using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public struct FieldCell2
{
    public Vector3 position;
    public Vector3 direction;
    public float cost;
    public List<int> neighbors;
}

public class FlowFieldSemi2D
{
    protected List<List<FieldCell2>> field;
    protected int fieldSizeX { get; set; }
    protected int fieldSizeZ { get; set; }
    protected int fieldMaxHeight { get; set; }
    protected Vector3 offset { get; set; } //offset.y must be zero
    protected int goalIdx { get; }
    protected Vector3 goalPosition { get; }

    public FlowFieldSemi2D(int sizeX, int sizeZ, int height, Vector3 offset, Vector3 goalPosition)
    {
        this.fieldSizeX = sizeX;
        this.fieldSizeZ = sizeZ;
        this.fieldMaxHeight = height;
        this.offset = offset;
        this.goalIdx = positionToIndex(goalPosition);
        this.goalPosition = goalPosition;
    }

    protected virtual int positionToIndex(Vector3 p)
    {
        Vector3 pos = p - offset;
        int idx = Mathf.RoundToInt(pos.x) * fieldSizeZ + Mathf.RoundToInt(pos.z);

        return idx;
    }

    public virtual List<FieldCell2> positionToColumn(Vector3 p)
    {
        int idx = positionToIndex(p);

        List<FieldCell2> col = field[idx];

        return col;
    }

    public void SetField(List<List<FieldCell2>> f)
    {
        field = new List<List<FieldCell2>>();

        foreach (List<FieldCell2> l in f)
        {
            List<FieldCell2> col = new List<FieldCell2>();
            //if (l.Count == 0) continue;
            foreach (FieldCell2 c in l)
            {
                col.Add(new FieldCell2
                {
                    position = c.position,
                    direction = c.direction,
                    cost = c.cost,
                    neighbors = c.neighbors
                });
            }

            field.Add(col);
        }
    }

    public List<List<FieldCell2>> GetField()
    {
        return field;
    }

    public virtual bool positionToCell(Vector3 p, out FieldCell2 cell)
    {
        List<FieldCell2> col = positionToColumn(p);
        cell = new FieldCell2();

        if (col.Count > 0)
        {   
               cell = closestInColumn(col, p,out int ci);
               return true;          
        }

        return false;
    }

    private FieldCell2 closestInColumn(List<FieldCell2> col, Vector3 p, out int colIdx)
    {
        FieldCell2 closest = new FieldCell2();
        float closestDist = float.MaxValue;
        colIdx = -1;

        for (int idx = 0; idx < col.Count; idx++)
        {
            FieldCell2 c = col[idx];
            float dist = (c.position - p).sqrMagnitude;
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = c;
                colIdx = idx;
            }
        }
        return closest;
    }

    public virtual void GenerateField()
    {
        field = new List<List<FieldCell2>>();
        
        for (int x = 0; x < fieldSizeX; x++)
        {
            for (int z = 0; z < fieldSizeZ; z++)
            {
                List<FieldCell2> col = new List<FieldCell2>();

                RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, fieldMaxHeight, z) + offset, Vector3.down, fieldMaxHeight*2);

                //Debug.Log(hits.Length);
                foreach (RaycastHit hit in hits)
                {
                    //if(Vector3.Dot(hit.normal, Vector3.up) > 0.66f && !Physics.Raycast(new Vector3(x, hit.point.y, z), Vector3.up, 2.5f))

                    NavMeshHit h;

                    if (Vector3.Dot(hit.normal, Vector3.up) >= 0.66f && pointInNavMesh(hit.point, out h))
                    {
                        bool posTaked = false;
                        foreach(FieldCell2 c in col)
                        {
                            if ((c.position - hit.point).sqrMagnitude < 1.0f)
                                posTaked = true;
                        }

                        if (posTaked) continue;

                        FieldCell2 newCell = new FieldCell2
                        {
                            position = hit.point,
                            direction = Vector3.zero,
                            cost = float.MaxValue,
                            neighbors = new List<int>()
                        };
                     
                        col.Add(newCell);
                    }
                }

                field.Add(col);
            }
        }

        //Si la diferencia de altura entre dos celdas es mas de 1f no pueden ser neighbors
        
        for (int x = 0; x < fieldSizeX; x++)
        {
            for (int z = 0; z < fieldSizeZ; z++)
            {
                List<FieldCell2> col = positionToColumn(new Vector3(x, 0f, z) + offset);               

                for(int i = 0; i < col.Count; i++)
                {
                    List<int> neighs = getNeighbors(positionToIndex(new Vector3(x, 0f, z)+offset), col[i]);

                    FieldCell2 c = new FieldCell2 { 
                        position = col[i].position, 
                        direction = col[i].direction, 
                        cost = col[i].cost, 
                        neighbors = neighs };

                    field[positionToIndex(new Vector3(x, 0f, z)+offset)][i] = c;
                }               
            }
        }
    }

    private bool pointInNavMesh(Vector3 p, out NavMeshHit hit)
    {
        bool inNav = NavMesh.SamplePosition(p, out hit, 0.2f, NavMesh.AllAreas);

        return inNav;
    }

    private List<int> getNeighbors(int idx, FieldCell2 c)
    {
        List<int> neigh = new List<int>();
        List<int> neighbor = new List<int>();
        int maxIdx = fieldSizeX * fieldSizeZ;

        //N, S
        if (idx + fieldSizeZ < maxIdx) neigh.Add(idx + fieldSizeZ);
        if (idx - fieldSizeZ >= 0) neigh.Add(idx - fieldSizeZ);

        //W, SW, NW
        if (idx % fieldSizeZ != fieldSizeZ - 1)
        {
            neigh.Add(idx + 1);

            if (idx + 1 + fieldSizeZ < maxIdx) neigh.Add(idx + 1 + fieldSizeZ);
            if (idx + 1 - fieldSizeZ >= 0) neigh.Add(idx + 1 - fieldSizeZ);
        }

        //E, NE, SE
        if (idx % fieldSizeZ != 0)
        {
            neigh.Add(idx - 1);

            if (idx - 1 + fieldSizeZ < maxIdx) neigh.Add(idx - 1 + fieldSizeZ);
            if (idx - 1 - fieldSizeZ >= 0) neigh.Add(idx - 1 - fieldSizeZ);
        }

        foreach(int i in neigh)
        {         
            if (field[i].Count > 0) 
            {
                NavMeshHit hit;
                Vector3 target = new Vector3(field[i][0].position.x, c.position.y, field[i][0].position.z);

                if (!NavMesh.Raycast(c.position, target, out hit, NavMesh.AllAreas))
                    neighbor.Add(i);
            }
        }

        return neighbor;
    }

    public void CreateFlow()
    {
        List<FieldCell2> openList = new List<FieldCell2>();
        List<FieldCell2> closeList = new List<FieldCell2>();
        int colIdx;

        FieldCell2 cell = closestInColumn(field[goalIdx], goalPosition, out colIdx);
        cell.cost = 0.0f;
        Debug.Log(goalIdx+"/"+field.Count);
        Debug.Log(colIdx + "/" + field[goalIdx].Count);
        field[goalIdx][colIdx] = cell;

        openList.Add(field[goalIdx][colIdx]);

        while (openList.Count > 0)
        {
            List<int> neighbours;
            FieldCell2 currentCell = GetLowestFCost(openList);

            openList.Remove(currentCell);
            closeList.Add(currentCell);

            //GetNeighbours(currentIdx, out neighbours);

            foreach (int idx in currentCell.neighbors)
            {
                FieldCell2 c = closestInColumn(field[idx], currentCell.position, out colIdx);
                FieldCell2 copy = c;
                if (closeList.Contains(field[idx][colIdx])) continue;

                float newCost = currentCell.cost + (currentCell.position - c.position).magnitude;
                if (newCost < c.cost)
                {
                    c.cost = newCost;
                    c.direction = (currentCell.position - c.position).normalized;

                    if (c.neighbors.Count >= 8)
                    {
                        Vector3 dir = Vector3.zero;
                        foreach (int idx2 in c.neighbors)
                        {
                            int colIdx2;
                            FieldCell2 c2 = closestInColumn(field[idx2], c.position, out colIdx2);
                            dir += c2.direction;
                        }

                        c.direction += dir;
                        c.direction /= c.neighbors.Count + 1;
                        c.direction = c.direction.normalized;
                    }

                    field[idx][colIdx] = c;
                }

                if (!openList.Contains(field[idx][colIdx]))
                    openList.Add(field[idx][colIdx]);
            }
        }

        /*for (int idx = 0; idx < field.Count; idx++)
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
        }*/
    }

    protected FieldCell2 GetLowestFCost(List<FieldCell2> openList)
    {
        FieldCell2 lowestCostCell = openList[0];
        float lowestCost = float.MaxValue;

        foreach (FieldCell2 idx in openList)
        {
            if (idx.cost < lowestCost && idx.cost >= 0)
            {
                lowestCostCell = idx;
                lowestCost = idx.cost;
            }
        }

        return lowestCostCell;
    }
}
