﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

public class Combat : NetworkBehaviour
{
    public const int maxHealth = 3;

    [SyncVar]
    public int health = maxHealth;

    public GameObject bulletPrefab;
    public float bulletSpeed = 1f;
    public float bulletTimer = 2f;

    public Text playerHealth;
    public Text enemyHealth;

    public GameObject HurtScreenPrefab;

    private HurtFlash[] hurtFlashes;
    private int hurtFlashCount = 7;
    private int hurtFlashIndex = 0;

    private Player player;
    public Transform canvas;

    private Transform avatar;
    private Vector3 prevPos;

    private int prevHealth = maxHealth;

    public GameObject healthBarPrefab;

    private void Start()
    {
        player = GetComponent<Player>();
        if (player.PlayerType == PlayerType.AR)
        {
            avatar = player.ARAvatar.transform;
        }
        else
        {
            avatar = player.VRAvatar.transform;
        }

        if (!isLocalPlayer)
        {
            HealthBar hb = Instantiate(healthBarPrefab).GetComponent<HealthBar>();
            hb.Init(this, player.PlayerType, avatar);
        }

        prevPos = avatar.position;
    }

    public override void OnStartLocalPlayer()
    {
        if (!canvas)
            canvas = GameObject.Find("Canvas").transform;

        hurtFlashes = new HurtFlash[hurtFlashCount];
        for (int i = 0; i < hurtFlashCount; i++)
        {
            hurtFlashes[i] = Instantiate(HurtScreenPrefab, canvas).GetComponent<HurtFlash>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (player.PlayerType == PlayerType.VR)
            avatar.forward = transform.forward;

        if (!isLocalPlayer)
            return;

        //DEBUG CODE
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.E))
        {
            TakeDamage();
        }

        if (!IsPointerOverUIObject() && (Input.GetMouseButtonDown(0) || CheckTap()))
        {
            CmdFire();
        }

        if (prevHealth != health)
        {
            hurtFlashes[hurtFlashIndex].FlashRed();
            hurtFlashIndex++;
            if (hurtFlashIndex > hurtFlashCount - 1)
                hurtFlashIndex = 0;
        }

        prevHealth = health;
        prevPos = avatar.position;
    }

    bool CheckTap()
    {
#if UNITY_IOS
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                    return true;
            }
        }
#endif
        return false;
    }

    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    [Command]
    void CmdFire()
    {
        GameObject bulletObj = null;

        if (player.PlayerType == PlayerType.AR)
        {
            bulletObj = Instantiate(bulletPrefab,
                                    avatar.position + avatar.localScale.z * avatar.forward,
                                    Quaternion.identity);
            bulletObj.GetComponent<Rigidbody>().velocity = avatar.forward * bulletSpeed;// + (avatar.position - prevPos);
        }
        else
        {
            bulletObj = Instantiate(bulletPrefab,
                                    transform.position + avatar.localScale.z * transform.forward,
                                    Quaternion.identity);
            bulletObj.GetComponent<Rigidbody>().velocity = transform.forward * bulletSpeed;// + (avatar.position - prevPos);

        }
        bulletObj.GetComponent<Bullet>().Init(player.PlayerType);

        NetworkServer.Spawn(bulletObj);
        Destroy(bulletObj, bulletTimer);
    }


    [Server]
    public void TakeDamage()
    {
        if (!isServer)
            return;

        health--;


        if (health < 1)
        {
            //health = maxHealth;
            //isDead = true;
            //RpcRespawn();
        }
    }

    [ClientRpc]
    void RpcRespawn()
    {
        if (isLocalPlayer)
        {
            if (GetComponent<Player>().PlayerType == PlayerType.VR)
                transform.position = Vector3.zero;
        }
    }
}
