using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    public static event System.Action rightClickPress; // Evento para clic derecho rápido
    public static event System.Action rightClickHold; // Evento para mantener clic derecho
    public static event System.Action rightClickRelease; // Evento para soltar clic derecho

    private bool isHoldingRightClick = false;

    void Update()
    {
        // Detecta el inicio del clic derecho
        if (Input.GetMouseButtonDown(1))
        {
            rightClickPress?.Invoke(); // Llama al evento de clic rápido
            isHoldingRightClick = true;
            rightClickHold?.Invoke(); // Llama al evento de mantener clic derecho
        }

        // Detecta cuando se suelta el clic derecho
        if (Input.GetMouseButtonUp(1))
        {
            isHoldingRightClick = false;
            rightClickRelease?.Invoke(); // Llama al evento de soltar clic derecho
        }
    }
}