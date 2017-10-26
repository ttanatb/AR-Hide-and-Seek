﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public enum GamePhase
{
    Scanning = 0,
    Placing = 1,
    Playing = 2,
}

[System.Serializable]
public class Trap
{
    public GameObject trap;
    public int count;
}

public class GameManager : NetworkBehaviour
{
    [SyncVar]
    private int currGamePhase = 0;

    public GamePhase CurrGamePhase { get { return (GamePhase)currGamePhase; } }

    public GameObject planeGeneratorPrefab;

    public Trap[] trapList;

    public GameObject relicPrefab;
    public GameObject entrancePrefab;

    private int currTrapSelection = -1;

    public override void OnStartServer()
    {
#if UNITY_IOS
        Instantiate(planeGeneratorPrefab);
#endif
        CanvasManager.Instance.SetUI(this);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isServer)
            return;
            
        switch (currGamePhase)
        {
            //Placing
            case 1:
                if (Utility.IsPointerOverUIObject()) return;
                if (currTrapSelection != -1)
                    CheckTapOnARPlane();
                else
                {
                    //move traps around??
                }
                break;
            default:
                break;
        }
    }


    void CheckTapOnARPlane()
    {
        RaycastHit hit;
        int layer = LayerMask.NameToLayer("Tower");

        if (Input.touchCount > 0)
        {
            foreach (Touch t in Input.touches)
            {
                if (t.phase == TouchPhase.Began &&
                    (currTrapSelection >= 0 && currTrapSelection < trapList.Length && trapList[currTrapSelection].count > 0) &&
                    Physics.Raycast(Camera.main.ScreenPointToRay(t.position), out hit, layer))
                {
                    trapList[currTrapSelection].count -= 1;

                    GameObject go = Instantiate(trapList[currTrapSelection].trap, hit.point, Quaternion.identity);
                    NetworkServer.Spawn(go);

                    CanvasManager.Instance.ClearSelection(this);
                    CanvasManager.Instance.UpdateTrapCount(this);

                    currTrapSelection = -1;
                    return;
                }
            }
        }

        //Testing for PC
        if (Input.GetKeyDown(KeyCode.S) &&
         (currTrapSelection >= 0 && currTrapSelection < trapList.Length && trapList[currTrapSelection].count > 0))
        {
            trapList[currTrapSelection].count -= 1;

            GameObject go = Instantiate(trapList[currTrapSelection].trap, Vector3.zero, Quaternion.identity);
            NetworkServer.Spawn(go);

            CanvasManager.Instance.ClearSelection(this);
            CanvasManager.Instance.UpdateTrapCount(this);

            currTrapSelection = -1;
            return;
        }

        return;
    }

    public void SetPhaseTo(GamePhase newPhase)
    {
        currGamePhase = (int)newPhase;
        CanvasManager.Instance.SetUI(this);

        switch(newPhase)
        {
            case GamePhase.Placing:
                SpawnRelic();
                break;

            case GamePhase.Playing:
                SpawnEntrancce();
                break;
        }
    }

    public void SetCurrTrapSelection(int toSelect)
    {
        currTrapSelection = toSelect;
    }

    private void SpawnRelic()
    {
        GameObject obj = Instantiate(relicPrefab);
        NetworkServer.Spawn(obj);
    }

    private void SpawnEntrancce()
    {
        GameObject obj = Instantiate(entrancePrefab);
        NetworkServer.Spawn(obj);
    }
}