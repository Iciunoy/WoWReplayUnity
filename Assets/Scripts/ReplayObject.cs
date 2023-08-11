using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReplayObject : MonoBehaviour
{
    private CGameManager gm;
    public CGameManager.CombatantStruct PlayerCombatant;

    public int CurrentEventIndex;
    public List<CGameManager.EventLogClass> LogEvents = new List<CGameManager.EventLogClass>();
    public double timeDifference;
    public Vector3 moveToPos;
    public string UnitGUID;

    public Color DEBUGUnitColor;

    

    // Start is called before the first frame update
    void Start()
    {
        CurrentEventIndex = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Time.timeScale > 0)
        {
            if (timeDifference > 0)
            {
                timeDifference -= Time.deltaTime;
                transform.position = Vector3.Lerp(transform.position, moveToPos, Time.deltaTime);
            }
            else
            {
                
                if(CurrentEventIndex < LogEvents.Count - 1)
                {
                        //DoAction EG: Cast a spell or whatever else you want to do.
                    CurrentEventIndex++;
                        //Do Some check where if logStruct.Count == 0 then done.
                    DoMovement(CurrentEventIndex);
                }    
                
            }
        }
        
    }

    public void Init(CGameManager.CombatantStruct comStruct)
    {
        gm = GameObject.Find("GameMap").GetComponent<CGameManager>();
        PlayerCombatant = comStruct;
        UnitGUID = comStruct.PlayerGUID;
        gameObject.name = UnitGUID;
        string specName = gm.GetComponent<CGameManager>().GetClassNameFromSpecID(comStruct.Class_CurrentSpecID);
        DEBUGUnitColor = gm.GetComponent<CGameManager>().GetClassColor(specName);
        gameObject.GetComponent<Renderer>().material.color = DEBUGUnitColor;
    }

    public void Init(List<CGameManager.EventLogClass> inLogStruct, string unitGUID)
    {
        gm = GameObject.Find("GameMap").GetComponent<CGameManager>();
        LogEvents = inLogStruct;
        UnitGUID = unitGUID;
        gameObject.name = UnitGUID;
        DEBUGUnitColor = Color.black;
        gameObject.GetComponent<Renderer>().material.color = DEBUGUnitColor;
    }

    public void Init(List<CGameManager.EventLogClass> inLogStruct, CGameManager.CombatantStruct comStruct)
    {
        gm = GameObject.Find("GameMap").GetComponent<CGameManager>();
        LogEvents = inLogStruct;
        PlayerCombatant = comStruct;
        UnitGUID = comStruct.PlayerGUID;
        gameObject.name = UnitGUID;
        string specName = gm.GetComponent<CGameManager>().GetClassNameFromSpecID(comStruct.Class_CurrentSpecID);
        DEBUGUnitColor = gm.GetComponent<CGameManager>().GetClassColor(specName);
        gameObject.GetComponent<Renderer>().material.color = DEBUGUnitColor;
    }

    public void AddLogEvent(CGameManager.EventLogClass lev)
    {
        LogEvents.Add(lev);
    }

    public void AddLogEventList(List<CGameManager.EventLogClass> inLogStruct)
    {
        LogEvents = inLogStruct;
    }

    public void DoMovement(int index)
    {
        if (transform.position.x == 0)
        {
            transform.position = new Vector3(LogEvents[0].PositionX, 1, LogEvents[0].PositionY);
        }
        try
        {
            TimeSpan ts = LogEvents[index + 1].Timestamp - LogEvents[index].Timestamp;
            float updatedX = moveToPos.x;
            float updatedZ = moveToPos.z;
            if (LogEvents[index + 1].PositionX != 0)
            {
                updatedX = LogEvents[index + 1].PositionX;
            }
            if (LogEvents[index + 1].PositionY != 0)
            {
                updatedZ = LogEvents[index + 1].PositionY;
            }
            timeDifference = ts.TotalSeconds;
            moveToPos = new Vector3(updatedX, 1, updatedZ);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
