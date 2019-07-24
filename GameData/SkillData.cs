﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SkillData : ScriptableObject
{
    public string GetId()
    {
        return name;
    }

    public int GetHashId()
    {
        return GetId().MakeHashId();
    }
    
    [Range(0, 7)]
    public sbyte hotkeyId;
    public Sprite icon;
    public AttackAnimation attackAnimation;
    public DamageEntity damagePrefab;
    [Tooltip("This will increase to weapon damage to calculate skill damage" +
        "Ex. weaponDamage => 10 * this => 1, skill damage = 10 + 1 = 11")]
    public int increaseDamage;
    [Tooltip("This will multiplies to weapon damage then increase to weapon damage to calculate skill damage." +
        "Ex. weaponDamage => 10 * this => 0.1, skill damage = 10 + (10 * 0.1) = 11")]
    public float increaseDamageByRate;
    public int spreadDamages = 0;
    public float coolDown = 3;
    [Header("SFX")]
    public AudioClip[] attackFx;

    public void Launch(CharacterEntity attacker)
    {
        if (attacker == null || !PhotonNetwork.isMasterClient)
            return;

        var gameNetworkManager = GameNetworkManager.Singleton;
        var gameplayManager = GameplayManager.Singleton;
        var spread = 1 + spreadDamages;
        var damage = (float)attacker.TotalAttack + increaseDamage + (attacker.TotalAttack * increaseDamageByRate);
        damage += Random.Range(gameplayManager.minAttackVaryRate, gameplayManager.maxAttackVaryRate) * damage;

        var addRotationX = 0f;
        var addRotationY = 0f;
        var addingRotationY = 360f / spread;

        if (spread <= 16)
        {
            addRotationY = (-(spread - 1) * 15f);
            addingRotationY = 30f;
        }

        for (var i = 0; i < spread; ++i)
        {
            Transform launchTransform;
            attacker.GetDamageLaunchTransform(false, out launchTransform);
            // An transform's rotation, position will be set when set `Attacker`
            // So don't worry about them before damage entity going to spawn
            // Velocity also being set when set `Attacker` too.
            var position = launchTransform.position;
            var direction = attacker.TempTransform.forward;

            var damageEntity = DamageEntity.InstantiateNewEntity(damagePrefab, false, position, direction, attacker.photonView.viewID, addRotationX, addRotationY);
            damageEntity.weaponDamage = Mathf.CeilToInt(damage);
            damageEntity.hitEffectType = CharacterEntity.RPC_EFFECT_SKILL_HIT;
            damageEntity.relateDataId = GetHashId();

            gameNetworkManager.photonView.RPC("RpcCharacterUseSkill", PhotonTargets.Others, GetHashId(), position, direction, attacker.photonView.viewID, addRotationX, addRotationY);
            addRotationY += addingRotationY;
        }

        attacker.photonView.RPC("RpcEffect", PhotonTargets.All, attacker.photonView.viewID, CharacterEntity.RPC_EFFECT_SKILL_SPAWN, GetHashId());
    }
}
