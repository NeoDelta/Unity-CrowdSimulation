﻿using System;
using System.Collections;
using System.Collections.Generic;
using Lean;
using RVO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Assertions.Comparers;
//using UnityEngine.Experimental.UIElements;
using Random = System.Random;
using Vector2 = RVO.Vector2;

public class GameMainManager : SingletonBehaviour<GameMainManager>
{
    public GameObject agentPrefab;
    public PathFindingSystem pfs;
    public float columns;
    public float files;

    [HideInInspector] public Vector2 mousePosition;

    private Plane m_hPlane = new Plane(Vector3.up, Vector3.zero);
    private Dictionary<int, GameAgent> m_agentMap = new Dictionary<int, GameAgent>();

    // Use this for initialization
    void Start()
    {
        Simulator.Instance.setTimeStep(Time.deltaTime); //0.25f default
        Simulator.Instance.setAgentDefaults(15.0f, 10, 5.0f, 5.0f, 0.5f, 1.0f, new Vector2(0.0f, 0.0f));

        // add in awake
        Simulator.Instance.processObstacles();

        for (int i = 0; i < columns; i += 1)
        {
            for (int x = 0; x < files; x += 1)
            {
                Vector2 agPos = new Vector2(transform.position.x + i*2, transform.position.z + x*2);
                int sid = Simulator.Instance.addAgent(agPos, 15.0f, 10, 5.0f, 5.0f, 0.5f, 1.0f, new Vector2(0.0f, 0.0f));
                if (sid >= 0)
                {
                    GameObject go = LeanPool.Spawn(agentPrefab, new Vector3(agPos.x(), 0, agPos.y()), Quaternion.identity);
                    GameAgent ga = go.GetComponent<GameAgent>();
                    Assert.IsNotNull(ga);
                    ga.sid = sid;
                    ga.pfs = this.pfs;
                    m_agentMap.Add(sid, ga);
                }
            }

        }
    }

    private void UpdateMousePosition()
    {
        Vector3 position = Vector3.zero;
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        float rayDistance;
        if (m_hPlane.Raycast(mouseRay, out rayDistance))
            position = mouseRay.GetPoint(rayDistance);

        mousePosition.x_ = position.x;
        mousePosition.y_ = position.z;
    }

    void DeleteAgent()
    {
        //float rangeSq = float.MaxValue;
        int agentNo = Simulator.Instance.queryNearAgent(mousePosition, 1.5f);
        if (agentNo == -1 || !m_agentMap.ContainsKey(agentNo))
            return;

        Simulator.Instance.delAgent(agentNo);
        LeanPool.Despawn(m_agentMap[agentNo].gameObject);
        m_agentMap.Remove(agentNo);
    }

    void CreatAgent()
    {
        int sid = Simulator.Instance.addAgent(mousePosition);
        if (sid >= 0)
        {
            GameObject go = LeanPool.Spawn(agentPrefab, new Vector3(mousePosition.x(), 0, mousePosition.y()), Quaternion.identity);
            GameAgent ga = go.GetComponent<GameAgent>();
            Assert.IsNotNull(ga);
            ga.sid = sid;
            ga.pfs = this.pfs;
            m_agentMap.Add(sid, ga);
        }
    }

    // Update is called once per frame
    private void Update()
    {
        /*UpdateMousePosition();
        if (Input.GetMouseButtonUp(0))
        {
            if (Input.GetKey(KeyCode.Delete))
            {
                DeleteAgent();
            }
            else
            {
                CreatAgent();
            }
        }*/

        Simulator.Instance.setTimeStep(Time.deltaTime); //0.25f default
        Simulator.Instance.doStep();
    }
}