#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponBuilder))]
[CanEditMultipleObjects]
public class WeaponBuilderEditor : Editor
{

    static bool projectileFold;
    static bool firingFold;
    static bool damageFold;
    static bool morphFold;
    static bool syncFold;
    static bool meleeFold;
    static bool homingFold;
    static bool miscFold;
    static bool movementFold;
    static bool spawnEventsFold;

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
    SerializedProperty hover;
    SerializedProperty hoverStrength;
    SerializedProperty hoverFloorRadius;
    SerializedProperty hoverDistance;
    SerializedProperty hoverDistanceAttenuation;
    SerializedProperty timeForFullHoverEffect;
    SerializedProperty projectileSpawnEvents;
    SerializedProperty weaponName;
    SerializedProperty delistWeapon;
    SerializedProperty setMorphTimeOnBounce;
    SerializedProperty morphTimeOnBounce;

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
        hover = specsProp.FindPropertyRelative("hover");
        hoverDistance = specsProp.FindPropertyRelative("hoverDistance");
        hoverStrength = specsProp.FindPropertyRelative("hoverStrength");
        hoverFloorRadius = specsProp.FindPropertyRelative("hoverFloorRadius");
        hoverDistanceAttenuation = specsProp.FindPropertyRelative("hoverDistanceAttenuation");
        timeForFullHoverEffect = specsProp.FindPropertyRelative("timeForFullHoverEffect");
        projectileSpawnEvents = specsProp.FindPropertyRelative("projectileSpawnEvents");
        weaponName = specsProp.FindPropertyRelative("weaponName");
        delistWeapon = specsProp.FindPropertyRelative("delistWeapon");
        setMorphTimeOnBounce = specsProp.FindPropertyRelative("setMorphOnBounce");
        morphTimeOnBounce = specsProp.FindPropertyRelative("morphTimeOnBounce");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Weapon Specs", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(weaponName);
        EditorGUILayout.PropertyField(delistWeapon);
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

        movementFold = EditorGUILayout.Foldout(movementFold, "Movement");
        if (movementFold)
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
            EditorGUILayout.PropertyField(stickToSender);
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
            EditorGUILayout.PropertyField(setMorphTimeOnBounce);
            EditorGUILayout.PropertyField(morphTimeOnBounce);
            EditorGUILayout.PropertyField(targetMorph);
            EditorGUILayout.PropertyField(timeToMorph);
            EditorGUILayout.PropertyField(clampMorph);
            EditorGUILayout.PropertyField(morphAnimation);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        syncFold = EditorGUILayout.Foldout(syncFold, "Sync");
        if (syncFold)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(sync);
            EditorGUILayout.PropertyField(syncSpeed);
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

        homingFold = EditorGUILayout.Foldout(homingFold, "Homing / Hover");
        if (homingFold)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(homing);
            EditorGUILayout.PropertyField(homingStrength);
            EditorGUILayout.PropertyField(homingDistance);
            EditorGUILayout.PropertyField(hover);
            EditorGUILayout.PropertyField(hoverStrength);
            EditorGUILayout.PropertyField(hoverDistance);
            EditorGUILayout.PropertyField(hoverFloorRadius);
            EditorGUILayout.PropertyField(hoverDistanceAttenuation);
            EditorGUILayout.PropertyField(timeForFullHoverEffect);
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

        spawnEventsFold = EditorGUILayout.Foldout(spawnEventsFold, "SpawnEvents");
        if (spawnEventsFold)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(projectileSpawnEvents);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        EditorGUILayout.LabelField(specsProp.FindPropertyRelative("typeID").uintValue.ToString());

        serializedObject.ApplyModifiedProperties();
    }

}
#endif