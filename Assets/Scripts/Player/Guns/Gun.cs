using MovementScript;
using System.Collections;
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
    private GameObject heldObject = null;
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
            else if (heldObject == null && canPickUp)
            {
                TryPickUpObject();
            }
            else if (heldObject != null)
            {
                HoldObjectInAir();
            }
        }
        else
        {
            if (isBeingAttracted)
            {
                StopAttraction();
            }
            else if (heldObject != null)
            {
                ReleaseObject();
            }
        }

        if (Input.GetMouseButtonDown(1) && heldObject != null)
        {
            ExpulseObject();
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

    public void ReleaseObject()
    {
        if (heldObject == null) return;

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;

            // Calcular la fuerza a aplicar basado en la velocidad angular de la cámara
            float angularSpeed = cameraAngularVelocity.magnitude;
            Vector3 forceDirection = playerCamera.transform.forward;

            // Calcular la fuerza original
            Vector3 force = forceDirection * angularSpeed * 0.1f;

            // Limitar la fuerza máxima
            float maxForce = 4f; // Define un valor máximo para la fuerza
            if (force.magnitude > maxForce)
            {
                force = force.normalized * maxForce; // Normaliza el vector y lo escala a la fuerza máxima
            }

            // Aplicar la fuerza al Rigidbody
            rb.AddForce(force, ForceMode.Impulse);
        }

        heldObject = null;
    }

    void AttractToObject(GameObject targetObject, Vector3 hitPoint)
    {
        isBeingAttracted = true;
        attractionTarget = targetObject;
        attractionPoint = hitPoint; // Usar el punto exacto donde el rayo impacta en el objeto
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

    void TryPickUpObject()
    {
        if (!canPickUp) return;

        RaycastHit hit;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, detectionRange))
        {
            GameObject targetObject = hit.collider.gameObject;

            if (targetObject.CompareTag("Ground")) return;

            float playerScaleSum = player.transform.localScale.x + player.transform.localScale.y + player.transform.localScale.z;
            float targetScaleSum = targetObject.transform.localScale.x + targetObject.transform.localScale.y + targetObject.transform.localScale.z;

            if (targetScaleSum > playerScaleSum && !heavyModeActive)
            {
                AttractToObject(targetObject, hit.point);
            }
            else
            {
                PickUpObject(targetObject);
            }
        }
    }

    void PickUpObject(GameObject targetObject)
    {
        heldObject = targetObject;

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        holdDistance = targetObject.transform.localScale.magnitude < player.transform.localScale.magnitude
            ? increasedHoldDistance
            : defaultHoldDistance;

        Vector3 initialPosition = targetObject.transform.position;
        Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;

        StartCoroutine(MoveToPosition(targetObject, initialPosition, targetPosition));
    }

    IEnumerator MoveToPosition(GameObject obj, Vector3 start, Vector3 end)
    {
        float progress = 0f;

        float playerScaleSum = player.transform.localScale.x + player.transform.localScale.y + player.transform.localScale.z;
        float targetScaleSum = obj.transform.localScale.x + obj.transform.localScale.y + obj.transform.localScale.z;

        float adjustedAttractionSpeed = attractAnObjectForce / Mathf.Max(1f, targetScaleSum / playerScaleSum);

        while (progress < 1f && heldObject == obj)
        {
            progress += Time.deltaTime * adjustedAttractionSpeed;
            obj.transform.position = Vector3.Lerp(start, end, progress);
            yield return null;
        }
    }

    void HoldObjectInAir()
    {
        if (heldObject == null) return;

        Vector3 direction = playerCamera.transform.forward;
        Vector3 targetPosition = playerCamera.transform.position + direction * holdDistance;

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        float playerScaleSum = player.transform.localScale.x + player.transform.localScale.y + player.transform.localScale.z;
        float targetScaleSum = heldObject.transform.localScale.x + heldObject.transform.localScale.y + heldObject.transform.localScale.z;

        float adjustedAttractionSpeed = attractAnObjectForce / Mathf.Max(1f, targetScaleSum / playerScaleSum);

        heldObject.transform.position = Vector3.Lerp(heldObject.transform.position, targetPosition, adjustedAttractionSpeed * Time.deltaTime);
        heldObject.transform.rotation = Quaternion.Lerp(heldObject.transform.rotation, Quaternion.identity, adjustedAttractionSpeed * Time.deltaTime);
    }

    void ExpulseObject()
    {
        ReleaseObject();
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
