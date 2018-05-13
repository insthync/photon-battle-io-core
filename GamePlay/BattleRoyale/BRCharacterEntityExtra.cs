﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(CharacterEntity))]
public class BRCharacterEntityExtra : PunBehaviour
{
    public const string CUSTOM_PLAYER_IS_SPAWNED = "bSPAWNED";
    public bool isSpawned
    {
        get { return (bool)photonView.owner.CustomProperties[CUSTOM_PLAYER_IS_SPAWNED]; }
        set { if (PhotonNetwork.isMasterClient && value != isSpawned) photonView.owner.SetCustomProperties(new Hashtable() { { CUSTOM_PLAYER_IS_SPAWNED, value } }); }
    }

    private Transform tempTransform;
    public Transform TempTransform
    {
        get
        {
            if (tempTransform == null)
                tempTransform = GetComponent<Transform>();
            return tempTransform;
        }
    }

    private CharacterEntity tempCharacterEntity;
    public CharacterEntity TempCharacterEntity
    {
        get
        {
            if (tempCharacterEntity == null)
                tempCharacterEntity = GetComponent<CharacterEntity>();
            return tempCharacterEntity;
        }
    }
    private float botRandomSpawn;
    private bool botSpawnCalled;
    private bool botDeadRemoveCalled;
    private float lastCircleCheckTime;
    private bool disableRenderers;

    private void Awake()
    {
        TempCharacterEntity.enabled = false;
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        var maxRandomDist = 30f;
        if (brGameManager != null)
            maxRandomDist = brGameManager.spawnerMoveDuration * 0.25f;
        botRandomSpawn = Random.Range(0f, maxRandomDist);

        if (photonView.isMine)
        {
            if (brGameManager != null && brGameManager.currentState != BRState.WaitingForPlayers)
                GameNetworkManager.Singleton.LeaveRoom();
        }
    }

    private void Start()
    {
        TempCharacterEntity.onDead += OnDead;
    }

    private void OnDestroy()
    {
        TempCharacterEntity.onDead -= OnDead;
    }

    private void Update()
    {
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        if (PhotonNetwork.isMasterClient)
        {
            if (brGameManager.currentState != BRState.WaitingForPlayers && Time.realtimeSinceStartup - lastCircleCheckTime >= 1f)
            {
                var currentPosition = TempTransform.position;
                currentPosition.y = 0;

                var centerPosition = brGameManager.currentCenterPosition;
                centerPosition.y = 0;
                var distance = Vector3.Distance(currentPosition, centerPosition);
                var currentRadius = brGameManager.currentRadius;
                if (distance > currentRadius)
                    TempCharacterEntity.Hp -= Mathf.CeilToInt(brGameManager.CurrentCircleHpRateDps * TempCharacterEntity.TotalHp);
                lastCircleCheckTime = Time.realtimeSinceStartup;
            }
        }
        if (brGameManager.currentState != BRState.WaitingForPlayers && !isSpawned)
        {
            if (PhotonNetwork.isMasterClient && !botSpawnCalled && TempCharacterEntity is BotEntity && brGameManager.CanSpawnCharacter(TempCharacterEntity))
            {
                botSpawnCalled = true;
                StartCoroutine(BotSpawnRoutine());
            }
            if (TempCharacterEntity.TempRigidbody.useGravity)
                TempCharacterEntity.TempRigidbody.useGravity = false;
            if (TempCharacterEntity.enabled)
                TempCharacterEntity.enabled = false;
            if (PhotonNetwork.isMasterClient || photonView.isMine)
            {
                TempTransform.position = brGameManager.GetSpawnerPosition();
                TempTransform.rotation = brGameManager.GetSpawnerRotation();
            }
            if (!disableRenderers)
            {
                var renderers = GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.enabled = false;
                }
                var canvases = GetComponentsInChildren<Canvas>();
                foreach (var canvas in canvases)
                {
                    canvas.enabled = false;
                }
                disableRenderers = true;
            }
        }
        else if (brGameManager.currentState == BRState.WaitingForPlayers || isSpawned)
        {
            if (PhotonNetwork.isMasterClient && !botDeadRemoveCalled && TempCharacterEntity is BotEntity && TempCharacterEntity.IsDead)
            {
                botDeadRemoveCalled = true;
                StartCoroutine(BotDeadRemoveRoutine());
            }
            if (!TempCharacterEntity.TempRigidbody.useGravity)
                TempCharacterEntity.TempRigidbody.useGravity = true;
            if (!TempCharacterEntity.enabled)
                TempCharacterEntity.enabled = true;
            if (disableRenderers)
            {
                var renderers = GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                }
                var canvases = GetComponentsInChildren<Canvas>();
                foreach (var canvas in canvases)
                {
                    canvas.enabled = true;
                }
                disableRenderers = false;
            }
        }
    }

    IEnumerator BotSpawnRoutine()
    {
        yield return new WaitForSeconds(botRandomSpawn);
        ServerCharacterSpawn();
    }

    IEnumerator BotDeadRemoveRoutine()
    {
        yield return new WaitForSeconds(5f);
        PhotonNetwork.Destroy(gameObject);
    }

    private void OnDead()
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameplayManager != null)
            RpcRankResult(BaseNetworkGameManager.Singleton.CountAliveCharacters() + 1);
    }

    IEnumerator ShowRankResultRoutine(int rank)
    {
        yield return new WaitForSeconds(3f);
        var ui = UIBRGameplay.Singleton;
        if (ui != null)
            ui.ShowRankResult(rank);
    }
    
    public void ServerCharacterSpawn()
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (!isSpawned && brGameplayManager != null)
        {
            isSpawned = true;
            RpcCharacterSpawned(brGameplayManager.SpawnCharacter(TempCharacterEntity) + new Vector3(Random.Range(-2.5f, 2.5f), 0, Random.Range(-2.5f, 2.5f)));
        }
    }
    
    public void CmdCharacterSpawn()
    {
        photonView.RPC("RpcServerCharacterSpawn", PhotonTargets.MasterClient);
    }
    
    [PunRPC]
    public void RpcServerCharacterSpawn()
    {
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (!isSpawned && brGameplayManager != null && brGameplayManager.CanSpawnCharacter(TempCharacterEntity))
            ServerCharacterSpawn();
    }

    [PunRPC]
    public void RpcCharacterSpawned(Vector3 spawnPosition)
    {
        TempCharacterEntity.TempTransform.position = spawnPosition;
        TempCharacterEntity.TempRigidbody.useGravity = true;
        TempCharacterEntity.TempRigidbody.isKinematic = false;
    }

    [PunRPC]
    public void RpcRankResult(int rank)
    {
        if (photonView.isMine)
            StartCoroutine(ShowRankResultRoutine(rank));
    }
}
