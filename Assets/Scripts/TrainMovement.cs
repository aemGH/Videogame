using UnityEngine;

public class TrainMovement : MonoBehaviour
{
    public Transform[] points;        // Destinations
    public float speed = 10f;
    public float waitTime = 2f;

    private int currentPointIndex;
    private bool isWaiting = false;

    void Start()
    {
        PickNewPoint();
    }

    void Update()
    {
        if (isWaiting || points.Length == 0) return;

        Transform target = points[currentPointIndex];

        // Move toward target
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        // Rotate toward direction
        Vector3 direction = (target.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Arrived
        if (Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            StartCoroutine(WaitAndPickNext());
        }
    }

    void PickNewPoint()
    {
        currentPointIndex = Random.Range(0, points.Length);
    }

    System.Collections.IEnumerator WaitAndPickNext()
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        PickNewPoint();
        isWaiting = false;
    }
}
