using UnityEngine;

public class Gun2 : MonoBehaviour
{
    public float pushForce; // Fuerza de empuje para objetos peque�os
    public float heavyPushForceMax; // M�xima fuerza para objetos pesados
    public float pushRange; // Alcance del empuje
    public float rayWidth; // Ancho del rayo (simula un rayo grueso)
    public LayerMask ejectableObjectLayer; // Capa de objetos que siempre pueden ser expulsados
    public LayerMask heavyObjectToEjectLayer; // Capa de objetos pesados a expulsar solo si el modo pesado est� activado
    public float cooldownTime = 1f; // Tiempo de reutilizaci�n en segundos
    public float heavyChargeTime = 2f; // Tiempo m�ximo para cargar fuerza pesada
    public Camera playerCamera; // La c�mara del jugador
    private bool isChargingHeavyPush = false; // Si se est� cargando el empuje pesado
    private float heavyPushForce = 0f; // Fuerza acumulada para objetos pesados
    private bool canShoot = true; // Controla si el disparo est� disponible
    public Gun gunScript; // Referencia al script Gun
    public LayerMask groundLayer; // Capa que define el suelo

    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main; // Asigna la c�mara principal si no se ha asignado
        }

        PlayerShoot.rightClickHold += StartHeavyPush; // Inicia la carga al mantener clic derecho
        PlayerShoot.rightClickRelease += AttemptExpulse; // Expulsa al soltar clic derecho
    }

    void OnDestroy()
    {
        PlayerShoot.rightClickHold -= StartHeavyPush;
        PlayerShoot.rightClickRelease -= AttemptExpulse;
    }

    void StartHeavyPush()
    {
        if (gunScript != null && gunScript.IsHeavyModeActive())
        {
            isChargingHeavyPush = true;
            heavyPushForce = 0f;
            StartCoroutine(ChargeHeavyPush());
        }
        else
        {
            AttemptExpulse();
        }
    }

    System.Collections.IEnumerator ChargeHeavyPush()
    {
        while (isChargingHeavyPush && heavyPushForce < heavyPushForceMax)
        {
            heavyPushForce += heavyPushForceMax / heavyChargeTime * Time.deltaTime;
            yield return null;
        }
        heavyPushForce = Mathf.Min(heavyPushForce, heavyPushForceMax);
    }

    void AttemptExpulse()
    {
        if (!canShoot) return;

        isChargingHeavyPush = false;
        Expulse();
        StartCoroutine(Cooldown());
    }

    void Expulse()
    {
        bool isHeavyModeActive = gunScript != null && gunScript.IsHeavyModeActive();
        Debug.Log($"Modo pesado activo: {isHeavyModeActive}");

        // Punto de origen y direcci�n del rayo (c�mara del jugador)
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;

        // �rea de detecci�n (radio del cono)
        float detectionRadius = rayWidth; // Radio del �rea de detecci�n
        Collider[] colliders = Physics.OverlapSphere(rayOrigin, detectionRadius, ejectableObjectLayer | heavyObjectToEjectLayer);

        Debug.Log($"Cantidad de objetos dentro del �rea: {colliders.Length}");

        foreach (Collider collider in colliders)
        {
            Vector3 directionToCollider = (collider.transform.position - rayOrigin).normalized;
            float angle = Vector3.Angle(rayDirection, directionToCollider);

            // Filtra objetos fuera del �ngulo de visi�n del "cono"
            if (angle > 45f) // Ajusta este �ngulo seg�n lo que necesites
            {
                Debug.Log($"{collider.name} est� fuera del �ngulo de visi�n.");
                continue;
            }

            Debug.Log($"Objeto en �rea: {collider.name}");
            Rigidbody rb = collider.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning($"El objeto {collider.name} no tiene un Rigidbody.");
                continue;
            }

            Vector3 pushDirection = directionToCollider;

            // Verifica si el objeto pertenece a la capa heavyObjectToEjectLayer
            if (((1 << collider.gameObject.layer) & heavyObjectToEjectLayer) != 0)
            {
                if (!isHeavyModeActive)
                {
                    Debug.Log($"El objeto pesado {collider.name} no puede ser movido porque el modo pesado est� desactivado.");
                    continue;
                }

                // Aplica fuerza solo si el modo pesado est� activo
                rb.AddForce(pushDirection * heavyPushForce, ForceMode.Impulse);
                Debug.Log($"Objeto pesado {collider.name} empujado con fuerza {heavyPushForce}");
            }
            else if (((1 << collider.gameObject.layer) & ejectableObjectLayer) != 0)
            {
                // Aplica fuerza para objetos que siempre pueden ser expulsados
                rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
                Debug.Log($"Objeto {collider.name} empujado con fuerza {pushForce}");
            }
        }
    }

    System.Collections.IEnumerator Cooldown()
    {
        canShoot = false;
        yield return new WaitForSeconds(cooldownTime);
        canShoot = true;
    }

    void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            // Color del rayo (puedes cambiarlo si prefieres otro color)
            Gizmos.color = Color.blue;

            // Dibuja una l�nea que representa el rayo desde la posici�n del objeto hasta la distancia del rayo
            Gizmos.DrawLine(transform.position, transform.position + playerCamera.transform.forward * pushRange);

            // Dibuja una esfera al final del rayo para marcar el alcance
            Gizmos.DrawWireSphere(transform.position + playerCamera.transform.forward * pushRange, rayWidth);
        }
    }
}