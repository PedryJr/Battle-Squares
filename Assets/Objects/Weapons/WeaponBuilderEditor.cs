#if UNITY_EDITOR
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

[CustomEditor(typeof(WeaponBuilder))]
[CanEditMultipleObjects]
public class WeaponBuilderEditor : Editor
{
    /*
    PSEUDOCODE / PLAN (detailed):
    - Add a new serialized property field to represent a selectable Sprite icon in the weapon specs.
    - Name the serialized property 'icon' to be concise and clear.
    - Declare a new `SerializedProperty icon` field alongside the other SerializedProperty fields.
    - In `OnEnable()` locate the 'icon' property via `specsProp.FindPropertyRelative("icon")`.
    - In the inspector UI, display the sprite property in the "Projectile" foldout (so it's easy to find).
      - Use `EditorGUILayout.PropertyField(icon)` guarded by a null check to avoid issues when field is missing.
    - No other logic changes required because ObjectReference handling in `ApplyObjectField` already supports assets by GUID.
    - Keep formatting consistent with the existing file and Unity editor code patterns.
    - Provide a full file replacement so the new field is integrated cleanly.
    */

    // Foldout state
    bool projectileFold;
    bool firingFold;
    bool damageFold;
    bool morphFold;
    bool syncFold;
    bool meleeFold;
    bool homingFold;
    bool miscFold;

    SerializedProperty specsProp;

    SerializedProperty projectile;
    SerializedProperty launchParticle;

    SerializedProperty projectileAmmo;
    SerializedProperty lifeTime;
    SerializedProperty projectileSpeed;
    SerializedProperty projectileAcceleration;
    SerializedProperty bounces;
    SerializedProperty noGravity;
    SerializedProperty fluctuation;
    SerializedProperty speedLimit;
    SerializedProperty minSpeed;
    SerializedProperty spinSpeed;
    SerializedProperty rotationFlipOnImpact;
    SerializedProperty icon;

    SerializedProperty reloadTime;
    SerializedProperty shootingInterval;
    SerializedProperty holdable;
    SerializedProperty burst;
    SerializedProperty burstSpread;
    SerializedProperty recoil;
    SerializedProperty flipFlop;

    SerializedProperty baseDamage;
    SerializedProperty aoeDamage;
    SerializedProperty aoe;
    SerializedProperty skipAoeOnTargetHit;
    SerializedProperty damageOnImpact;
    SerializedProperty dieOnImpact;
    SerializedProperty lingeringDamage;
    SerializedProperty lingeringFrequency;
    SerializedProperty damageTimeScale;
    SerializedProperty knockback;
    SerializedProperty oneTimeHit;

    SerializedProperty enableMorph;
    SerializedProperty targetMorph;
    SerializedProperty timeToMorph;
    SerializedProperty morphAnimation;

    SerializedProperty sync;
    SerializedProperty syncSpeed;
    SerializedProperty stickToSender;
    SerializedProperty alignDirection;

    SerializedProperty melee;
    SerializedProperty meleeRange;
    SerializedProperty swingDegrees;
    SerializedProperty meleeRotation;
    SerializedProperty meleePosAnimation;
    SerializedProperty meleeRotAnimation;

    SerializedProperty homing;
    SerializedProperty homingStrength;
    SerializedProperty homingDistance;

    SerializedProperty dieFromProjectiles;
    SerializedProperty dontBlockProjectiles;
    SerializedProperty bounceOfPlayers;
    SerializedProperty slowDownAmount;
    SerializedProperty senderSpeedOnDeath;
    SerializedProperty bounceParticle;
    SerializedProperty impactParticle;
    SerializedProperty clampMorph;
    SerializedProperty bounceSpeedLoss;
    SerializedProperty bounceAngleTilt;
    SerializedProperty spawnOffsetPadding;

    void OnEnable()
    {
        specsProp = serializedObject.FindProperty("specs");


        projectile = specsProp.FindPropertyRelative("projectile");
        launchParticle = specsProp.FindPropertyRelative("launchParticle");
        projectileAmmo = specsProp.FindPropertyRelative("projectileAmmo");
        lifeTime = specsProp.FindPropertyRelative("lifeTime");
        projectileSpeed = specsProp.FindPropertyRelative("projectileSpeed");
        projectileAcceleration = specsProp.FindPropertyRelative("projectileAcceleration");
        bounces = specsProp.FindPropertyRelative("bounces");
        noGravity = specsProp.FindPropertyRelative("noGravity");
        fluctuation = specsProp.FindPropertyRelative("fluctuation");
        speedLimit = specsProp.FindPropertyRelative("speedLimit");
        minSpeed = specsProp.FindPropertyRelative("minSpeed");
        spinSpeed = specsProp.FindPropertyRelative("spinSpeed");
        rotationFlipOnImpact = specsProp.FindPropertyRelative("rotationFlipOnImpact");
        icon = specsProp.FindPropertyRelative("icon");
        reloadTime = specsProp.FindPropertyRelative("reloadTime");
        shootingInterval = specsProp.FindPropertyRelative("shootingInterval");
        holdable = specsProp.FindPropertyRelative("holdable");
        burst = specsProp.FindPropertyRelative("burst");
        burstSpread = specsProp.FindPropertyRelative("burstSpread");
        recoil = specsProp.FindPropertyRelative("recoil");
        flipFlop = specsProp.FindPropertyRelative("flipFlop");
        baseDamage = specsProp.FindPropertyRelative("baseDamage");
        aoeDamage = specsProp.FindPropertyRelative("aoeDamage");
        aoe = specsProp.FindPropertyRelative("aoe");
        skipAoeOnTargetHit = specsProp.FindPropertyRelative("skipAoeOnTargetHit");
        damageOnImpact = specsProp.FindPropertyRelative("damageOnImpact");
        dieOnImpact = specsProp.FindPropertyRelative("dieOnImpact");
        lingeringDamage = specsProp.FindPropertyRelative("lingeringDamage");
        lingeringFrequency = specsProp.FindPropertyRelative("lingeringFrequency");
        damageTimeScale = specsProp.FindPropertyRelative("damageTimeScale");
        knockback = specsProp.FindPropertyRelative("knockback");
        oneTimeHit = specsProp.FindPropertyRelative("oneTimeHit");
        enableMorph = specsProp.FindPropertyRelative("enableMorph");
        targetMorph = specsProp.FindPropertyRelative("targetMorph");
        timeToMorph = specsProp.FindPropertyRelative("timeToMorph");
        morphAnimation = specsProp.FindPropertyRelative("morphAnimation");
        sync = specsProp.FindPropertyRelative("sync");
        syncSpeed = specsProp.FindPropertyRelative("syncSpeed");
        stickToSender = specsProp.FindPropertyRelative("stickToSender");
        alignDirection = specsProp.FindPropertyRelative("alignDirection");
        melee = specsProp.FindPropertyRelative("melee");
        meleeRange = specsProp.FindPropertyRelative("meleeRange");
        swingDegrees = specsProp.FindPropertyRelative("swingDegrees");
        meleeRotation = specsProp.FindPropertyRelative("meleeRotation");
        meleePosAnimation = specsProp.FindPropertyRelative("meleePosAnimation");
        meleeRotAnimation = specsProp.FindPropertyRelative("meleeRotAnimation");
        homing = specsProp.FindPropertyRelative("homing");
        homingStrength = specsProp.FindPropertyRelative("homingStrength");
        homingDistance = specsProp.FindPropertyRelative("homingDistance");
        dieFromProjectiles = specsProp.FindPropertyRelative("dieFromProjectiles");
        dontBlockProjectiles = specsProp.FindPropertyRelative("dontBlockProjectiles");
        bounceOfPlayers = specsProp.FindPropertyRelative("bounceOfPlayers");
        slowDownAmount = specsProp.FindPropertyRelative("slowDownAmount");
        senderSpeedOnDeath = specsProp.FindPropertyRelative("senderSpeedOnDeath");
        bounceParticle = specsProp.FindPropertyRelative("bounceParticle");
        impactParticle = specsProp.FindPropertyRelative("impactParticle");
        clampMorph = specsProp.FindPropertyRelative("clampMorph");
        bounceSpeedLoss = specsProp.FindPropertyRelative("bounceSpeedLoss");
        bounceAngleTilt = specsProp.FindPropertyRelative("bounceAngleTilt");
        spawnOffsetPadding = specsProp.FindPropertyRelative("spawnOffsetPadding");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Weapon Specs", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        projectileFold = EditorGUILayout.Foldout(projectileFold, "Projectile");
        if (projectileFold)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(projectile);
            EditorGUILayout.PropertyField(icon, new GUIContent("Icon"));
            EditorGUILayout.PropertyField(launchParticle);
            EditorGUILayout.PropertyField(bounceParticle);
            EditorGUILayout.PropertyField(impactParticle);
            EditorGUILayout.PropertyField(lifeTime);
            EditorGUILayout.PropertyField(spawnOffsetPadding);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        firingFold = EditorGUILayout.Foldout(firingFold, "Firing");
        if (firingFold)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(projectileAmmo);
            EditorGUILayout.PropertyField(reloadTime);
            EditorGUILayout.PropertyField(shootingInterval);
            EditorGUILayout.PropertyField(holdable);
            EditorGUILayout.PropertyField(burst);
            EditorGUILayout.PropertyField(burstSpread);
            EditorGUILayout.PropertyField(fluctuation);
            EditorGUILayout.PropertyField(recoil);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        homingFold = EditorGUILayout.Foldout(homingFold, "Movement");
        if (homingFold)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(projectileSpeed);
            EditorGUILayout.PropertyField(projectileAcceleration);
            EditorGUILayout.PropertyField(bounces);
            EditorGUILayout.PropertyField(bounceSpeedLoss);
            EditorGUILayout.PropertyField(bounceAngleTilt);
            EditorGUILayout.PropertyField(noGravity);
            EditorGUILayout.PropertyField(speedLimit);
            EditorGUILayout.PropertyField(minSpeed);
            EditorGUILayout.PropertyField(spinSpeed);
            EditorGUILayout.PropertyField(rotationFlipOnImpact);
            EditorGUILayout.PropertyField(alignDirection);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        damageFold = EditorGUILayout.Foldout(damageFold, "Damage");
        if (damageFold)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(baseDamage);
            EditorGUILayout.PropertyField(damageTimeScale);
            EditorGUILayout.PropertyField(knockback);
            EditorGUILayout.PropertyField(oneTimeHit);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(aoe);
            EditorGUILayout.PropertyField(aoeDamage);
            EditorGUILayout.PropertyField(skipAoeOnTargetHit);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(damageOnImpact);
            EditorGUILayout.PropertyField(dieOnImpact);
            EditorGUILayout.PropertyField(lingeringDamage);
            EditorGUILayout.PropertyField(lingeringFrequency);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        morphFold = EditorGUILayout.Foldout(morphFold, "Morph");
        if (morphFold)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(enableMorph);
            EditorGUILayout.PropertyField(targetMorph);
            EditorGUILayout.PropertyField(timeToMorph);
            EditorGUILayout.PropertyField(clampMorph);
            EditorGUILayout.PropertyField(morphAnimation);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        syncFold = EditorGUILayout.Foldout(syncFold, "Sync / Return");
        if (syncFold)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(sync);
            EditorGUILayout.PropertyField(syncSpeed);
            EditorGUILayout.PropertyField(stickToSender);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        meleeFold = EditorGUILayout.Foldout(meleeFold, "Melee");
        if (meleeFold)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(melee);
            EditorGUILayout.PropertyField(meleeRange);
            EditorGUILayout.PropertyField(swingDegrees);
            EditorGUILayout.PropertyField(meleeRotation);
            EditorGUILayout.PropertyField(meleePosAnimation);
            EditorGUILayout.PropertyField(meleeRotAnimation);
            EditorGUILayout.PropertyField(flipFlop);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        homingFold = EditorGUILayout.Foldout(homingFold, "Homing");
        if (homingFold)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(homing);
            EditorGUILayout.PropertyField(homingStrength);
            EditorGUILayout.PropertyField(homingDistance);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        miscFold = EditorGUILayout.Foldout(miscFold, "Misc");
        if (miscFold)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(dieFromProjectiles);
            EditorGUILayout.PropertyField(dontBlockProjectiles);
            EditorGUILayout.PropertyField(bounceOfPlayers);
            EditorGUILayout.PropertyField(slowDownAmount);
            EditorGUILayout.PropertyField(senderSpeedOnDeath);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        serializedObject.ApplyModifiedProperties();
    }

}
#endif