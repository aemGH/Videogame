using UnityEngine;
using System.Collections;

public class TrainRouteSystem : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public Transform pointC;

    public float speed = 10f;
    public float waitTime = 2f;

    private enum State { ToA, ToB, ToC }
    private State state;

    void Start()
    {
        state = State.ToA;
        StartCoroutine(MoveRoutine());
    }

    IEnumerator MoveRoutine()
    {
        while (true)
        {
            Transform target = GetTarget();

            // Move to target
            while (Vector3.Distance(transform.position, target.position) > 0.2f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target.position,
                    speed * Time.deltaTime
                );

                yield return null;
            }

            yield return new WaitForSeconds(waitTime);

            // FORCE ROUTE
            if (state == State.ToA)
            {
                state = State.ToB;
            }
            else if (state == State.ToB)
            {
                //  forced teleport step
                transform.position = pointC.position;
                state = State.ToA;
            }
        }
    }

    Transform GetTarget()
    {
        if (state == State.ToA) return pointA;
        if (state == State.ToB) return pointB;
        return pointA;
    }
}