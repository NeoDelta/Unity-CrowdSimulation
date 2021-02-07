using System;
using System.Collections;
using System.Collections.Generic;
using RVO;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;
using Random = System.Random;
using Vector2 = RVO.Vector2;

public class FieldGameAgent : MonoBehaviour
{
    [HideInInspector] public int sid = -1;
    [HideInInspector] public FlowFieldGenerator fieldGenerator;
    private FlowFieldSemi2D field;
    public ThirdPersonCharacter controller;

    /** Random number generator. */
    private Random m_random = new Random();

    private Vector3 destination;
    private bool hasField;
    // Use this for initialization
    void Start()
    {
        hasField = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (hasField)
        {
            if (sid >= 0)
            {
                //Vector2 pos = Simulator.Instance.getAgentPosition(sid);
                Vector2 vel = Simulator.Instance.getAgentPrefVelocity(sid);
                //transform.position = new Vector3(pos.x(), transform.position.y, pos.y());
                //if (Math.Abs(vel.x()) > 0.01f && Math.Abs(vel.y()) > 0.01f)
                    //transform.forward = new Vector3(vel.x(), 0, vel.y()).normalized;
            }

            /*if (!Input.GetMouseButton(1))
            {
                Simulator.Instance.setAgentPrefVelocity(sid, new Vector2(0, 0));
                return;
            }*/

            //Vector2 goalVector = GameMainManager.Instance.mousePosition - Simulator.Instance.getAgentPosition(sid);
            Vector3 direction = Vector3.zero;
            FieldCell2 c2;
            if (field.positionToCell(transform.position, out c2))
                if ((transform.position - c2.position).magnitude < 1.0f) direction = c2.direction;

            Vector2 goal = new Vector2(direction.x, direction.z);
            Vector2 goalVector = goal;
            if (RVOMath.absSq(goalVector) > 1.0f)
            {
                goalVector = RVOMath.normalize(goalVector);
            }

            Simulator.Instance.setAgentPrefVelocity(sid, goalVector);

            /* Perturb a little to avoid deadlocks due to perfect symmetry. */
            float angle = (float)m_random.NextDouble() * 2.0f * (float)Math.PI;
            float dist = (float)m_random.NextDouble() * 0.0001f;

            Simulator.Instance.setAgentPrefVelocity(sid, Simulator.Instance.getAgentPrefVelocity(sid) +
                                                         dist *
                                                         new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)));

            //Update character controller
            Vector2 newVel = Simulator.Instance.getAgentVelocity(sid);
            controller.Move(new Vector3(newVel.x(), 0.0f, newVel.y()), false, false);

            Vector2 pos = Simulator.Instance.getAgentPosition(sid);
            transform.position = new Vector3(pos.x(), transform.position.y, pos.y());

            Debug.DrawLine(transform.position, transform.position + new Vector3(newVel.x(), 0.0f, newVel.y()).normalized, Color.red);
            Debug.DrawLine(transform.position, transform.position + direction.normalized * 3, Color.blue);

            float distanceToGoal = (destination - transform.position).magnitude;

            if (distanceToGoal < 2.0f)
            {
                hasField = false;
            }
        }
        if (!hasField)
        {
            fieldGenerator.GetRandomGoal(out field, out destination);

            hasField = true;
        }
    }

    public Vector3 RandomNavmeshLocation(float radius)
    {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius;
        randomDirection += transform.position;

        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;

        if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1))
        {
            finalPosition = hit.position;
        }

        return finalPosition;
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
            xx = x1;
            yy = y1;
        }
        else if (param > 1)
        {
            xx = x2;
            yy = y2;
        }
        else
        {
            xx = x1 + param * C;
            yy = y1 + param * D;
        }
        return new Vector3(xx, transform.position.y, yy);
    }
}
