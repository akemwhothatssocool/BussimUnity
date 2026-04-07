using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BusExteriorScrollObject : MonoBehaviour
{
    public Vector3 scrollDirection = new Vector3(-1f, 0f, 0f);

    CityManager cityManager;
    Rigidbody attachedBody;
    Collider attachedCollider;
    Collider busInteriorZone;
    bool isInsideBus = true;

    void Awake()
    {
        attachedBody = GetComponent<Rigidbody>();
        attachedCollider = GetComponent<Collider>();
        cityManager = Object.FindFirstObjectByType<CityManager>();
        RefreshBusInteriorZone();
    }

    void FixedUpdate()
    {
        isInsideBus = IsInsideBusZone();

        if (isInsideBus || cityManager == null || attachedBody == null)
            return;

        if (attachedBody.isKinematic || transform.parent != null)
            return;

        Vector3 scrollMovement = scrollDirection * cityManager._currentSpeed * Time.fixedDeltaTime;
        attachedBody.MovePosition(attachedBody.position + scrollMovement);
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("BusZone"))
            isInsideBus = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("BusZone"))
            isInsideBus = false;
    }

    void RefreshBusInteriorZone()
    {
        if (busInteriorZone != null)
            return;

        GameObject busZoneObject = GameObject.FindWithTag("BusZone");
        if (busZoneObject != null)
            busInteriorZone = busZoneObject.GetComponent<Collider>();
    }

    bool IsInsideBusZone()
    {
        RefreshBusInteriorZone();
        if (busInteriorZone == null)
            return isInsideBus;

        if (attachedCollider == null)
            return busInteriorZone.bounds.Contains(transform.position);

        return attachedCollider.bounds.Intersects(busInteriorZone.bounds);
    }
}
