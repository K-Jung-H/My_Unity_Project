using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyProfile", menuName = "Game/Enemy Stat Profile")]
public class EnemyStatProfile : ScriptableObject
{
    [Header("Physical Stats")]
    public float mass = 1500.0f;
    public float drag = 1.0f;
    public float angularDrag = 2.0f;

    [Header("Movement Stats")]
    public float normalSpeed = 15.0f;
    public float roadSpeed = 25.0f;
    public float turnSpeed = 10.0f;
    public float acceleration = 60.0f;
    public float slopeDamping = 20.0f;

    [Header("Pathfinding Optimization")]
    public float pathUpdateInterval = 0.1f; 
    public float directChaseDistance = 40.0f;

    [Header("Combat Stats")]
    public float attackTriggerRange = 8.0f;
    public float disengageDistance = 10.0f;
    public float chargeForce = 60.0f;
    public float pressForce = 20.0f; 
    public float chargeDuration = 1.0f;
    public float chargeCooldown = 3.0f;

    [Header("Stability Stats")]
    public float airborneCheckDist = 2.0f;
    public float uprightSpeed = 5.0f;
    public float recoveryDelay = 1.0f; 
}