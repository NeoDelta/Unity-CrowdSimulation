using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

public class PawnController : MonoBehaviour
{
    // ---------------------------------------
    // Añadir randomness a ciertos elementos (avoidance, right preference)
    //--------------------------------------
    //Animation
    public ThirdPersonCharacter character;

    //SimulationController
    public SimulationController simCont;

    // Navigation
    public PathFindingSystem pfs;
    private List<PathPortal> pathPortals;
    private bool hasPath;
    private Vector3 currentCorner;
    private int destNode;

    // Steering/movement variables
    public Vector3 velocity;
    public float acceleration;
    public float maxSpeed;
    public float waitTime;
    public float reactionTime;
    public bool showWaitingRule = true;
    public Vector3 steerWeights;
    private bool wait = false;
    private float remainingTime;

    // Forces
    private Vector3 agentsAvoidanceForce;
    private Vector3 repulsionForce;

    // Influence
    public float densityFactor;
    private float density;
    private BoxCollider influenceBox;

    // Visualization
    private Material influenceBoxMat;
    private Material surfaceMat;
    private Color baseColor;

    private int elapsedFrames = 0;

    float ti = 0;
 
    void Start()
    {
        hasPath = false;

        agentsAvoidanceForce = Vector3.zero;
        repulsionForce = Vector3.zero;
        //waitTime = UnityEngine.Random.Range(0.25f, 1.0f);
        influenceBox = this.transform.Find("InfluenceBox").GetComponent<BoxCollider>();
        influenceBoxMat = influenceBox.GetComponentInChildren<MeshRenderer>().material;

        surfaceMat = this.transform.Find("xbot").Find("Beta_Surface").GetComponent<SkinnedMeshRenderer>().material;
        baseColor = this.transform.Find("xbot").Find("Beta_Surface").GetComponent<SkinnedMeshRenderer>().material.color;

        int startNode;
        pathPortals = new List<PathPortal>();
        pathPortals = pfs.PathFinding(startNode = pfs.FindClosestNodeToAgent(transform.position), destNode = pfs.FindClosestNodeToAgent(new Vector3(5,1,-11f)));
        Node node = pfs.nodes[startNode];
        node.agentInArea += 1;
        node.densityCost = node.agentInArea / node.area;
        pfs.nodes[startNode] = node;

        if (pathPortals.Count > 0) hasPath = true;  

        SimulationController.current.onShowWaitingRuleChange += setShowWaitingRule;
        SimulationController.current.onPauseChange += IsPaused;
    }

    // Update is called once per frame
    void FixedUpdate()
    {


        if (simCont.isPaused)
        {
            return;
        }

        if (hasPath)
        {

            resizeInfluenceBox(density);
            if (wait)
            {

                remainingTime += Time.deltaTime;
                if (remainingTime >= waitTime)
                {
                    wait = false;
                    surfaceMat.color = baseColor;
                }

                float vel2 = velocity.sqrMagnitude - acceleration * Time.deltaTime;
                if (vel2 <= 0.0f) vel2 = 0.0f;

                velocity = Vector3.zero;
                //velocity = vel2 * velocity.normalized;
                //transform.position += vel2 * velocity.normalized * Time.deltaTime;
                character.Move(velocity, false, false);

                return;
            }

            if (ti >= reactionTime)
            {
                ti = 0f;
            }
            else
            {
                ti += Time.deltaTime;
                float vel = velocity.magnitude + acceleration * Time.deltaTime;
                if (vel > maxSpeed) vel = maxSpeed;
                velocity = vel * velocity.normalized;

                //Repulsion forces
                if (Vector3.Dot(velocity.normalized, repulsionForce.normalized) < 0.0f && repulsionForce.magnitude > 0)
                {
                    wait = true;
                    remainingTime = 0.0f;

                    if (showWaitingRule) surfaceMat.color = new Color(1, 0, 0, baseColor.a);
                }

                transform.position += new Vector3(velocity.x, 0.0f, velocity.z) * Time.deltaTime;
                character.Move(new Vector3(velocity.x, 0.0f, velocity.z), false, false);

                //Reset avoidance forces for next iteration
                agentsAvoidanceForce = Vector3.zero;
                repulsionForce = Vector3.zero;
                return;
            }

            //Atractor force
            currentCorner = getProyectedWaypoint3D(pathPortals[pathPortals.Count-1].v1, pathPortals[pathPortals.Count - 1].v2);
            Debug.DrawLine(transform.position, transform.position + (currentCorner - transform.position).normalized * 3 , Color.blue);

            float distanceToCorner = (currentCorner - transform.position).sqrMagnitude;
            Vector3 direction = (currentCorner - transform.position).normalized;
            Vector3 atractor = direction * maxSpeed;

            //Avoidance forces
            Vector3 agentAvoidance = agentsAvoidanceForce;
            Vector3 obstacleAvoidance = Vector3.zero;

            // Obstacle avoidance
            NavMeshHit hit;
            bool blocked = NavMesh.Raycast(transform.position, transform.position + velocity.normalized * this.transform.Find("InfluenceBox").GetComponent<BoxCollider>().size.z, out hit, NavMesh.AllAreas);
            //Debug.DrawLine(transform.position, transform.position + velocity.normalized * this.transform.Find("InfluenceBox").GetComponent<BoxCollider>().size.z, blocked ? Color.red : Color.green);
            if (blocked) obstacleAvoidance += CalcWallAvoidanceForce(hit.normal);
            obstacleAvoidance = obstacleAvoidance.normalized * maxSpeed;

            // Agent avoidance
            agentAvoidance.y = 0.0f;
            agentAvoidance = agentAvoidance.normalized * maxSpeed;

            Vector3 steering = velocity + atractor * steerWeights.x + agentAvoidance * steerWeights.y + obstacleAvoidance * steerWeights.z;
            steering = steering.normalized;

            //Repulsion forces
            if (Vector3.Dot(velocity.normalized, repulsionForce.normalized) < 0.0f && repulsionForce.magnitude > 0)
            {
                wait = true;
                remainingTime = 0.0f;

                if (showWaitingRule) surfaceMat.color = new Color(1,0,0, baseColor.a);
            }

            if (repulsionForce.magnitude > 0.0f) velocity = Vector3.zero;
            else
            {
                float vel = velocity.magnitude + acceleration * Time.deltaTime;
                if (vel > maxSpeed) vel = maxSpeed;
                Vector3 newVelocity = vel * steering;

                //if (Vector3.Angle(velocity, newVelocity) > 180 * Time.deltaTime) velocity = (Quaternion.AngleAxis(180 * Time.deltaTime, Vector3.up) * velocity).normalized * vel;
                //else velocity = newVelocity;

                velocity = newVelocity;

            }

            
            transform.position += new Vector3(velocity.x, 0.0f, velocity.z) * Time.deltaTime;
            //transform.rotation = Quaternion.LookRotation(velocity.normalized);
            character.Move(new Vector3(velocity.x, 0.0f, velocity.z), false, false);

            Debug.DrawLine(transform.position, transform.position + new Vector3(velocity.x, 0.0f, velocity.z).normalized, Color.red);

            if (distanceToCorner < 1.0f)
            {
                Node node = pfs.nodes[pathPortals[pathPortals.Count - 1].nodeIndex];
                if(node.agentInArea >= 1)
                {
                    node.agentInArea -= 1;
                    node.densityCost = node.agentInArea / node.area;
                    pfs.nodes[pathPortals[pathPortals.Count - 1].nodeIndex] = node;
                }
                
                pathPortals.RemoveAt(pathPortals.Count - 1);
                if (pathPortals.Count == 0) hasPath = false;
                else
                {
                    node = pfs.nodes[pathPortals[pathPortals.Count - 1].nodeIndex];
                    node.agentInArea += 1;
                    node.densityCost = node.agentInArea / node.area;
                    pfs.nodes[pathPortals[pathPortals.Count - 1].nodeIndex] = node;

                    if (pathPortals.Count - 2 >= 0)
                    {
                        if (pfs.nodes[pathPortals[pathPortals.Count - 2].nodeIndex].densityCost >= 2)
                        {
                            int startNode = pathPortals[pathPortals.Count - 1].nodeIndex;
                            pathPortals.Clear();
                            pathPortals = pfs.PathFinding(startNode, destNode, true);

                            if (pathPortals.Count > 0) hasPath = true;
                            else hasPath = false;
                        }
                    }
                }            
            }

            //Reset avoidance forces for next iteration
            agentsAvoidanceForce = Vector3.zero;
            repulsionForce = Vector3.zero;

        }

        if (!hasPath)
        {
            //agent.enabled = true;
            //agent.SetDestination(RandomNavmeshLocation(50f));
            int startNode = pfs.FindClosestNodeToAgent(transform.position);
            pathPortals.Clear();
            pathPortals = pfs.PathFinding(startNode, destNode = pfs.FindClosestNodeToAgent(RandomNavmeshLocation(50f)), true);

            if (pathPortals.Count > 0) hasPath = true;
        }
    }

    public Vector3 RandomNavmeshLocation(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;

        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;

        if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1))
        {
            finalPosition = hit.position;
        }

        return finalPosition;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.tag == "Player")
            repulsionForce += collision.impulse;
        if (collision.collider.gameObject.tag == "Agent")
            repulsionForce += collision.impulse;
            //repulsionForces.Add(collision);
           
    }

    /*private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.gameObject.tag == "Agent")
            //repulsionForces.Remove(collision);
    }*/

    public void AddAgentAvoidanceForce(Vector3 pos, Vector3 dir)
    {
        Vector3 distance = (pos - (transform.position + velocity * Time.deltaTime));
        if (distance.magnitude < 0.4f) repulsionForce += ((transform.position + velocity * Time.deltaTime) - pos).normalized * velocity.magnitude;

        agentsAvoidanceForce += CalcAgentAvoidanceForce(pos, dir);
    }

    private Vector3 CalcAgentAvoidanceForce(Vector3 pos, Vector3 dir)
    {
        Vector3 tanForce = Vector3.zero;
        Vector3 distance = pos - transform.position;

        tanForce = Vector3.Cross(distance, velocity);
        tanForce = Vector3.Cross(tanForce, distance).normalized;

        //Right bias
        float bias;
        if (density >= densityFactor) bias = -0.5f;
        else bias = 0.0f;

        if (Mathf.Abs(Vector3.Dot(velocity.normalized, dir.normalized)) <= bias)
        {

            Vector3 rightForce = new Vector3(velocity.z, velocity.y, -velocity.x);
            tanForce += rightForce * 0.2f;
            //tanForce = tanForce.normalized;

        }

        tanForce *= Mathf.Pow(distance.sqrMagnitude - influenceBox.size.z, 2.0f);

        if (Vector3.Dot(velocity, dir) > 0)
            tanForce *= 1.2f;
        else
            tanForce *= 2.4f;
  
        return tanForce;
    }

    private Vector3 CalcObstacleAvoidanceForce(Vector3 pos)
    {
        Vector3 tanForce;
        Vector3 distance = pos - transform.position;

        tanForce = Vector3.Cross(distance, velocity);
        tanForce = Vector3.Cross(tanForce, distance).normalized;

        return tanForce;
    }

    private Vector3 CalcWallAvoidanceForce(Vector3 normal)
    {
        return Vector3.Reflect(velocity, normal);
    }

    public void setDensity(float d)
    {
        density = d;
    }

    public void resizeInfluenceBox(float density)
    {
        if (density >= densityFactor)
        {
            influenceBox.size   = new Vector3(influenceBox.size.x, 1.0f, 1.5f);
            influenceBox.center = new Vector3(0.0f, 0.0f, 0.75f);
            influenceBoxMat.color = new Color(1, 0, 0, influenceBoxMat.color.a);
        }
        else
        {
            influenceBox.size   = new Vector3(influenceBox.size.x, 1.0f, 3.0f);
            influenceBox.center = new Vector3(0.0f, 0.0f, 1.5f);
            influenceBoxMat.color = new Color(0, 1, 0, influenceBoxMat.color.a);
        }
    }

    /// <summary>
    /// Calculates the projection of the agent position to the destination portal (represented as it's to end points).
    /// </summary>
    /// <param name="v1"> End point 1 of the portal.</param>
    /// <param name="v2"> End point 2 of the portal.</param>
    /// <returns></returns>
    public Vector3 getProyectedWayPoint(Vector3 v1, Vector3 v2)
    {
        float x = transform.position.x;
        float y = transform.position.z;
        float x1 = v1.x;
        float y1 = v1.z;
        float x2 = v2.x;
        float y2 = v2.z;

        float A = x - x1;
        float B = y - y1;
        float C = x2 - x1;
        float D = y2 - y1;

        float dot = A * C + B * D;
        float len_sq = C * C + D * D;
        float param = -1;
        if (len_sq != 0) //in case of 0 length line
            param = dot / len_sq;

        float xx, yy;

        if (param < 0)
        {
            xx = x2;
            yy = y2;
        }
        else if (param > 1)
        {
            xx = x1;
            yy = y1;
        }
        else
        {
            xx = x1 + param * C;
            yy = y1 + param * D;
        }
        return new Vector3(xx, transform.position.y, yy);
    }

    public Vector3 getProyectedWaypoint3D(Vector3 a, Vector3 b)
    {

        Vector3 p = transform.position;

        Vector3 ap = p - a;
        Vector3 ab = b - a;

        float param = Vector3.Dot(ap, ab) / Vector3.Dot(ab, ab);

        if (param < 0) return a;
        else if (param > 1) return b;
        else return a + param * ab;
    }

    public void setShowWaitingRule()
    {
        showWaitingRule = !showWaitingRule;

        if (showWaitingRule)
        {
            if (wait)
                surfaceMat.color = new Color(1, 0, 0, baseColor.a);
            else
                surfaceMat.color = baseColor;
        }
        else
        {
            if (wait)
                surfaceMat.color = baseColor;
        }

    }

    private void IsPaused()
    {
        if (simCont.isPaused)
            character.changeAnimatorSpeed(0f);
        else
            character.changeAnimatorSpeed(1f);
    }

    public Vector3 getCurrentCorner()
    {
        return currentCorner;
    }

    private void OnDestroy()
    {
        SimulationController.current.onShowWaitingRuleChange -= setShowWaitingRule;
        SimulationController.current.onPauseChange -= IsPaused;
    }
}

