using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class SimulationController : MonoBehaviour
{
    public static SimulationController current;

    public Camera cam;
    public UnityStandardAssets.Cameras.FreeLookCam freeLookCam;
    public GameObject original;
    public PathFindingSystem pfs;
    public bool isPaused;
    public float maxSpeed;
    public float waitTime;
    public float reactionTime;
    public float densityFactor;
    public Vector3 steerWeights;


    //Visualization variables
    public bool showWaitTime;
    public bool showInfluenceBox;
    public bool showAttractorVector;
    public Text numAgents;

    private List<GameObject> agents;

    void Start()
    {
        agents = new List<GameObject>();
        current = this;
        pfs.DrawPortals();
    }

    void Update()
    {   
        if(Input.GetKeyDown(KeyCode.Space))
        {
            freeLookCam.Pause();
            //Cursor.visible = !freeLookCam.enabled;
        }
        if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 pos = hit.point;
                Quaternion ori = new Quaternion(0, 0, 0, 0);

                GameObject agent = Instantiate(original, pos, ori);
                PawnController agentController = agent.GetComponent<PawnController>();
                agentController.pfs = this.pfs;
                agentController.simCont = this;
                agentController.maxSpeed = this.maxSpeed;
                agentController.waitTime = this.waitTime;
                agentController.reactionTime = this.reactionTime;
                agentController.steerWeights= this.steerWeights;
                agentController.showWaitingRule = this.showWaitTime;
                agentController.densityFactor = this.densityFactor;

                Material mat = agent.GetComponentInChildren<MeshRenderer>().material;

                if (showInfluenceBox)
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.2f);
                else
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0);

                agents.Add(agent);

                numAgents.text = agents.Count.ToString();
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {

                if (hit.collider.gameObject.tag == "Agent")
                {
                    agents.Remove(hit.collider.gameObject);
                    Destroy(hit.collider.gameObject);
                    numAgents.text = agents.Count.ToString();
                }
            }
        }
    }

    public void ChangeMaxSpeed(string MaxSpeed)
    {
        maxSpeed = float.Parse(MaxSpeed,CultureInfo.InvariantCulture);
    }

    public void ChangeWaitTime(string WaitTime)
    {
        waitTime = float.Parse(WaitTime, CultureInfo.InvariantCulture);
    }

    public void ChangeReactionTime(string ReactionTime)
    {
        reactionTime = float.Parse(ReactionTime, CultureInfo.InvariantCulture);
    }

    public void ChangeDensityFactor(string density)
    {
        densityFactor = float.Parse(density, CultureInfo.InvariantCulture);
    }

    public void ChangeAttractorWeight(string wat)
    {
        steerWeights.x = float.Parse(wat, CultureInfo.InvariantCulture);
    }

    public void ChangeAgentAvoidanceWeight(string waa)
    {
        steerWeights.y = float.Parse(waa, CultureInfo.InvariantCulture);
    }

    public void ChangeWallAvoidanceWeight(string waw)
    {
        steerWeights.z = float.Parse(waw, CultureInfo.InvariantCulture);
    }

    public event Action onShowWaitingRuleChange;
    public void ShowWaitingRule(bool show)
    {
        showWaitTime = !showWaitTime;

        onShowWaitingRuleChange?.Invoke();
    }

    public event Action onShowInfluenceBoxChange;
    public void ShowInfluenceBox(bool show)
    {
        showInfluenceBox = !showInfluenceBox;

        onShowInfluenceBoxChange?.Invoke();
    }

    public event Action onShowAttractorVectorChange;
    public void ShowAttractorVector(bool show)
    {
        showAttractorVector = !showAttractorVector;

        onShowAttractorVectorChange?.Invoke();
    }

    public event Action onPauseChange;
    public void changePause()
    {
        isPaused = !isPaused;

        onPauseChange?.Invoke();
    }

    public event Action onShowPortalsChange;
    public void ShowPortals()
    {
        onShowPortalsChange?.Invoke();
    }

    public void DestroyAllAgents()
    {
        foreach (GameObject agent in agents)
        {
            Destroy(agent);          
        }

        numAgents.text = agents.Count.ToString();
    }

    public void UpdateAgentsValues()
    {
        foreach(GameObject agent in agents)
        {
            PawnController pc = agent.GetComponent<PawnController>();

            PawnController agentController = agent.GetComponent<PawnController>();
            agentController.maxSpeed = this.maxSpeed;
            agentController.waitTime = this.waitTime;
            agentController.reactionTime = this.reactionTime;
            agentController.densityFactor = this.densityFactor;
            agentController.steerWeights = this.steerWeights;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Agent")
        {
            agents.Remove(other.gameObject);
            Destroy(other.gameObject);
            numAgents.text = agents.Count.ToString();
        }
    }

}
