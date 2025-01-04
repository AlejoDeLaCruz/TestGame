using MovementScript;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public GameObject player;
    public Camera playerCamera;
    public float detectionRange = 10f;
    public float attractToObjectForce = 10f;
    public float attractAnObjectForce = 10f;
    public float defaultHoldDistance = 5f;
    public float increasedHoldDistance = 3f;
    public float stopAttractionDistance = 1f; // Distancia mínima para detener la atracción

    private List<GameObject> heldObjects = new List<GameObject>();
    private float holdDistance;
    private bool canPickUp = true;

    private bool heavyModeActive = false;
    private bool canActivateHeavyMode = true;

    private bool isBeingAttracted = false;
    private GameObject attractionTarget = null;
    private bool isStabilized = false;
    private Vector3 attractionPoint;

    private Vector3 lastCameraRotation;
    private Vector3 cameraAngularVelocity;

    void Start()
    {
        lastCameraRotation = playerCamera.transform.eulerAngles;
    }

    void Update()
    {
        CalculateCameraAngularVelocity();

        if (player.GetComponent<Movement>().isDoubleSprinting) // Verifica si está en doble sprint
        {
            return; // No hacer nada si el jugador está en doble sprint
        }

        if (Input.GetMouseButton(0))
        {
            if (isBeingAttracted)
            {
                AttractPlayerToSurface();
            }
            else if (heldObjects.Count == 0 && canPickUp)
            {
                TryPickUpObjects();
            }
            else if (heldObjects.Count > 0)
            {
                HoldObjectsInAir();
            }
        }
        else
        {
            if (isBeingAttracted)
            {
                StopAttraction();
            }
            else if (heldObjects.Count > 0)
            {
                ReleaseObjects();
            }
        }

        if (Input.GetMouseButtonDown(1) && heldObjects.Count > 0)
        {
            ExpulseObjects();
        }

        if (Input.GetKeyDown(KeyCode.Q) && canActivateHeavyMode)
        {
            ToggleHeavyMode();
        }
    }

    void CalculateCameraAngularVelocity()
    {
        Vector3 currentRotation = playerCamera.transform.eulerAngles;
        cameraAngularVelocity = (currentRotation - lastCameraRotation) / Time.deltaTime;
        lastCameraRotation = currentRotation;
    }

    public void ReleaseObjects()
    {
        foreach (var obj in heldObjects)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;

                float angularSpeed = cameraAngularVelocity.magnitude;
                Vector3 forceDirection = playerCamera.transform.forward;
                Vector3 force = forceDirection * angularSpeed * 0.1f;

                float maxForce = 4f;
                if (force.magnitude > maxForce)
                {
                    force = force.normalized * maxForce;
                }

                rb.AddForce(force, ForceMode.Impulse);
            }
        }
        heldObjects.Clear();
    }

    void AttractPlayerToSurface()
    {
        if (attractionTarget == null) return;

        Vector3 direction = attractionPoint - player.transform.position;
        float distance = direction.magnitude;

        if (distance > stopAttractionDistance)
        {
            player.transform.position += direction.normalized * attractToObjectForce * Time.deltaTime;
        }
        else
        {
            StabilizePlayer();
        }
    }

    void StopAttraction()
    {
        isBeingAttracted = false;
        attractionTarget = null;
        isStabilized = false;

        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.isKinematic = false;
        }
    }

    void StabilizePlayer()
    {
        isStabilized = true;
        player.transform.position = attractionPoint;

        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = true;
        }
    }

    void TryPickUpObjects()
    {
        if (!canPickUp) return;

        // Detectar el objeto apuntado por el RayCast
        RaycastHit hit;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, detectionRange))
        {
            GameObject targetObject = hit.collider.gameObject;

            if (targetObject.CompareTag("Ground")) return;

            float playerScaleSum = player.transform.localScale.x + player.transform.localScale.y + player.transform.localScale.z;
            float targetScaleSum = targetObject.transform.localScale.x + targetObject.transform.localScale.y + targetObject.transform.localScale.z;

            // Priorizar atraer hacia el objeto más grande
            if (targetScaleSum > playerScaleSum && !heavyModeActive)
            {
                AttractToObject(targetObject, hit.point);
            }
            else
            {
                // Si no está apuntando hacia un objeto grande, recoger los pequeños
                if (heldObjects.Count == 0)
                {
                    PickUpObject(targetObject);
                }
            }
        }

        float smallObjectsRange = detectionRange * 0.6f; // Rango reducido para objetos pequeños

        // Recoger objetos pequeños dentro del rango reducido
        Collider[] colliders = Physics.OverlapSphere(playerCamera.transform.position, smallObjectsRange);

        foreach (var collider in colliders)
        {
            GameObject targetObject = collider.gameObject;

            if (targetObject.layer == LayerMask.NameToLayer("littleObjectToPickUp"))
            {
                PickUpObject(targetObject);
            }
        }
    }

    void AttractToObject(GameObject targetObject, Vector3 hitPoint)
    {
        isBeingAttracted = true;
        attractionTarget = targetObject;
        attractionPoint = hitPoint; // Usar el punto exacto donde el rayo impacta en el objeto
    }

    void PickUpObject(GameObject targetObject)
    {
        if (heldObjects.Contains(targetObject)) return;

        heldObjects.Add(targetObject);

        Rigidbody rb = targetObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void HoldObjectsInAir()
    {
        Vector3 basePosition = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        for (int i = 0; i < heldObjects.Count; i++)
        {
            GameObject obj = heldObjects[i];
            Vector3 targetPosition = basePosition + direction * (defaultHoldDistance + i * 0.5f);

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            obj.transform.position = Vector3.Lerp(obj.transform.position, targetPosition, attractAnObjectForce * Time.deltaTime);
            obj.transform.rotation = Quaternion.Lerp(obj.transform.rotation, Quaternion.identity, attractAnObjectForce * Time.deltaTime);
        }
    }

    void ExpulseObjects()
    {
        ReleaseObjects();
        StartCoroutine(CooldownPickUp());
    }

    public bool IsHeavyModeActive()
    {
        return heavyModeActive;
    }

    IEnumerator CooldownPickUp()
    {
        canPickUp = false;
        yield return new WaitForSeconds(1f);
        canPickUp = true;
    }

    void ToggleHeavyMode()
    {
        heavyModeActive = !heavyModeActive;
        Debug.Log($"Modo pesado {(heavyModeActive ? "activado" : "desactivado")}");
    }
}
