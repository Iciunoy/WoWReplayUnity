using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.PackageManager;
using UnityEngine;

public class CGameManager : MonoBehaviour
{
    Renderer m_Renderer;
    public bool isReplayLoaded { get; private set; }
    private Dictionary<string,Color> ClassColors = new Dictionary<string, UnityEngine.Color>();
    
        //GAME MAP STUFF
    /// <summary>
    /// INT = UIMAPID
    /// </summary>
    private Dictionary<int, GameMapStruct> UIMaps = new Dictionary<int, GameMapStruct>();
    public Vector3 GameMapLowerBounds { get; private set; }
    public Vector3 GameMapUpperBounds { get; private set; }
    public Vector3 GameMapNormalizedBounds { get; private set; }
    public Vector3 GameMapSize { get; private set; }
    public double elapsedGameTime { get; private set; }
    
    
    public GameObject RefReplayObject;
    public GameObject RefUIEncountersObject;

    private List<SubeventType> TypesWithDiffBaseParams = new List<SubeventType>(){SubeventType.EMOTE,
        SubeventType.ENCOUNTER_START, SubeventType.ENCOUNTER_END, SubeventType.MAP_CHANGE,
        SubeventType.WORLD_MARKER_PLACED, SubeventType.WORLD_MARKER_REMOVED,
        SubeventType.ZONE_CHANGE, SubeventType.COMBAT_LOG_VERSION, SubeventType.UNIT_DIED, SubeventType.PARTY_KILL};
    public Dictionary<string, GameObject> ActiveReplayObjects = new Dictionary<string, GameObject>();

    public Dictionary<KeyValuePair<int, int>, EncounterStruct> Encounters = new Dictionary<KeyValuePair<int, int>, EncounterStruct>();

    /// <summary>
    /// Key: KeyValuePair of EncounterID and PullNumber (from Encounters), 
    /// Value: List of LogStrings (from LoadedEncounterUnitData).
    /// </summary>
    public Dictionary<KeyValuePair<int, int>, List<string>> EncounterLogs = new Dictionary<KeyValuePair<int, int>, List<string>>();

    /// <summary>
    /// Key: SourceGUID  Value: List of SourceEventLogs.
    /// </summary>
    public Dictionary<string, List<EventLogClass>> LoadedEncounterUnitData = new Dictionary<string, List<EventLogClass>>();

    public TextAsset logFile;

    public int DEBUG_LOG_LINES_READ;

    //unused so far
    //private float encounterReplayTimeElapsed;

    private void Awake()
    {
        //ASSIGN CLASS COLORS
        Color colordk = new Color(0.77f,  0.12f,    0.23f);
        Color colordh = new Color(0.64f, 0.19f, 0.79f);
        Color colordruid = new Color(1.00f, 0.49f, 0.04f);
        Color colorevoker = new Color(0.20f, 0.58f, 0.50f);
        Color colorhunter = new Color(0.67f, 0.83f, 0.45f);
        Color colormage = new Color(0.25f, 0.78f, 0.92f);
        Color colormonk = new Color(0.00f, 1.00f, 0.60f);
        Color colorpally = new Color(0.96f, 0.55f, 0.73f);
        Color colorpriest = new Color(1f, 1f, 1f);
        Color colorrogue = new Color(1.00f, 0.96f, 0.41f);
        Color colorshaman = new Color(0.00f, 0.44f, 0.87f);
        Color colorwarlock = new Color(0.53f, 0.53f, 0.93f);
        Color colorwarrior = new Color(0.78f, 0.61f, 0.43f);
        ClassColors.Add("deathknight", colordk);
        ClassColors.Add("demonhunter", colordh);
        ClassColors.Add("druid", colordruid);
        ClassColors.Add("evoker", colorevoker);
        ClassColors.Add("hunter", colorhunter);
        ClassColors.Add("mage", colormage);
        ClassColors.Add("monk", colormonk);
        ClassColors.Add("paladin", colorpally);
        ClassColors.Add("priest", colorpriest);
        ClassColors.Add("rogue", colorrogue);
        ClassColors.Add("shaman", colorshaman);
        ClassColors.Add("warlock", colorwarlock);
        ClassColors.Add("warrior", colorwarrior);
    }

    // Start is called before the first frame update
    void Start()
    {
        GameMapLowerBounds = Vector3.zero;
        GameMapUpperBounds = Vector3.one;
        GameMapNormalizedBounds = Vector3.zero;
        GameMapSize = Vector3.one;
        elapsedGameTime = 0;
        isReplayLoaded = false;
        m_Renderer = GetComponent<Renderer>();
        //encounterReplayTimeElapsed = 0;
        //ParseLogForEncounters();
    }

    // Update is called once per frame
    void Update()
    {
        if (isReplayLoaded && Time.timeScale > 0)
        {
            elapsedGameTime += Time.deltaTime;
        }
        
    }




    //Parse through the log file and split all the log events by their encounter. Creates encounter keys.
    public void ParseLogForEncounters()
    {   
            //Create list of all the log events delimited by lines
        List<string> LogEvents = new List<string>(logFile.text.Split('\n'));
            //Create list of all the encounter events, to select logs by encounter
        List<string> EncounterEvents = new List<string>();
        KeyValuePair<int, int> EncounterKey = new KeyValuePair<int, int>(-1, 1);
        
        bool EncounterStarted = false;
        DateTime NewEncounterStartTime = DateTime.MinValue;
        int NewEncounterID = -1;
        string NewEncounterName = " ";
        int NewEncounterDiffID = -1;
        int NewEncounterGroupSize = -1;
        int NewEncounterInstanceID = -1;
        int NewEncounterPull = -1;
        int NewEncounterTime = -1;
        float NewEncounterPercent = 100;
        Debug.Log("STARTING ENCOUNTER PARSE");
            //Begin parse for all encounter data in log file
        for (int lineNum = 0; lineNum < LogEvents.Count; lineNum++)
        {
            
            try
            {
                string[] events = GetLogEventsSplit(LogEvents[lineNum]);
                
                    //ADD POSSIBLE MAPS TO THE UIMAPS DICTIONARY
                if (events[0] == "MAP_CHANGE" && !UIMaps.ContainsKey(int.Parse(events[1])))
                {
                    //Debug.Log(events);
                    try
                    {
                        GameMapStruct gmap = new GameMapStruct();
                        gmap.UIMapID = int.Parse(events[1]);
                        gmap.UIMapName = events[2];
                        gmap.ZUpper = float.Parse(events[3]);
                        gmap.ZLower = float.Parse(events[4]);
                        gmap.XUpper = float.Parse(events[5]);
                        gmap.XLower = float.Parse(events[6]);
                        UIMaps.Add(gmap.UIMapID, gmap);
                    }
                    catch (Exception e)
                    {
                        Debug.Log("MAP_CHANGE READ ERROR" + " at line number " + lineNum + ":" + '\n' + e.Message + '\n' + events);
                    }
                    
                }

                    //Inside an encounter?
                if (EncounterStarted)
                {
                        //Add the event to the list
                    EncounterEvents.Add(LogEvents[lineNum]);
                        //Has the encounter ended?
                    if (events[0] == "ENCOUNTER_END")
                    {
                            //End encounter parse.
                        EncounterStarted = false;
                        if (events[5] == "1")
                        {
                            NewEncounterPercent = 0;
                        }
                        try
                        {
                            NewEncounterTime = int.Parse(events[6]);
                        }
                        catch (Exception e)
                        {
                            Debug.Log(e.Message);
                        }
                        EncounterStruct NewEncounter = LoadEncounterStruct(NewEncounterStartTime, 
                                                                        NewEncounterID,
                                                                        NewEncounterName,
                                                                        NewEncounterDiffID,
                                                                        NewEncounterGroupSize,
                                                                        NewEncounterInstanceID,
                                                                        NewEncounterPull,
                                                                        NewEncounterTime,
                                                                        NewEncounterPercent);
                        EncounterLogs.Add(EncounterKey, EncounterEvents);
                        Encounters.Add(EncounterKey, NewEncounter);
                        if (NewEncounterPull <= 1)
                        {
                            RefUIEncountersObject.GetComponent<UIEncounters>().CreateEncounterPanel(NewEncounter);
                        }
                        RefUIEncountersObject.GetComponent<UIEncounters>().CreateEncounterButton(NewEncounter);
                    }
                }
                else
                {
                        //New encounter starting?
                    if (events[0] == "ENCOUNTER_START")
                    {
                            //Begin encounter parse
                        EncounterStarted = true;
                        try
                        {
                            NewEncounterStartTime = GetLogTimestamp(LogEvents[lineNum]);
                            NewEncounterID = int.Parse(events[1]);
                            NewEncounterName = events[2];
                            NewEncounterDiffID = int.Parse(events[3]);
                            NewEncounterGroupSize = int.Parse(events[4]);
                            NewEncounterInstanceID = int.Parse(events[5]);
                        }
                        catch (FormatException e)
                        {
                            Debug.Log(e.Message);
                        }
                        //Sets EncounterKey to the latest key and adds the key to Encounters list.
                        EncounterKey = GetNewEncounterKey(NewEncounterID, 1);
                        NewEncounterPull = EncounterKey.Value;
                        //Encounters.Add(EncounterKey);
                        //Debug.Log("ENCOUNTER FOUND: " + EncounterKey);
                        EncounterEvents.Clear();
                        EncounterEvents.Add(LogEvents[lineNum]);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("Error occured in log " + logFile.name + " at line number " + lineNum + ": " + '\n' + LogEvents[lineNum] + '\n' + e);
            }


        }
        Debug.Log("FINISHED ENCOUNTER PARSE. ENCOUNTERS:");
    }

    public void LoadEncounterUnitData(EncounterStruct enStruct)
    {
        KeyValuePair<int, int> k = new KeyValuePair<int, int>(enStruct.EncounterID, enStruct.EncounterPull);
        int lineNum = 0;
        string lineVal = " ";
        foreach (string ev in EncounterLogs[k])
        {
            
            lineNum++;
            lineVal = ev;
            DEBUG_LOG_LINES_READ = lineNum;
            if (ev.Contains("0000000000000000") || ev.Contains("COMBATANT_INFO"))
            {
                //SOMETHING IS HAPPENING IN HERE, MUST FIX
                //Create event struct for log event line
                EventLogClass els = new EventLogClass();
                AssignToNewEventStruct(els, ev);
                //Debug.Log(els.SourceGUID);
                if (ev.Contains("0000000000000000"))
                {
                    if (els.SourceGUID != null && ActiveReplayObjects.ContainsKey(els.SourceGUID))
                    {
                        //Debug.Log("SOURCEGUID: " + els.SourceGUID);
                        ActiveReplayObjects[els.SourceGUID].GetComponent<ReplayObject>().LogEvents.Add(els);
                    }
                    //Debug.Log("SOURCEGUID: " + els.SourceGUID);
                    //ActiveReplayObjects[els.SourceGUID].GetComponent<ReplayObject>().LogEvents.Add(els);
                    //Debug.Log("DO YOU GET HERE?");
                }
                
                
                DEBUG_LOG_LINES_READ = lineNum;

            }



        }

        //Debug.Log("HERE ARE THE ACTIVE REPLAY OBJECTS: ");
        foreach (KeyValuePair<string,GameObject> kvp in ActiveReplayObjects)
        {
            //Debug.Log("REPLAY OBJECT: " + kvp.Value.name + "(" + kvp.Key + ")" + '\n' + "NUMBER OF LOG EVENTS: " + kvp.Value.GetComponent<ReplayObject>().LogEvents.Count);
        }

        isReplayLoaded = true;
        //try
        //{

        //}
        //catch (Exception e)
        //{
        //Debug.Log("Error when loading unit data: " + '\n' + "Line Number: " + lineNum + '\n' + "String: " + lineVal + '\n' + e.Message);
        //}


    }

    //Call this on loading encounter to load the game map object
    public void LoadGameMapToScale(EncounterStruct enStruct)
    {
        Time.timeScale = 0;
        GameMapStruct mapStruct;
        if (UIMaps.ContainsKey(GetUIMapIDFromEncounterID(enStruct.EncounterID)))
        {
            try
            {
                mapStruct = UIMaps[GetUIMapIDFromEncounterID(enStruct.EncounterID)];
                m_Renderer.material.SetTexture("_MainTex", Resources.Load<Texture>(mapStruct.UIMapID.ToString()));
                GameMapNormalizedBounds = new Vector3((mapStruct.XUpper - mapStruct.XLower), 0, (mapStruct.ZUpper - mapStruct.ZLower));
                GameMapLowerBounds = new Vector3(mapStruct.XLower, 0f, mapStruct.ZLower);
                GameMapSize = new Vector3(GameMapNormalizedBounds.x * 0.1f, 1, GameMapNormalizedBounds.z * 0.1f);
                gameObject.transform.localScale = GameMapSize;
                gameObject.transform.localPosition = new Vector3(GameMapNormalizedBounds.x * 0.5f, 0, GameMapNormalizedBounds.z * -0.5f);

            }
            catch (Exception e)
            {
                Debug.Log("ERROR LOADING GAME MAP FOR ENCOUNTER - " + enStruct.EncounterID + ": " + e.Message);
            }
        }
        else
        {
            Debug.Log("ERROR: Map not found for encounter " + enStruct.EncounterID);
        }
        LoadEncounterUnitData(enStruct);
        
    }


    public int GetUIMapIDFromEncounterID(int enID)
    {
        switch (enID)
        {
            //Kazzara
            case 2688:
                return 2166;
            //Amalg
            case 2687:
                return 2167;
            //Experiments
            case 2693:
                return 2166;
            //Assault
            case 2682:
                return 2168;
            //Rashok
            case 2680:
                return 2166;
            //Zskarn
            case 2689:
                return 2166;
            //Magmorax
            case 2683:
                return 2166;
            //Echo of Nelth
            case 2684:
                return 2169;
            //Scalecommander Sark
            case 2685:
                return 2170;

            default:
                Debug.Log("ERROR INVALID ENCOUNTER ID: " + enID);
                return -1;
        }
    }

    public string GetClassNameFromSpecID(int specID)
    {
        switch (specID)
        {
            case 250:
            case 251:
            case 252:
                return "DeathKnight";
            case 577:
            case 581:
                return "DemonHunter";
            case 102:
            case 103:
            case 104:
            case 105:
                return "Druid";
            case 1467:
            case 1468:
            case 1473:
                return "Evoker";
            case 253:
            case 254:
            case 255:
                return "Hunter";
            case 62:
            case 63:
            case 64:
                return "Mage";
            case 268:
            case 270:
            case 269:
                return "Monk";
            case 65:
            case 66:
            case 70:
                return "Paladin";
            case 256:
            case 257:
            case 258:
                return "Priest";
            case 259:
            case 260:
            case 261:
                return "Rogue";
            case 262:
            case 263:
            case 264:
                return "Shaman";
            case 265:
            case 266:
            case 267:
                return "Warlock";
            case 71:
            case 72:
            case 73:
                return "Warrior";
            default:
                string r = "INVALID SPEC ID: " + specID.ToString();
                return r;
        }
    }

    /// <summary>
    /// Takes an event line from log, returns the timestamp of event.
    /// </summary>
    /// <param name="log"></param>
    /// <returns></returns>
    public Color GetClassColor(string className)
    {
        if (ClassColors.ContainsKey(className.ToLower()))
        {
            return ClassColors[className.ToLower()];
        }
        else
        {
            return Color.black;
        }

    }
    public SubeventType StringToSubeventType(string str)
    {
        SubeventType returnType = SubeventType.UNKNOWN_TYPE_ERROR;
        try
        {
            returnType = (SubeventType)System.Enum.Parse(typeof(SubeventType), str, true);
        }
        catch (ArgumentException e)
        {
            Debug.Log(e.GetType().Name + ": " + e.Message);
        }
        return returnType;
    }
    public DateTime GetLogTimestamp(string log)
    {
        DateTime returnTime = DateTime.MinValue;
        string sTime = log.Split("  ")[0].Split(" ")[1];
        try
        {
            returnTime = System.DateTime.ParseExact(sTime, "HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (FormatException e)
        {
            Debug.Log(e.Message + "(" + sTime + ")");
        }
        return returnTime;
    }
    public string[] GetLogEventsSplit(string log)
    {
        string logEvent = log.Split("  ")[1];
        
        //this regular expression splits string on the separator character NOT inside double quotes. 
        //separatorChar can be any character like comma or semicolon etc. 
        //it also allows single quotes inside the string value: e.g. "Mike's Kitchen","Jane's Room"
        Regex regx = new Regex(',' + "(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
        return regx.Split(logEvent);
    }
    public string GetSourceGUID(string logEvent)
    {
        return logEvent.Split(',')[1];
    }

    //FINISH THIS LATER, DOESNT MATTER NOW
    public EventLogClass EventZoneChange(int lineNum, string eventString)
    {
        EventLogClass zoneEvent = new EventLogClass();
        zoneEvent.Timestamp = GetLogTimestamp(eventString);

        return zoneEvent;
    }

    public KeyValuePair<int, int> GetNewEncounterKey(int eID, int pull)
    {
        KeyValuePair<int, int> e = new KeyValuePair<int, int>(eID, pull);
        if (Encounters.ContainsKey(e))
        {
            e = GetNewEncounterKey(eID, pull + 1);
        }
        return e;

    }

    //WORK IN PROGRESS AFTER DEBUG FINISHED
    //public EventLogClass CreateNewEventStruct(string evLog)
    //{
    //    EventLogClass newStruct = new EventLogClass();
    //    DateTime ts = GetLogTimestamp(evLog);
    //    string[] evParams = GetLogEventsSplit(evLog);
    //    SubeventType t = StringToSubeventType(evParams[0]);
    //        //IS THIS A SPICY EVENT TYPE, OR A NORMAL ONE?
    //    if (!TypesWithDiffBaseParams.Contains(t))
    //    {
    //        //if (evParams[0] == "COMBATANT_INFO")
    //        //{
    //        //    Debug.Log("IT SHOULD MAKE AN OBJECT HERE FOR " + evParams[2]);
    //        //    GameObject newReplayObject = Instantiate(RefReplayObject, Vector3.zero, Quaternion.identity);
    //        //    ActiveReplayObjects.Add(evParams[1], newReplayObject);
    //        //}

    //        if (!ActiveReplayObjects.ContainsKey(evParams[1]))
    //        {
                
    //            if (evParams[0].Contains("COMBATANT_INFO"))
    //            {
    //                try
    //                {
    //                    GameObject newReplayObject = Instantiate(RefReplayObject, Vector3.zero, Quaternion.identity);
    //                    ActiveReplayObjects.Add(evParams[1], newReplayObject);
    //                    CombatantStruct newComStruct = LoadCombatantStruct(evLog, evParams[1], int.Parse(evParams[2]), int.Parse(evParams[3]), int.Parse(evParams[4]), int.Parse(evParams[5]), int.Parse(evParams[6]), int.Parse(evParams[7]), int.Parse(evParams[8]), int.Parse(evParams[9]), int.Parse(evParams[10]), int.Parse(evParams[11]), int.Parse(evParams[12]), int.Parse(evParams[13]), int.Parse(evParams[14]), int.Parse(evParams[15]), int.Parse(evParams[16]), int.Parse(evParams[17]), int.Parse(evParams[18]), int.Parse(evParams[19]), int.Parse(evParams[20]), int.Parse(evParams[21]), int.Parse(evParams[22]), int.Parse(evParams[23]), int.Parse(evParams[24]));
    //                    newReplayObject.GetComponent<ReplayObject>().Init(newComStruct);
                        
    //                }
    //                catch (Exception e)
    //                {
    //                    Debug.Log("TRYING TO LOAD COMBATANT INFO INTO STRUCT" + '\n' + e);
    //                }
                    
                    
    //                //newReplayObject.GetComponent<ReplayObject>().Init()
    //            }
    //        }


    //        //  //ASSIGN BASE PARAMS
    //        //AssignBaseParameters(newStruct, ts, t, evParams[1], evParams[2], evParams[3], evParams[4], evParams[5], evParams[6], evParams[7], evParams[8]);
    //        //  //ASSIGN PREFIX PARAMS
    //        //switch (t)
    //        //{
    //        //      //ENVIRONMENTAL PREFIX
    //        //    case SubeventType.ENVIRONMENTAL_DAMAGE:
    //        //        break;

    //        //      //SWING PREFIX
    //        //    case SubeventType.SWING_DAMAGE:
    //        //        break;
    //        //    case SubeventType.SWING_MISSED:
    //        //        break;

    //        //      //RANGE, SPELL, SPELL_PERIODIC, SPELL_BUILDING PREFIXES
    //        //    case SubeventType.RANGE_DAMAGE:
    //        //        break;
    //        //    case SubeventType.RANGE_MISSED:
    //        //        break;
    //        //    case SubeventType.SPELL_CAST_START:
    //        //        break;
    //        //    case SubeventType.SPELL_CAST_SUCCESS:
    //        //        break;
    //        //    case SubeventType.SPELL_CAST_FAILED:
    //        //        break;
    //        //    case SubeventType.SPELL_MISSED:
    //        //        break;
    //        //    case SubeventType.SPELL_DAMAGE:
    //        //        break;
    //        //    case SubeventType.SPELL_ABSORBED:
    //        //        break;
    //        //    case SubeventType.SPELL_HEAL:
    //        //        break;
    //        //    case SubeventType.SPELL_HEAL_ABSORBED:
    //        //        break;
    //        //    case SubeventType.SPELL_ENERGIZE:
    //        //        break;
    //        //    case SubeventType.SPELL_DRAIN:
    //        //        break;
    //        //    case SubeventType.SPELL_LEECH:
    //        //        break;
    //        //    case SubeventType.SPELL_SUMMON:
    //        //        break;
    //        //    case SubeventType.SPELL_RESURRECT:
    //        //        break;
    //        //    case SubeventType.SPELL_CREATE:
    //        //        break;
    //        //    case SubeventType.SPELL_INSTAKILL:
    //        //        break;
    //        //    case SubeventType.SPELL_INTERRUPT:
    //        //        break;
    //        //    case SubeventType.SPELL_EXTRA_ATTACKS:
    //        //        break;
    //        //    case SubeventType.SPELL_DURABILITY_DAMAGE:
    //        //        break;
    //        //    case SubeventType.SPELL_DURABILITY_DAMAGE_ALL:
    //        //        break;
    //        //    case SubeventType.SPELL_AURA_APPLIED:
    //        //        break;
    //        //    case SubeventType.SPELL_AURA_APPLIED_DOSE:
    //        //        break;
    //        //    case SubeventType.SPELL_AURA_REMOVED:
    //        //        break;
    //        //    case SubeventType.SPELL_AURA_REMOVED_DOSE:
    //        //        break;
    //        //    case SubeventType.SPELL_AURA_BROKEN:
    //        //        break;
    //        //    case SubeventType.SPELL_AURA_BROKEN_SPELL:
    //        //        break;
    //        //    case SubeventType.SPELL_AURA_REFRESH:
    //        //        break;
    //        //    case SubeventType.SPELL_DISPEL:
    //        //        break;
    //        //    case SubeventType.SPELL_DISPEL_FAILED:
    //        //        break;
    //        //    case SubeventType.SPELL_STOLEN:
    //        //        break;
    //        //    case SubeventType.SPELL_PERIODIC_DAMAGE:
    //        //        break;
    //        //    case SubeventType.SPELL_PERIODIC_MISSED:
    //        //        break;
    //        //    case SubeventType.SPELL_PERIODIC_HEAL:
    //        //        break;
    //        //    case SubeventType.SPELL_PERIODIC_ENERGIZE:
    //        //        break;
    //        //    case SubeventType.SPELL_PERIODIC_DRAIN:
    //        //        break;
    //        //    case SubeventType.SPELL_PERIODIC_LEECH:
    //        //        break;
    //        //    case SubeventType.SPELL_EMPOWER_START:
    //        //        break;
    //        //    case SubeventType.SPELL_EMPOWER_END:
    //        //        break;
    //        //    case SubeventType.SPELL_EMPOWER_INTERRUPT:
    //        //        break;
    //        //    case SubeventType.SPELL_BUILDING_DAMAGE:
    //        //        break;
    //        //    case SubeventType.SPELL_BUILDING_HEAL:
    //        //        break;

    //        //      // SPECIAL
    //        //    case SubeventType.DAMAGE_SHIELD:
    //        //        break;
    //        //    case SubeventType.DAMAGE_SHIELD_MISSED:
    //        //        break;
    //        //    case SubeventType.DAMAGE_SPLIT:
    //        //        break;
    //        //    case SubeventType.ENCHANT_APPLIED:
    //        //        break;
    //        //    case SubeventType.ENCHANT_REMOVED:
    //        //        break;
    //        //    case SubeventType.UNIT_DESTROYED:
    //        //        break;
    //        //    case SubeventType.UNIT_DISSIPATES:
    //        //        break;
    //        //    case SubeventType.COMBATANT_INFO:
    //        //        //CreateReplayObject(RefReplayObject);
    //        //        break;
    //        //    default:
    //        //        //Debug.Log("UNKNOWN EVENT TYPE: " + evParams);
    //        //        break;
    //        //}
    //        //ASSIGN ADV PARAMS


    //        //THIS IS TEMPORARY AND DOES NOT INCLUDE EVENTS FOR PETS OR MINIONS
    //        if (evParams.Contains<string>("0000000000000000"))
    //        {
    //            AssignBaseParameters(evStruct, ts, t, evParams[1], evParams[2], evParams[3], evParams[4], evParams[5], evParams[6], evParams[7], evParams[8]);
    //            int indOfOwnerGUID = -1;
    //            try
    //            {

    //                indOfOwnerGUID = Array.IndexOf<string>(evParams, "0000000000000000");
    //                if (indOfOwnerGUID > 5)
    //                {
    //                    //Debug.Log("GOT A POSITION");
    //                    float offsetZ = float.Parse(evParams[indOfOwnerGUID + 11]) - GameMapLowerBounds.x;
    //                    float offsetX = float.Parse(evParams[indOfOwnerGUID + 12]) - GameMapLowerBounds.z;
    //                    evStruct.PositionY = offsetX;
    //                    evStruct.PositionX = offsetZ;
    //                }
    //            }
    //            catch (Exception e)
    //            {
    //                Debug.Log("ISSUE TRYING TO FIND POSITION. INDEX VALUE: " + indOfOwnerGUID + '\n' + "LOG LINE: " + evLog + '\n' + e);
    //            }
    //        }
            

    //    }
    //    return newStruct;

    //}

    public void AssignToNewEventStruct(EventLogClass evStruct, string evLog)
    {
        DateTime ts = GetLogTimestamp(evLog);
        string[] evParams = GetLogEventsSplit(evLog);
        SubeventType t = StringToSubeventType(evParams[0]);
        //IS THIS A SPICY EVENT TYPE, OR A NORMAL ONE?
        if (!TypesWithDiffBaseParams.Contains(t))
        {
            //if (evParams[0] == "COMBATANT_INFO")
            //{
            //    Debug.Log("IT SHOULD MAKE AN OBJECT HERE FOR " + evParams[2]);
            //    GameObject newReplayObject = Instantiate(RefReplayObject, Vector3.zero, Quaternion.identity);
            //    ActiveReplayObjects.Add(evParams[1], newReplayObject);
            //}

            if (!ActiveReplayObjects.ContainsKey(evParams[1]))
            {

                if (evParams[0].Contains("COMBATANT_INFO"))
                {
                    try
                    {
                        GameObject newReplayObject = Instantiate(RefReplayObject, Vector3.zero, Quaternion.identity);
                        ActiveReplayObjects.Add(evParams[1], newReplayObject);
                        CombatantStruct newComStruct = LoadCombatantStruct(evLog, evParams[1], int.Parse(evParams[2]), int.Parse(evParams[3]), int.Parse(evParams[4]), int.Parse(evParams[5]), int.Parse(evParams[6]), int.Parse(evParams[7]), int.Parse(evParams[8]), int.Parse(evParams[9]), int.Parse(evParams[10]), int.Parse(evParams[11]), int.Parse(evParams[12]), int.Parse(evParams[13]), int.Parse(evParams[14]), int.Parse(evParams[15]), int.Parse(evParams[16]), int.Parse(evParams[17]), int.Parse(evParams[18]), int.Parse(evParams[19]), int.Parse(evParams[20]), int.Parse(evParams[21]), int.Parse(evParams[22]), int.Parse(evParams[23]), int.Parse(evParams[24]));
                        newReplayObject.GetComponent<ReplayObject>().Init(newComStruct);

                    }
                    catch (Exception e)
                    {
                        Debug.Log("TRYING TO LOAD COMBATANT INFO INTO STRUCT" + '\n' + e);
                    }


                    //newReplayObject.GetComponent<ReplayObject>().Init()
                }
            }


            //  //ASSIGN BASE PARAMS
            //AssignBaseParameters(newStruct, ts, t, evParams[1], evParams[2], evParams[3], evParams[4], evParams[5], evParams[6], evParams[7], evParams[8]);
            //  //ASSIGN PREFIX PARAMS
            //switch (t)
            //{
            //      //ENVIRONMENTAL PREFIX
            //    case SubeventType.ENVIRONMENTAL_DAMAGE:
            //        break;

            //      //SWING PREFIX
            //    case SubeventType.SWING_DAMAGE:
            //        break;
            //    case SubeventType.SWING_MISSED:
            //        break;

            //      //RANGE, SPELL, SPELL_PERIODIC, SPELL_BUILDING PREFIXES
            //    case SubeventType.RANGE_DAMAGE:
            //        break;
            //    case SubeventType.RANGE_MISSED:
            //        break;
            //    case SubeventType.SPELL_CAST_START:
            //        break;
            //    case SubeventType.SPELL_CAST_SUCCESS:
            //        break;
            //    case SubeventType.SPELL_CAST_FAILED:
            //        break;
            //    case SubeventType.SPELL_MISSED:
            //        break;
            //    case SubeventType.SPELL_DAMAGE:
            //        break;
            //    case SubeventType.SPELL_ABSORBED:
            //        break;
            //    case SubeventType.SPELL_HEAL:
            //        break;
            //    case SubeventType.SPELL_HEAL_ABSORBED:
            //        break;
            //    case SubeventType.SPELL_ENERGIZE:
            //        break;
            //    case SubeventType.SPELL_DRAIN:
            //        break;
            //    case SubeventType.SPELL_LEECH:
            //        break;
            //    case SubeventType.SPELL_SUMMON:
            //        break;
            //    case SubeventType.SPELL_RESURRECT:
            //        break;
            //    case SubeventType.SPELL_CREATE:
            //        break;
            //    case SubeventType.SPELL_INSTAKILL:
            //        break;
            //    case SubeventType.SPELL_INTERRUPT:
            //        break;
            //    case SubeventType.SPELL_EXTRA_ATTACKS:
            //        break;
            //    case SubeventType.SPELL_DURABILITY_DAMAGE:
            //        break;
            //    case SubeventType.SPELL_DURABILITY_DAMAGE_ALL:
            //        break;
            //    case SubeventType.SPELL_AURA_APPLIED:
            //        break;
            //    case SubeventType.SPELL_AURA_APPLIED_DOSE:
            //        break;
            //    case SubeventType.SPELL_AURA_REMOVED:
            //        break;
            //    case SubeventType.SPELL_AURA_REMOVED_DOSE:
            //        break;
            //    case SubeventType.SPELL_AURA_BROKEN:
            //        break;
            //    case SubeventType.SPELL_AURA_BROKEN_SPELL:
            //        break;
            //    case SubeventType.SPELL_AURA_REFRESH:
            //        break;
            //    case SubeventType.SPELL_DISPEL:
            //        break;
            //    case SubeventType.SPELL_DISPEL_FAILED:
            //        break;
            //    case SubeventType.SPELL_STOLEN:
            //        break;
            //    case SubeventType.SPELL_PERIODIC_DAMAGE:
            //        break;
            //    case SubeventType.SPELL_PERIODIC_MISSED:
            //        break;
            //    case SubeventType.SPELL_PERIODIC_HEAL:
            //        break;
            //    case SubeventType.SPELL_PERIODIC_ENERGIZE:
            //        break;
            //    case SubeventType.SPELL_PERIODIC_DRAIN:
            //        break;
            //    case SubeventType.SPELL_PERIODIC_LEECH:
            //        break;
            //    case SubeventType.SPELL_EMPOWER_START:
            //        break;
            //    case SubeventType.SPELL_EMPOWER_END:
            //        break;
            //    case SubeventType.SPELL_EMPOWER_INTERRUPT:
            //        break;
            //    case SubeventType.SPELL_BUILDING_DAMAGE:
            //        break;
            //    case SubeventType.SPELL_BUILDING_HEAL:
            //        break;

            //      // SPECIAL
            //    case SubeventType.DAMAGE_SHIELD:
            //        break;
            //    case SubeventType.DAMAGE_SHIELD_MISSED:
            //        break;
            //    case SubeventType.DAMAGE_SPLIT:
            //        break;
            //    case SubeventType.ENCHANT_APPLIED:
            //        break;
            //    case SubeventType.ENCHANT_REMOVED:
            //        break;
            //    case SubeventType.UNIT_DESTROYED:
            //        break;
            //    case SubeventType.UNIT_DISSIPATES:
            //        break;
            //    case SubeventType.COMBATANT_INFO:
            //        //CreateReplayObject(RefReplayObject);
            //        break;
            //    default:
            //        //Debug.Log("UNKNOWN EVENT TYPE: " + evParams);
            //        break;
            //}
            //ASSIGN ADV PARAMS


            //THIS IS TEMPORARY AND DOES NOT INCLUDE EVENTS FOR PETS OR MINIONS
            if (evParams.Contains<string>("0000000000000000"))
            {
                AssignBaseParameters(evStruct, ts, t, evParams[1], evParams[2], evParams[3], evParams[4], evParams[5], evParams[6], evParams[7], evParams[8]);
                int indOfOwnerGUID = -1;
                try
                {

                    indOfOwnerGUID = Array.IndexOf<string>(evParams, "0000000000000000");
                    if (indOfOwnerGUID > 5)
                    {
                        //Debug.Log("GOT A POSITION");
                        float offsetZ = float.Parse(evParams[indOfOwnerGUID + 11]) - GameMapLowerBounds.z;
                        float offsetX = float.Parse(evParams[indOfOwnerGUID + 12]) - GameMapLowerBounds.x;
                        evStruct.PositionY = offsetZ * -1;
                        evStruct.PositionX = offsetX;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("ISSUE TRYING TO FIND POSITION. INDEX VALUE: " + indOfOwnerGUID + '\n' + "LOG LINE: " + evLog + '\n' + e);
                }
            }


        }
    }

    void AssignBaseParameters(EventLogClass st, DateTime timestamp, SubeventType subevent, string sourceGUID, string sourceName, string sourceFlags, string sourceRaidFlags, string destGUID, string destName, string destFlags, string destRaidFlags)
    {
        st.Timestamp= timestamp;
        st.Subevent= subevent;
        st.SourceGUID= sourceGUID;
        st.SourceName= sourceName;
        st.SourceFlags= sourceFlags;
        st.SourceRaidFlags = sourceRaidFlags;
        st.DestGUID= destGUID;
        st.DestName= destName;
        st.DestFlags= destFlags;
        st.DestRaidFlags= destRaidFlags;
    }

    void CreateReplayObject(GameObject prefab)
    {
        //GameObject newReplayObject = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        //ActiveReplayObjects.Add(newReplayObject);
        //STILL NEED TO CALL THE INIT ON OBJECTS AFTER THIS
    }
    
    void DEBUG_SeeArgsOnObjects()
    {

    }

    /// <summary>
    /// STRUCTS AND ENUMS
    /// </summary>
    
    
    public class EventLogClass
    {
        //BASE PARAMETERS
        public DateTime Timestamp;
        public SubeventType Subevent;
        public string SourceGUID;
        public string SourceName;
        public string SourceFlags;
        public string SourceRaidFlags;
        public string DestGUID;
        public string DestName;
        public string DestFlags;
        public string DestRaidFlags;

        // PREFIX PARAMETERS
        public string SpellID;
        public string SpellName;
        public string SpellSchool;
        public string EnvironmentalType;

        // ADVANCED PARAMETERS
        public string InfoGUID;
        public string OwnerGUID;
        public string CurrentHP;
        public string MaxHP;
        public string AttackPower;
        public string SpellPower;
        public string Armor;
        public string Absorb;
        public string PowerType;
        public string CurrentPower;
        public string MaxPower;
        public string PowerCost;
        public float PositionX;
        public float PositionY;
        public int UIMapID;
        public float Facing;
        public int Level;

        // SUFFIX PARAMETERS
        public float Amount;
        public float Overkill;
        public string School;
        public string Resisted;
        public string Blocked;
        public string Absorbed;
        public bool Critical;
        public bool Glancing;
        public bool Crushing;
        public bool IsOffHand;
        public string ExtraGUID;
        public string ExtraName;
        public string ExtraFlags;
        public string ExtraRaidFlags;
        public float ExtraSpellID;
        public string ExtraSpellName;
        public string ExtraSchool;
        public float AbsorbedAmount;
        public float TotalAmount;
        public string MissType;
        public float AmountMissed;
        public float Overhealing;
        public string OverEnergize;
        public string AuraType;
        public string FailedType;
        public string UnconsciousOnDeath;

        //SPECIAL PARAMETERS
        public int EncounterID;
        public string EncounterName;
        public int DifficultyID;
        public int GroupSize;
        public int InstanceID;
        public int EncounterSuccess;
        public int EncounterTime;
        public string UIMapName;
        public string ZoneName;
        public float BoundXLower;
        public float BoundXUpper;
        public float BoundYLower;
        public float BoundYUpper;

        public EventLogClass()
        {

        }

        public EventLogClass(DateTime timestamp, SubeventType subevent, string sourceGUID, string sourceName, string sourceFlags, string sourceRaidFlags, string destGUID, string destName, string destFlags, string destRaidFlags, string spellID, string spellName, string spellSchool, string environmentalType, string infoGUID, string ownerGUID, string currentHP, string maxHP, string attackPower, string spellPower, string armor, string absorb, string powerType, string currentPower, string maxPower, string powerCost, float positionX, float positionY, int uIMapID, float facing, int level, float amount, float overkill, string school, string resisted, string blocked, string absorbed, bool critical, bool glancing, bool crushing, bool isOffHand, string extraGUID, string extraName, string extraFlags, string extraRaidFlags, float extraSpellID, string extraSpellName, string extraSchool, float absorbedAmount, float totalAmount, string missType, float amountMissed, float overhealing, string overEnergize, string auraType, string failedType, string unconsciousOnDeath, int encounterID, string encounterName, int difficultyID, int groupSize, int instanceID, int encounterSuccess, int encounterTime, string uIMapName, string zoneName, float boundXLower, float boundXUpper, float boundYLower, float boundYUpper)
        {
            Timestamp = timestamp;
            Subevent = subevent;
            SourceGUID = sourceGUID;
            SourceName = sourceName;
            SourceFlags = sourceFlags;
            SourceRaidFlags = sourceRaidFlags;
            DestGUID = destGUID;
            DestName = destName;
            DestFlags = destFlags;
            DestRaidFlags = destRaidFlags;
            SpellID = spellID;
            SpellName = spellName;
            SpellSchool = spellSchool;
            EnvironmentalType = environmentalType;
            InfoGUID = infoGUID;
            OwnerGUID = ownerGUID;
            CurrentHP = currentHP;
            MaxHP = maxHP;
            AttackPower = attackPower;
            SpellPower = spellPower;
            Armor = armor;
            Absorb = absorb;
            PowerType = powerType;
            CurrentPower = currentPower;
            MaxPower = maxPower;
            PowerCost = powerCost;
            PositionX = positionX;
            PositionY = positionY;
            UIMapID = uIMapID;
            Facing = facing;
            Level = level;
            Amount = amount;
            Overkill = overkill;
            School = school;
            Resisted = resisted;
            Blocked = blocked;
            Absorbed = absorbed;
            Critical = critical;
            Glancing = glancing;
            Crushing = crushing;
            IsOffHand = isOffHand;
            ExtraGUID = extraGUID;
            ExtraName = extraName;
            ExtraFlags = extraFlags;
            ExtraRaidFlags = extraRaidFlags;
            ExtraSpellID = extraSpellID;
            ExtraSpellName = extraSpellName;
            ExtraSchool = extraSchool;
            AbsorbedAmount = absorbedAmount;
            TotalAmount = totalAmount;
            MissType = missType;
            AmountMissed = amountMissed;
            Overhealing = overhealing;
            OverEnergize = overEnergize;
            AuraType = auraType;
            FailedType = failedType;
            UnconsciousOnDeath = unconsciousOnDeath;
            EncounterID = encounterID;
            EncounterName = encounterName;
            DifficultyID = difficultyID;
            GroupSize = groupSize;
            InstanceID = instanceID;
            EncounterSuccess = encounterSuccess;
            EncounterTime = encounterTime;
            UIMapName = uIMapName;
            ZoneName = zoneName;
            BoundXLower = boundXLower;
            BoundXUpper = boundXUpper;
            BoundYLower = boundYLower;
            BoundYUpper = boundYUpper;
        }
    }

    //to do: pull combatant log info, give to replay object and fetch class color
    public struct CombatantStruct
    {
        public string EventString;
        public string PlayerGUID;
        public int Faction;
        public int Stat_Strength;
        public int Stat_Agility;
        public int Stat_Stamina;
        public int Stat_Intelligence;
        public int Stat_Dodge;
        public int Stat_Parry;
        public int Stat_Block;
        public int Stat_CritMelee;
        public int Stat_CritRanged;
        public int Stat_CritSpell;
        public int Stat_Speed;
        public int Stat_Lifesteal;
        public int Stat_HasteMelee;
        public int Stat_HasteRanged;
        public int Stat_HasteSpell;
        public int Stat_Avoidance;
        public int Stat_Mastery;
        public int Stat_VersatilityDamageDone;
        public int Stat_VersatilityHealingDone;
        public int Stat_VersatilityDamageTaken;
        public int Stat_Armor;
        public int Class_CurrentSpecID;
        //TO DO LATER: FIGURE OUT HOW THE HELL TO ADD DRAGONFLIGHT TALENTS
        //WILL NEED TO PARSE THROUGH WHILE ACCOUNTING FOR COMMAS IN BRACKETS/PARENTHS
        //SOMETHING LIKE THIS:
        // Regex regx = new Regex(',' + "(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
        // return regx.Split(logEvent);
    }

    public struct EncounterStruct
    {
        public DateTime EncounterStartTime;
        /// <summary>
        /// source: https://wowpedia.fandom.com/wiki/DungeonEncounterID
        /// </summary>
        public int EncounterID;
        public string EncounterName;
        /// <summary>
        /// source: https://wowpedia.fandom.com/wiki/DifficultyID
        /// </summary>
        public int DifficultyID;
        public int GroupSize;
        /// <summary>
        /// source: https://wowpedia.fandom.com/wiki/InstanceID
        /// </summary>
        public int InstanceID;
        public int EncounterPull;
        /// <summary>
        /// encounter time in milliseconds
        /// </summary>
        public int EncounterTime;
        public float EncounterPercent;
    }

    public struct GameMapStruct
    {
        public int UIMapID;
        public string UIMapName;
        public float XLower;
        public float ZLower;
        public float XUpper;
        public float ZUpper;
    }
        //Hold all the encounter data, passed by the ENCOUNTER_START and ENCOUNTER_END
        //As well as the latest Map and Zone data from before ENCOUNTER_START
    public EncounterStruct LoadEncounterStruct(DateTime enStart, 
        int enID, 
        string enName, 
        int diffID, 
        int groupSize, 
        int instanceID, 
        int enPull, 
        int enTime, 
        float enPercent)
    {
        EncounterStruct newEncounter = new EncounterStruct();
        newEncounter.EncounterStartTime = enStart;
        newEncounter.EncounterID= enID;
        newEncounter.EncounterName= enName;
        newEncounter.DifficultyID= diffID;
        newEncounter.GroupSize= groupSize;
        newEncounter.InstanceID= instanceID;
        newEncounter.EncounterPull= enPull;
        newEncounter.EncounterTime= enTime;
        newEncounter.EncounterPercent= enPercent;
        return newEncounter;
    }

    public CombatantStruct LoadCombatantStruct(string _EventString,
        string _PlayerGUID,
        int _Faction,
        int _Stat_Strength,
        int _Stat_Agility,
        int _Stat_Stamina,
        int _Stat_Intelligence,
        int _Stat_Dodge,
        int _Stat_Parry,
        int _Stat_Block,
        int _Stat_CritMelee,
        int _Stat_CritRanged,
        int _Stat_CritSpell,
        int _Stat_Speed,
        int _Stat_Lifesteal,
        int _Stat_HasteMelee,
        int _Stat_HasteRanged,
        int _Stat_HasteSpell,
        int _Stat_Avoidance,
        int _Stat_Mastery,
        int _Stat_VersatilityDamageDone,
        int _Stat_VersatilityHealingDone,
        int _Stat_VersatilityDamageTaken,
        int _Stat_Armor,
        int _Class_CurrentSpecID)
    {
        CombatantStruct newCombatantStruct = new CombatantStruct();
        newCombatantStruct.EventString = _EventString;
        newCombatantStruct.PlayerGUID = _PlayerGUID;
        newCombatantStruct.Faction = _Faction;
        newCombatantStruct.Stat_Strength = _Stat_Strength;
        newCombatantStruct.Stat_Agility = _Stat_Agility;
        newCombatantStruct.Stat_Stamina = _Stat_Stamina;
        newCombatantStruct.Stat_Intelligence = _Stat_Intelligence;
        newCombatantStruct.Stat_Dodge = _Stat_Dodge;
        newCombatantStruct.Stat_Parry = _Stat_Parry;
        newCombatantStruct.Stat_Block = _Stat_Block;
        newCombatantStruct.Stat_CritMelee = _Stat_CritMelee;
        newCombatantStruct.Stat_CritRanged = _Stat_CritRanged;
        newCombatantStruct.Stat_CritSpell = _Stat_CritSpell;
        newCombatantStruct.Stat_Speed = _Stat_Speed;
        newCombatantStruct.Stat_Lifesteal = _Stat_Lifesteal;
        newCombatantStruct.Stat_HasteMelee = _Stat_HasteMelee;
        newCombatantStruct.Stat_HasteRanged = _Stat_HasteRanged;
        newCombatantStruct.Stat_HasteSpell = _Stat_HasteSpell;
        newCombatantStruct.Stat_Avoidance = _Stat_Avoidance;
        newCombatantStruct.Stat_Mastery = _Stat_Mastery;
        newCombatantStruct.Stat_VersatilityDamageDone = _Stat_VersatilityDamageDone;
        newCombatantStruct.Stat_VersatilityHealingDone = _Stat_VersatilityHealingDone;
        newCombatantStruct.Stat_VersatilityDamageTaken = _Stat_VersatilityDamageTaken;
        newCombatantStruct.Stat_Armor = _Stat_Armor;
        newCombatantStruct.Class_CurrentSpecID = _Class_CurrentSpecID;
        return newCombatantStruct;
    }
        //Each class spec ID
    public enum SpecID
    {
        DK_Blood = 250,
        DK_Frost = 251,
        DK_Unholy = 252,
        DH_Havoc = 577,
        DH_Vengeance = 581,
        DR_Balance = 102,
        DR_Feral = 103,
        DR_Guardian = 104,
        DR_Restoration = 105,
        EV_Devastation = 1467,
        EV_Preservation = 1468,
        EV_Augmentation = 1473,
        HU_BeastMastery = 253,
        HU_Marksmanship = 254,
        HU_Survival = 255,
        MA_Arcane = 62,
        MA_Fire = 63,
        MA_Frost = 64,
        MO_Brewmaster = 268,
        MO_Mistweaver = 270,
        MO_Windwalker = 269,
        PA_Holy = 65,
        PA_Protection = 66,
        PA_Retribution = 70,
        PR_Discipline = 256,
        PR_Holy = 257,
        PR_Shadow = 258,
        RO_Assassination = 259,
        RO_Outlaw = 260,
        RO_Subtlety = 261,
        SH_Elemental = 262,
        SH_Enhancement = 263,
        SH_Restoration = 264,
        WL_Affliction = 265,
        WL_Demonology = 266,
        WL_Destruction = 267,
        WR_Arms = 71,
        WR_Fury = 72,
        WR_Protection = 73
    }

    public enum SubeventType
    {
        //ENVIRONMENTAL PREFIX
        ENVIRONMENTAL_DAMAGE,

        //SWING PREFIX
        SWING_DAMAGE,
        SWING_DAMAGE_LANDED,
        SWING_MISSED,

        //RANGE, SPELL, SPELL_PERIODIC, SPELL_BUILDING PREFIXES
        RANGE_DAMAGE,
        RANGE_MISSED,
        SPELL_CAST_START,
        SPELL_CAST_SUCCESS,
        SPELL_CAST_FAILED,
        SPELL_MISSED,
        SPELL_DAMAGE,
        SPELL_ABSORBED,
        SPELL_HEAL,
        SPELL_HEAL_ABSORBED,
        SPELL_ENERGIZE,
        SPELL_DRAIN,
        SPELL_LEECH,
        SPELL_SUMMON,
        SPELL_RESURRECT,
        SPELL_CREATE,
        SPELL_INSTAKILL,
        SPELL_INTERRUPT,
        SPELL_EXTRA_ATTACKS,
        SPELL_DURABILITY_DAMAGE,
        SPELL_DURABILITY_DAMAGE_ALL,
        SPELL_AURA_APPLIED,
        SPELL_AURA_APPLIED_DOSE,
        SPELL_AURA_REMOVED,
        SPELL_AURA_REMOVED_DOSE,
        SPELL_AURA_BROKEN,
        SPELL_AURA_BROKEN_SPELL,
        SPELL_AURA_REFRESH,
        SPELL_DISPEL,
        SPELL_DISPEL_FAILED,
        SPELL_STOLEN,
        SPELL_PERIODIC_DAMAGE,
        SPELL_PERIODIC_MISSED,
        SPELL_PERIODIC_HEAL,
        SPELL_PERIODIC_ENERGIZE,
        SPELL_PERIODIC_DRAIN,
        SPELL_PERIODIC_LEECH,
        SPELL_EMPOWER_START,
        SPELL_EMPOWER_END,
        SPELL_EMPOWER_INTERRUPT,
        SPELL_BUILDING_DAMAGE,
        SPELL_BUILDING_HEAL,

        // SPECIAL
        DAMAGE_SHIELD,
        DAMAGE_SHIELD_MISSED,
        DAMAGE_SPLIT,
        ENCHANT_APPLIED,
        ENCHANT_REMOVED,
        PARTY_KILL,
        UNIT_DIED,
        UNIT_DESTROYED,
        UNIT_DISSIPATES,
        COMBATANT_INFO,
        EMOTE,
        ENCOUNTER_START,
        ENCOUNTER_END,
        MAP_CHANGE,
        WORLD_MARKER_PLACED,
        WORLD_MARKER_REMOVED,
        ZONE_CHANGE,
        COMBAT_LOG_VERSION,

        //BROKEN EVENT TYPE
        UNKNOWN_TYPE_ERROR
    }


}
