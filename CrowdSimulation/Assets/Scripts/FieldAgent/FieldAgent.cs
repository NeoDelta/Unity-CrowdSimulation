using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

public class FieldAgent : MonoBehaviour
{
    // ---------------------------------------
    // Añadir randomness a ciertos elementos (avoidance, right preference)
    //--------------------------------------
    //public Camera cam;
    public ThirdPersonCharacter character;

    public FlowFieldGenerator fieldGenerator;
    private FlowFieldSemi2D field;
    private float distanceToGoal;

    public Vector3 destination;
    public Vector3 velocity;
    public float acceleration;
    public float maxSpeed;
    public float waitTime;
    public Vector3 direction;

    private bool hasField;

    private Vector3 agentsAvoidanceForce;
    private List<Collider> collisionObstacles;
    private float density;

    //private List<Collision> repulsionForces;
    private Vector3 repulsionForce;

    private bool wait = false;
    private float remainingTime;

    public BoxCollider influenceBox;

    void OnEnable()
    {
        hasField = false;

        agentsAvoidanceForce = Vector3.zero;
        collisionObstacles = new List<Collider>();
        repulsionForce = Vector3.zero;
        waitTime = UnityEngine.Random.Range(0.25f, 1.0f);
        //influenceBox = this.transform.Find("InfluenceBox").GetComponent<BoxCollider>();
        BoxCollider[] children = GetComponentsInChildren<BoxCollider>();
        foreach (BoxCollider child in children)
        {
            if (child.name == "InfluenceBox")
            {
                influenceBox = child;
            }
        }

        //pathPortals = pfs.PathFinding(pfs.FindClosestNodeToAgent(transform.position), pfs.FindClosestNodeToAgent(destination));

        //if (pathPortals.Count > 0) hasPath = true;    
    }

    // Update is called once per frame
    void Update()
    {
        /*if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                agent.SetDestination(hit.point);
            }
        }*/

        if (hasField)
        {

            resizeInfluenceBox(density);
            if (wait)
            {
                remainingTime -= Time.deltaTime;
                if (remainingTime <= 0.0f) wait = false;

                float vel2 = velocity.sqrMagnitude - acceleration * Time.deltaTime;
                if (vel2 <= 0.0f) vel2 = 0.0f;

                velocity = Vector3.zero;
                //velocity = vel2 * velocity.normalized;
                //transform.position += vel2 * velocity.normalized * Time.deltaTime;
                character.Move(velocity, false, false);

                return;
            }

            //Atractor force
            FieldCell2 c2;
            if(field.positionToCell(transform.position, out c2))
                if((transform.position - c2.position).magnitude < 1.0f) direction = c2.direction;
            Debug.DrawLine(transform.position, transform.position + direction * 3, Color.blue);
            Vector3 atractor = direction * maxSpeed;

            //Avoidance forces
            Vector3 agentAvoidance = agentsAvoidanceForce;
            Vector3 obstacleAvoidance = Vector3.zero;

            // Obstacle avoidance
            NavMeshHit hit;
            bool blocked = NavMesh.Raycast(transform.position, transform.position + velocity.normalized * influenceBox.size.z, out hit, NavMesh.AllAreas);
            //Debug.DrawLine(transform.position, transform.position + velocity.normalized * this.transform.Find("InfluenceBox").GetComponent<BoxCollider>().size.z, blocked ? Color.red : Color.green);
            if (blocked)
                obstacleAvoidance += CalcWallAvoidanceForce(hit.normal);

            Debug.DrawLine(transform.position, transform.position + new Vector3(obstacleAvoidance.x, 0, obstacleAvoidance.z).normalized * 3, Color.green);
            obstacleAvoidance = obstacleAvoidance.normalized * maxSpeed;

            // Agent avoidance
            agentAvoidance.y = 0.0f;
            agentAvoidance = agentAvoidance.normalized * maxSpeed;

            Vector3 steering = velocity + (atractor * 1.1f + agentAvoidance * 1.0f + obstacleAvoidance * 1.5f);
            steering = steering.normalized;

            //Repulsion forces
            if (Vector3.Dot(velocity.normalized, repulsionForce.normalized) < 0.0f && repulsionForce.magnitude > 0)
            {
                wait = true;
                remainingTime = waitTime;
            }

            if (repulsionForce.magnitude > 0.0f) velocity = Vector3.zero;
            else
            {
                float vel = velocity.magnitude + acceleration * Time.deltaTime;
                if (vel > maxSpeed) vel = maxSpeed;
                Vector3 newVelocity = vel * steering;

                if (Vector3.Angle(velocity, newVelocity) > 90) velocity = (Quaternion.AngleAxis(180 * Time.deltaTime, Vector3.up) * velocity).normalized * vel;
                else velocity = newVelocity;
            }

            transform.position += new Vector3(velocity.x, 0.0f, velocity.z) * Time.deltaTime;
            //transform.rotation = Quaternion.LookRotation(velocity.normalized);
            character.Move(new Vector3(velocity.x, 0.0f, velocity.z), false, false);

            Debug.DrawLine(transform.position, transform.position + new Vector3(velocity.x, 0.0f, velocity.z).normalized, Color.red);

            distanceToGoal = (destination - transform.position).magnitude;

            if (distanceToGoal < 2.0f)
            {
                hasField = false;
            }

            //Reset avoidance forces for next iteration
            agentsAvoidanceForce = Vector3.zero;
            repulsionForce = Vector3.zero;

        }

        if (!hasField)
        {
            fieldGenerator.GetRandomGoal(out field, out destination);
            hasField = true;
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
        if(Vector3.Dot(collision.impulse, velocity) < 0)
        {
            if (collision.collider.gameObject.tag == "Player")
                repulsionForce += collision.impulse;
            if (collision.collider.gameObject.tag == "Agent")
                repulsionForce += collision.impulse;
        }       
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
        if (distance.magnitude < 0.6f) repulsionForce += ((transform.position + velocity * Time.deltaTime) - pos).normalized * velocity.magnitude;

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
        if (density >= 0.5f) bias = -0.5f;
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
        Vector3 tanForce = Vector3.zero;
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
        if (density >= 1.0f)
        {
            influenceBox.size = new Vector3(1.25f, 1.0f, 1.5f);
            influenceBox.center = new Vector3(0.0f, 0.0f, 0.75f);
        }
        else
        {
            influenceBox.size = new Vector3(1.25f, 1.0f, 3.0f);
            influenceBox.center = new Vector3(0.0f, 0.0f, 1.5f);
        }
    }
}
