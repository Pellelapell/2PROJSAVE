using UnityEngine;
using UnityEngine.AI;

public class UnitNavigation : MonoBehaviour
{
    public NavMeshAgent agent;

    [Header("Abilities")]
    public bool canJump = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        int walkable = NavMesh.GetAreaFromName("Walkable");
        int jump = NavMesh.GetAreaFromName("Jump");

        agent.areaMask = 1 << walkable;

        if (canJump)
        {
            agent.areaMask |= 1 << jump;
        }
    }

    public void SetDestination(Vector3 target)
    {
        agent.SetDestination(target);
    }
}