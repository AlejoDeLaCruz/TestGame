using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform orientation;
    public Transform weaponHolder; // Referencia al objeto que contiene las armas

    public float tiltAmount = 3f; // Cantidad de inclinación (más pequeña para ser más sutil)
    public float tiltSpeed = 3f; // Velocidad de la inclinación (ajustada para un cambio más suave)

    public float weaponBobAmount = 0.05f; // Cantidad de movimiento para las armas
    public float weaponBobSpeed = 5f; // Velocidad del movimiento de las armas

    float xRotation;
    float yRotation;

    private float currentTilt = 0f;
    private float bobOffset = 0f;

    void Start()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    void Update()
    {
        // Obtener la entrada del mouse
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Inclinación de la cámara en función de la entrada "A" y "D"
        float horizontalInput = Input.GetAxisRaw("Horizontal"); // "A" y "D" controlan el movimiento horizontal

        if (horizontalInput != 0)
        {
            // Aplicar una inclinación hacia la izquierda (A) o derecha (D)
            currentTilt = Mathf.Lerp(currentTilt, -horizontalInput * tiltAmount, Time.deltaTime * tiltSpeed);
        }
        else
        {
            // Vuelve a la posición neutral sin inclinación
            currentTilt = Mathf.Lerp(currentTilt, 0f, Time.deltaTime * tiltSpeed);
        }

        // Aplicar la rotación con la inclinación adicional
        transform.rotation = Quaternion.Euler(xRotation, yRotation, currentTilt);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);

        // Movimiento de las armas basado en la entrada de movimiento
        HandleWeaponBob();
    }

    void HandleWeaponBob()
    {
        float verticalInput = Input.GetAxisRaw("Vertical"); // "W" y "S" controlan el movimiento vertical (adelante/atrás)

        if (verticalInput != 0 || Input.GetAxisRaw("Horizontal") != 0)
        {
            // El movimiento hacia adelante y hacia atrás genera un ligero movimiento de las armas
            bobOffset = Mathf.Sin(Time.time * weaponBobSpeed) * weaponBobAmount;
            weaponHolder.localPosition = new Vector3(weaponHolder.localPosition.x, bobOffset, weaponHolder.localPosition.z);
        }
        else
        {
            // Detener el movimiento de las armas cuando no haya entrada de movimiento
            weaponHolder.localPosition = new Vector3(weaponHolder.localPosition.x, 0, weaponHolder.localPosition.z);
        }
    }
}