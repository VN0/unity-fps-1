﻿using UnityEngine;


// Usage: this script is meant to be placed on a Player.
// The Player must have a CharacterController component.
[RequireComponent(typeof(CharacterController))]
public class FPControllerBhop : MonoBehaviour {

    private CharacterController characterController;


    [SerializeField] private float gravityMultiplier = 1.6f;
    private float stickToGroundForce = 10f;
    private float friction;
    [SerializeField] private float[] frictionConstants = { 5f, 10f };
    
    
    private Vector2 inputVec;   // Horizontal movement input
    private Vector3 moveVec;    // Vector3 used to move the character controller


    private bool jump;                              // Whether the jump key is inputted
    [SerializeField] private float jumpSpeed = 5f;  // Initial upwards speed of the jump
    private bool isJumping;                         // Player has jumped and not been grounded yet
    private bool previouslyGrounded;                // Player was grounded during the last frame


    [SerializeField] private float groundAccel = 5f;
    [SerializeField] private float airAccel = 800f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxSpeedAir = 1.3f;


    void Awake()
    {
        this.characterController = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        SetDefaultState();
    }

    void Update()
    {
        // Jump
        if (!this.jump)
            this.jump = Input.GetKeyDown(KeyCode.Space) || Input.GetAxis("Mouse ScrollWheel") != 0;

        if (!this.previouslyGrounded && this.characterController.isGrounded)
        {
            this.moveVec.y = 0f;
            this.isJumping = false;
        }

        if (!this.characterController.isGrounded && !this.isJumping && this.previouslyGrounded)
        {
            this.moveVec.y = 0f;
        }

        // Horizontal movement
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        this.inputVec = new Vector2(h, v);
        if (this.inputVec.magnitude > 1)
            this.inputVec = this.inputVec.normalized;

        if (this.characterController.isGrounded)
            MoveGround();
        else
            MoveAir();
        
        //Debug.Log("Speed: " + characterController.velocity.magnitude);
        
        this.characterController.Move(this.moveVec * Time.deltaTime);
        this.jump = false;
        this.previouslyGrounded = this.characterController.isGrounded;
    }
    
    void MoveGround()
    {
        Vector3 wishVel = this.moveSpeed * (transform.forward * this.inputVec.y + this.transform.right * this.inputVec.x);
        Vector3 prevMove = new Vector3(this.moveVec.x, 0, this.moveVec.z);

        // Apply friction
        float speed = prevMove.magnitude;
        if (speed != 0) // To avoid divide by zero errors
        {
            // May implement some "sv_stopspeed"-like variable if low-speed gameplay feels too responsive 
            float drop = speed * this.friction * Time.deltaTime;
            float newSpeed = speed - drop;
            if (newSpeed < 0)
                newSpeed = 0;
            if (newSpeed != speed)
            {
                newSpeed /= speed;
                prevMove = prevMove * newSpeed;
            }

            wishVel -= (1.0f - newSpeed) * prevMove;
        }

        float wishSpeed = wishVel.magnitude;
        Vector3 wishDir = wishVel.normalized;

        Vector3 nextMove = GroundAccelerate(wishDir, prevMove, wishSpeed, this.groundAccel);
        nextMove.y = -this.stickToGroundForce;

        if (this.jump)
        {
            nextMove.y = this.jumpSpeed;
            this.jump = false;
            this.isJumping = true;
        }

        this.moveVec = nextMove;
    }

    Vector3 GroundAccelerate(Vector3 wishDir, Vector3 prevVelocity, float wishSpeed, float accel)
    {
        float currentSpeed = Vector3.Dot(prevVelocity, wishDir);
        float addSpeed = wishSpeed - currentSpeed;

        if (addSpeed <= 0)
            return prevVelocity;

        float accelSpeed = accel * wishSpeed * Time.deltaTime;

        if (accelSpeed > addSpeed)
            accelSpeed = addSpeed;

        return prevVelocity + accelSpeed * wishDir;
    }

    void MoveAir()
    {
        Vector3 wishVel = this.moveSpeed * (this.transform.forward * this.inputVec.y + transform.right * this.inputVec.x);
        float wishSpeed = wishVel.magnitude;
        Vector3 wishDir = wishVel.normalized;

        Vector3 prevMove = new Vector3(this.moveVec.x, 0, this.moveVec.z);

        Vector3 nextMove = AirAccelerate(wishDir, prevMove, wishSpeed, this.airAccel);
        nextMove.y = this.moveVec.y;
        nextMove += Physics.gravity * this.gravityMultiplier * Time.deltaTime;

        this.moveVec = nextMove;
    }

    Vector3 AirAccelerate(Vector3 wishDir, Vector3 prevVelocity, float wishSpeed, float accel)
    {
        if (wishSpeed > this.maxSpeedAir)
            wishSpeed = this.maxSpeedAir;

        float currentSpeed = Vector3.Dot(prevVelocity, wishDir);
        float addSpeed = wishSpeed - currentSpeed;

        if (addSpeed <= 0)
            return prevVelocity;

        float accelSpeed = accel * wishSpeed * Time.deltaTime;

        if (accelSpeed > addSpeed)
            accelSpeed = addSpeed;

        return prevVelocity + wishDir * accelSpeed;
    }

    public void SetFriction(int i)
    {
        if (i >= 0 && i < this.frictionConstants.Length)
            this.friction = this.frictionConstants[i];
    }

    void SetDefaultState()
    {
        this.friction = this.frictionConstants[0];
        this.inputVec = Vector3.zero;
        this.moveVec = Vector3.zero;
        this.jump = false;
        this.isJumping = false;
        this.previouslyGrounded = false;
    }
}
