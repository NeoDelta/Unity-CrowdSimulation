using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using System;
using Unity.Collections;


[System.Serializable]
public struct PathPortal
{
    public Vector3 v1;
    public Vector3 v2;
    public int nodeIndex;
}

[System.Serializable]
public struct Node
{
    public int index;
    public Vector3 center;
    public int verticesIndex;
    public List<int> neighboursIndices;
    public int agentInArea;
    public float densityCost;
    public float area;
}

[System.Serializable]
public class PathFindingSystem : MonoBehaviour
{
    //public NavMesh rawNavMesh;
    [HideInInspector]  public List<Node> nodes;
    [HideInInspector]  private List<Neighbour> neighbours;
    [HideInInspector]  private List<Vector3> meshVertices;
    [HideInInspector]  private List<int> meshIndices;


    [System.Serializable]
    private struct Portal
    {
        public Vector3 v1;
        public Vector3 v2;
    }

    [System.Serializable]
    private struct Neighbour
    {
        public Portal portal;
        public int nodeIndex;
    }

    public void OnEnable()
    {
        BuildNodes();
    }

    /// <summary>
    /// Builds all the nodes and neighbours data structures from the NavMesh.
    /// </summary>
    public void BuildNodes()
    {
        var navMesh = NavMesh.CalculateTriangulation();

        meshVertices = new List<Vector3>();
        foreach (Vector3 v in navMesh.vertices)
            meshVertices.Add(v);

        meshIndices = new List<int>();
        foreach (int idx in navMesh.indices)
            meshIndices.Add(idx);

        nodes = new List<Node>();
        neighbours = new List<Neighbour>();

        // Initialize node lists
        for(int index = 0; index < meshIndices.Count; index+=3)
        {
            Vector3 v1 = meshVertices[meshIndices[index]];
            Vector3 v2 = meshVertices[meshIndices[index+1]];
            Vector3 v3 = meshVertices[meshIndices[index+2]];

            Vector3 polygonCenter = (v1 + v2 + v3) / 3.0f;

            nodes.Add(new Node { 
                index = index/3, 
                center = polygonCenter, 
                verticesIndex = index, 
                neighboursIndices = new List<int>(), 
                agentInArea = 0,
                densityCost = 0,
                area = TriangleArea(v1, v2, v3) 
            });
        };

        // Find node neighbors
        for (int idx1 = 0; idx1 < meshIndices.Count; idx1 += 3)
        {
            Vector3 v11 = meshVertices[meshIndices[idx1]];
            Vector3 v12 = meshVertices[meshIndices[idx1 + 1]];
            Vector3 v13 = meshVertices[meshIndices[idx1 + 2]];

            List<Vector3> verts1 = new List<Vector3>() { v11, v12, v13 };

            for (int idx2 = idx1+3; idx2 < meshIndices.Count; idx2 += 3)
            {
                if (idx2 == idx1) continue;

                Vector3 v21 = meshVertices[meshIndices[idx2]];
                Vector3 v22 = meshVertices[meshIndices[idx2 + 1]];
                Vector3 v23 = meshVertices[meshIndices[idx2 + 2]];

                List<Vector3> verts2 = new List<Vector3>() { v21, v22, v23 };

                (bool isNeighbor, Portal portal) = EdgeInCommon(verts1, verts2);
                if (isNeighbor)
                {
                    neighbours.Add(new Neighbour { portal = portal, nodeIndex = idx2 / 3 });

                    Node node = nodes[idx1 / 3];
                    node.neighboursIndices.Add(neighbours.Count-1);
                    nodes[idx1 / 3] = node;

                    neighbours.Add(new Neighbour { portal = portal, nodeIndex = idx1 / 3 });

                    node = nodes[idx2 / 3];
                    node.neighboursIndices.Add(neighbours.Count - 1);
                    nodes[idx2 / 3] = node;

                }

                if (nodes[idx1 / 3].neighboursIndices.Count >= 3) break;
            };
        };

        // Debugging purposes
        Debug.Log("Number of nodes: " + nodes.Count);
        Debug.Log("Number of neighbours: " + neighbours.Count);
    }

    /// <summary>
    /// Calculates the area of a triangle based on the given vertices
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <returns> Area of the triangle </returns>
    private float TriangleArea(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        float a = (v1 - v2).magnitude;
        float b = (v3 - v2).magnitude;
        float c = (v1 - v3).magnitude;

        float s = (a + b + c) / 2;

        return Mathf.Sqrt(s * (s - a) * (s - b) * (s - c));
    }

    /// <summary>
    /// Returns a bool indicating wheter the two provided nodes have and edge in common.
    /// Also, if true, gives the edge as a portal.
    /// </summary>
    /// <param name="verts1"> Vertices of one node.</param>
    /// <param name="verts2"> Vertices of another node.</param>
    /// <param name="p"> Portal to be filled if the node are neighbours.</param>
    /// <returns></returns>
    private (bool, Portal) EdgeInCommon(List<Vector3> verts1, List<Vector3> verts2)
    {
        bool vertInCommon = false;
        bool edgeInCommon = false;
        Portal p = new Portal { v1 = Vector3.zero, v2 = Vector3.zero };

        foreach(Vector3 v1 in verts1)
        {
            foreach (Vector3 v2 in verts2)
            {
                float distance = Mathf.Abs((v1 - v2).magnitude);

                if (distance <= 0.00005 && !vertInCommon)
                {
                    vertInCommon = true;
                    p.v1 = v1;
                }
                else if (distance <= 0.00005 && vertInCommon)
                {
                    edgeInCommon = true;
                    p.v2 = v1;
                    break;
                }
            }
        }

        return (edgeInCommon, p);
    }

    /// <summary>
    /// This job perform an A* path search.
    /// 
    /// Inputs:
    ///     startIndex: Index of the starting node (agents current node)
    ///     endIndex: Index of the objective node
    ///     nodes: List of the nav mesh nodes
    ///     neighbours: List of node neighbours
    /// </summary>
    public List<PathPortal> PathFinding(int startIndex, int endIndex, bool useDensityCost = true)
    {

        NativeArray<int> parents = new NativeArray<int>(nodes.Count, Allocator.Temp);

        List<int> openList = new List<int>();
        List<int> closeList = new List<int>();

        //Initialize costs and parents indices
        List<Vector2> costs = new List<Vector2>(); //x for g and y for h costs

        for (int i = 0; i < nodes.Count; i++)
        {
            float distance = Mathf.Abs((nodes[i].center - nodes[endIndex].center).magnitude);
            costs.Add(new Vector2(float.MaxValue, distance));
            //parents.Add(-1);
            parents[i] = -1;
         }

        openList.Add(startIndex);
        Vector2 startCost = costs[startIndex];
        startCost.x = 0.0f;
        costs[startIndex] = startCost;

        while (openList.Count > 0)
        {
            // Find lowest cost node
            int currentNodeIdx = GetLowestFCost(costs, openList);

            if (currentNodeIdx == endIndex) break; // Path finished

            // Remove current node from list and add it to close list
            openList.Remove(currentNodeIdx);
            closeList.Add(currentNodeIdx);

            // Explore current node neighborhood
            foreach (int idx in nodes[currentNodeIdx].neighboursIndices)
            {
                int neighbourNodeIndex = neighbours[idx].nodeIndex;

                if (closeList.Contains(neighbourNodeIndex)) continue; // Node already visited

                float newGCost = costs[currentNodeIdx].x + (nodes[currentNodeIdx].center - nodes[neighbourNodeIndex].center).magnitude;
                if (useDensityCost) newGCost += nodes[neighbourNodeIndex].densityCost * 10.0f;

                if (newGCost < costs[neighbourNodeIndex].x)
                {
                    parents[neighbourNodeIndex] = currentNodeIdx;
                    Vector2 c = costs[neighbourNodeIndex];
                    c.x = newGCost;
                    costs[neighbourNodeIndex] = c;

                    if (!openList.Contains(neighbourNodeIndex))
                        openList.Add(neighbourNodeIndex);

                }

            }

        }

        // Create Path
        List<PathPortal> pathNodes = new List<PathPortal>();
        if (parents[endIndex] == -1 || parents[endIndex] == endIndex)
        {
            Debug.Log("Path not found "+parents[endIndex]);
        }
        else
        {
            // Path found
            int currentIdx = endIndex;
            while (parents[currentIdx] != -1)
            {

                Vector3 v1 = Vector3.zero;
                Vector3 v2 = Vector3.zero;

                foreach (int i in nodes[currentIdx].neighboursIndices)
                {
                    if (neighbours[i].nodeIndex == parents[currentIdx])
                    {
                        v1 = neighbours[i].portal.v1;
                        v2 = neighbours[i].portal.v2;
                    }
                }

                currentIdx = parents[currentIdx];
                pathNodes.Add(new PathPortal { v1 = v1, v2 = v2, nodeIndex = currentIdx});
                //currentIdx = parents[currentIdx];

            }

        }

        // Dispose of all native arrays
        parents.Dispose();

        return pathNodes;
    }

    /// <summary>
    /// Return the index (int) of the node with the lowest F cost in the openList.
    /// </summary>
    /// <param name="costs"> Native array containing the cost of all nodes</param>
    /// <param name="openList"> Native list of the open list of nodes</param>
    /// <returns></returns>
    private int GetLowestFCost(List<Vector2> costs, List<int> openList)
    {
        int lowestCostNodeIndx = openList[0];

        for (int i = 0; i < openList.Count; i++)
        {
            if ((costs[i].x + costs[i].y) < (costs[lowestCostNodeIndx].x + costs[lowestCostNodeIndx].y))
                lowestCostNodeIndx = openList[i];
        }

        return lowestCostNodeIndx;
    }

    /// <summary>
    /// Searches for the closest nav mesh node to a given agent.
    /// This may not be the best solution, but provides more flexibility for now
    /// </summary>
    public int FindClosestNodeToAgent(Vector3 agentPosition)
    {
        int closestNodeIndex = nodes[0].index;
        float closestDistance = float.MaxValue;


        for(int n = 0; n < nodes.Count; n++)
        {
            Node node = nodes[n];
            //Vector3 a = nodeVertices[node.verticesIndex];
            //Vector3 b = nodeVertices[node.verticesIndex+1];
            //Vector3 c = nodeVertices[node.verticesIndex+2];
            float distance = (node.center - agentPosition).sqrMagnitude;
            if ( distance < closestDistance)
            {
                closestDistance = distance;
                closestNodeIndex = node.index;
            }
        }

        return closestNodeIndex;
    }

    /*private void Update()
    {
        //for (int i = 0; i < neighbours.Count; i++)
        //Debug.DrawLine(neighbours[i].portal.v1, neighbours[i].portal.v2);
        
    }*/

    void OnDrawGizmos()
    {
        for (int i = 0; i < nodes.Count; i++)
            UnityEditor.Handles.Label(nodes[i].center, nodes[i].densityCost.ToString());
    }

    public void DrawPortals()
    {
        for (int i = 0; i < neighbours.Count; i++)
        {
            GameObject portal = new GameObject("Portal" + i);

            portal.AddComponent<LineRenderer>();
            LineRenderer lr = portal.GetComponent<LineRenderer>();

            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.startWidth = 0.1f;
            lr.endWidth = 0.1f;
            lr.SetPosition(0, neighbours[i].portal.v1);
            lr.SetPosition(1, neighbours[i].portal.v2);
            lr.material = (Material)Resources.Load<Material>("Assets/Prefabs/AgentPrefabs/WithAStar/AttractorMaterial");

            portal.AddComponent<PortalVisualizationController>();

           
        }
    }
}
