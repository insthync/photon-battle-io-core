﻿using UnityEngine;

public class SpawnArea : MonoBehaviour
{
    public PunTeams.Team team;
    public float areaSizeX;
    public float areaSizeZ;

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + (Vector3.up * 1f), new Vector3(areaSizeX, 2f, areaSizeZ));
    }

    public Vector3 GetSpawnPosition()
    {
        return transform.position + new Vector3(Random.Range(-areaSizeX / 2f, areaSizeX / 2f), 0, Random.Range(-areaSizeZ / 2f, areaSizeZ / 2f));
    }
}
