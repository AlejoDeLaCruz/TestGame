using UnityEngine;
using UnityEngine.InputSystem;

namespace MovementScript
{
    public class Movement : MonoBehaviour
    {
        public float moveSpeed;
        public float groundDrag;
        public float playerHeight;
        public float jumpForce;
        public float jumpCooldown;
        public float airMultiplier;
        public float crouchSpeed;
        public float crouchScale;
        public float slideForce;
        public float maxSlideTime;
        public GameObject playerBody;
        public Transform orientation;
        //private bool atract;
        public float slopeForce;
        public float maxSlopeAngle;

        public int fuerzaExtra;

        public KeyCode crouchKey = KeyCode.LeftControl;
        public KeyCode sprintKey = KeyCode.LeftShift;
        public LayerMask whatIsGround;
        public bool isDoubleSprinting = false;
        private bool grounded;
        private float startScale;
        private bool readyToJump;
        private bool isRunning;
        private bool isDoubleTapped;
        private bool sliding = false;
        private float slideTimer;
        private float horizontalInput;
        private float verticalInput;
        private float timeSinceStoppedRunning = Mathf.Infinity;
        private Rigidbody rb;

        public float RayLength = 0.24f;
        private KeyCode lastKeyPressed;
        private float lastKeyTime;
        private float doubleTapTime = 0.3f;  
        private float sprintMultiplier = 1.5f;
        private float doubleSprintMultiplier = 2f; 
        private float originalMoveSpeed;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            startScale = transform.localScale.y;
            originalMoveSpeed = moveSpeed;
        }

        private void Update()
        {
            //// Comprueba si el jugador est� en el suelo mediante un raycast
            grounded = Physics.Raycast(transform.position, Vector3.down, RayLength, whatIsGround);

            Debug.DrawRay(transform.position, Vector3.down * RayLength, Color.red);

            Debug.Log(grounded + " Grounded balor");
            HandleJump();        // maneja el salto
            HandleInput();       // maneja la entrada del jugador
            HandleRunning();     // maneja el sprint
            HandleSliding();     // maneja el deslizamiento
            HandleDoubleTap();
        }

        void HandleJump()
        {
            if (Input.GetButtonDown("Jump") && readyToJump) // Si se presiona espacio y est� listo para saltar
            {
                Debug.Log("ellepungado");
                rb.AddForce(new Vector3(0, jumpForce * 2, 0), ForceMode.Impulse); // Duplicar la fuerza actual del salto // Aplica una fuerza hacia arriba
                readyToJump = false; // Previene saltos consecutivos sin tocar el suelo
            }
            else if(grounded)
            {
                ResetJump();
            }
        }

        void ResetJump()
        {
            readyToJump = true;
        }

        void Falling()
        {
            //Agrega una fuerza extra para tirar al jugador hacia abajo
            rb.AddForce(fuerzaExtra * Physics.gravity);
        }

        void StopCrouchOrSlide()
        {
            sliding = false;

            // Mantiene al jugador agachado después del slide
            transform.localScale = new Vector3(transform.localScale.x, crouchScale, transform.localScale.z);
            moveSpeed = crouchSpeed;
        }

        void Crouch()
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchScale, transform.localScale.z);
            moveSpeed = crouchSpeed;
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        void StopCrouch()
        {
            // Llamar a este método solo cuando se suelte la tecla de agacharse
            transform.localScale = new Vector3(transform.localScale.x, startScale, transform.localScale.z);
            moveSpeed = originalMoveSpeed;
        }

        // Modifica HandleInput para usar el nuevo método StopCrouch
        void HandleInput()
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");

            if (Input.GetKeyDown(crouchKey))
            {
                if (isRunning || timeSinceStoppedRunning <= 2f)
                {
                    StartSlide();
                }
                else
                {
                    Crouch();
                }
            }

            // Detiene solo el estado de agachado si se suelta la tecla
            if (Input.GetKeyUp(crouchKey) && !sliding)
            {
                StopCrouch();
            }
        }
        void OnCollisionEnter(Collision collision)
        {
            if (grounded)
            {
                Debug.Log("onColission");
                readyToJump = true; // Permite saltar nuevamente cuando toca el suelo
            }
        }

        void HandleSliding()
        {
            if (sliding)
            {
                SlidingMovement();
            }
        }

        void SlidingMovement()
        {
            Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
            rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0)
            {
                StopCrouchOrSlide();
            }
        }

        void HandleRunning()
        {
            if (Input.GetKeyDown(sprintKey) && !isRunning)
            {
                moveSpeed = originalMoveSpeed * sprintMultiplier;
                isRunning = true;
                timeSinceStoppedRunning = 0f;
            }

            if (Input.GetKeyUp(sprintKey) && isRunning)
            {
                moveSpeed = originalMoveSpeed;
                isRunning = false;
            }

            if (!isRunning)
            {
                timeSinceStoppedRunning += Time.deltaTime;
            }
        }

        void HandleDoubleTap()
        {
            if (Input.GetKeyDown(sprintKey))
            {
                if (sprintKey == lastKeyPressed && Time.time - lastKeyTime <= doubleTapTime)
                {
                    isDoubleTapped = true;
                    moveSpeed = originalMoveSpeed * doubleSprintMultiplier;
                    isDoubleSprinting = true; // Activar el estado de doble sprint
                }
                else
                {
                    isDoubleTapped = false;
                }

                lastKeyPressed = sprintKey;
                lastKeyTime = Time.time;
            }
            else if (Input.GetKeyUp(sprintKey))  // Si se suelta Shift
            {
                ResetDoubleTap();  // Llamamos a ResetDoubleTap para desactivar el super sprint
            }
        }

        void ResetDoubleTap()
        {
            if (isDoubleTapped)
            {
                moveSpeed = isRunning ? originalMoveSpeed * sprintMultiplier : originalMoveSpeed;
                isDoubleSprinting = false;  // Desactivar el estado de super sprint
                isDoubleTapped = false;
            }
        }
        void StartSlide()
        {
            sliding = true;
            transform.localScale = new Vector3(transform.localScale.x, crouchScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            slideTimer = maxSlideTime;
        }

        private void FixedUpdate()
        {
            MovePlayer();
        }

        bool IsOnSlope(out Vector3 slopeNormal)
        {
            slopeNormal = Vector3.up;
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, playerHeight * 0.5f + 0.3f))
            {
                float angle = Vector3.Angle(Vector3.up, hit.normal);
                if (angle > 0 && angle <= maxSlopeAngle)
                {
                    slopeNormal = hit.normal;
                    return true;
                }
            }
            return false;
        }

        void MovePlayer()
        {
            Vector3 moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

            // Comprobamos si estamos en una pendiente
            if (IsOnSlope(out Vector3 slopeNormal))
            {
                // Proyecta el movimiento en la pendiente para ajustarlo al ángulo
                moveDirection = Vector3.ProjectOnPlane(moveDirection, slopeNormal).normalized;

                // Establece la velocidad constante en la pendiente
                rb.linearVelocity = moveDirection * moveSpeed / 2;
            }
            else if (grounded)
            {
                // Movimiento normal en el suelo
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
            }
            else
            {
                // Movimiento en el aire
                rb.AddForce(moveDirection.normalized * moveSpeed * airMultiplier, ForceMode.Force);
            }

            // Ajusta el damping en el suelo para simular fricción
            if (grounded)
            {
                rb.linearDamping = groundDrag;  // Usa drag en lugar de linearDamping
            }
            else
            {
                rb.linearDamping = 0f;  // Sin fricción en el aire
            }
        }
    }
}
