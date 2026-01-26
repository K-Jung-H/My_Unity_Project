using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyProfile", menuName = "Data/Enemy Profile")]
public class EnemyStatProfile : ScriptableObject
{
    [Header("Basic Physics (Rigidbody)")]
    public float Mass = 1500f;
    public float LinearDamping = 0.2f;
    public float AngularDamping = 5.0f;
    public float CenterOfMassY = -0.9f;

    [Header("Drive Performance")]
    public float AccelerationForce = 20000f;
    public float MaxSpeed = 30f;   
    public float TurnSpeed = 350f;
    public float BrakeForce = 15f;

    [Header("Handling & Grip")]
    public float SteeringGrip = 12.0f;
    public float Stability = 5.0f;
}