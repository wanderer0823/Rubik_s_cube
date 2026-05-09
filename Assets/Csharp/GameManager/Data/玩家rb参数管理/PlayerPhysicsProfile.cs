using UnityEngine;

[CreateAssetMenu(fileName = "PhysicsProfile", menuName = "RubiksCube/Player Physics Profile")]
public class PlayerPhysicsProfile : ScriptableObject
{
    [Header("Rigidbody")]
    public float mass = 1f;
    public float drag = 0.5f;
    public float angularDrag = 0.05f;

    [Header("Åö×²²ÄÖÊ")]
    [Range(0f, 1f)]
    public float bounciness = 0f;
    [Range(0f, 1f)]
    public float friction = 0.5f;

    [Header("ÒÆ¶¯")]
    public float moveSpeed = 5f;
}
