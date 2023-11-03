using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    float speed;
    public float walkSpeed;
    public float sprintSpeed;
    public float crouchSpeed;
    public float sideMoveMultipliter;
    public Transform groundCheck;
    float groundRadius;
    public float standingGroundRadius = 0.14f;
    public float crouchingGroundRadius;
    public LayerMask ground;
    CharacterController controller;
    public Transform orientation;
    public float jumpCooldown = 1.0f;
    float jumpCooldownTimer = 0.0f;
    public float jumpForce = 5.0f;
    bool canJump = false;
    float yVel;
    public float gravity = 2.84f;
    float startHeight;
    public float crouchSlideHeight;
    Transform groundCheckPos;
    public Transform slideCrouchGroundCheckPos;
    public float speedBoostSlide;
    public float speedSlideAdd;
    bool isSliding = false;
    public float maxSlideSpeed;
    public float slidingFriction;
    public float jumpSlideBoost;
    public float overSpeedLimitFriction;
    public float overSpeedLimitFrictionCrouched;
    public float minimumSlideSpeed;
    bool requestingUnCrouch = false;
    public float underObjectCrouchedCheckLength;
    bool underObjectCrouched = false;
    public Transform underObjectCheck;
    public float underObjectCheckLength;
    bool underObject = false;
    string moveMode = "walking";
    bool boosted = false;
    public LayerMask wall;
    public float wallRunSpeed;
    public float wallRunTime;
    private float wallRunTimer;
    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft;
    private bool wallRight;
    bool wallJumped = false;
    public float wallJumpForceUp;
    public float wallJumpForceHorizontal;
    Vector3 horizontalForce;
    public float wallJumpWeakness;
    bool isGrounded;
    bool exitingWall;
    float exitWallTimer;
    public float exitWallTime;
    bool setTimerWallRun = false;
    CameraLook playerCam;

    // Start is called before the first frame update
    void Start()
    {
        speed = walkSpeed;
        controller = GetComponent<CharacterController>();
        groundCheckPos = groundCheck;
        startHeight = controller.height;
        playerCam = GameObject.FindFirstObjectByType<CameraLook>();
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, ground);
    }

    public void Die()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundRadius, ground);
        if(moveMode == "crouching")
        {
            groundRadius = crouchingGroundRadius;
            underObjectCrouched = Physics.Raycast(this.transform.position, Vector3.up, controller.height + underObjectCrouchedCheckLength, ground);
        }
        else
        {
            groundRadius = standingGroundRadius;
            underObjectCrouched = false;
        }
        underObject = Physics.Raycast(underObjectCheck.position, Vector3.up, underObjectCheckLength, ground);
        Physics.Raycast(groundCheck.position, Vector3.down, out var hitInfo, groundRadius);

        horizontalForce = Vector3.Lerp(horizontalForce, Vector3.zero, wallJumpWeakness  * Time.deltaTime);

        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, wall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, wall);

        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");

        if(moveMode != "wallRunning")
        {
            setTimerWallRun = false;
            playerCam.DoFOV(70f);
            playerCam.DoTilt(0f);
        }

        if(hitInfo.collider != null)
        {
            if (isGrounded && hitInfo.collider.gameObject.tag == "Lava")
            {
                Die();
            }
        }

        CrouchHandler();
        SprintHandler();
        WallRunHandler();

        if(hitInfo.collider != null)
        {
            if (hitInfo.collider.gameObject.tag == "Jump Pad" && isGrounded && !boosted)
            {
                yVel = hitInfo.collider.gameObject.GetComponent<JumpPad>().jumpForce;
                boosted = true;
            }
        }
        if(boosted == true && !isGrounded)
        {
            boosted = false;
        }
        if (wallJumped && !wallLeft && !wallRight && exitingWall == false)
        {
            wallJumped = false;
        }

        if (isGrounded)
        {
            horizontalForce = Vector3.zero;
        }

        // Moving the character
        if (!isSliding && (moveMode == "crouching" || moveMode == "walking" || moveMode == "sprinting"))
        {
            controller.Move(speed * Time.deltaTime * vertical * orientation.forward + horizontal * sideMoveMultipliter * speed * Time.deltaTime * orientation.right + Time.deltaTime * yVel * Vector3.up + horizontalForce * Time.deltaTime);
        }
        if (moveMode == "sprinting" && speed >= sprintSpeed && isGrounded)
        {
            speed -= overSpeedLimitFriction * Time.deltaTime;
            speed = Mathf.Clamp(speed, sprintSpeed, speed);
        }
        else if (moveMode == "walking" && speed >= crouchSpeed && isGrounded)
        {
            speed -= overSpeedLimitFriction * Time.deltaTime;
            speed = Mathf.Clamp(speed, walkSpeed, speed);
        }
        else if (moveMode == "crouching" && speed >= crouchSpeed && !isSliding && isGrounded)
        {
            speed -= overSpeedLimitFrictionCrouched * Time.deltaTime;
            speed = Mathf.Clamp(speed, crouchSpeed, speed);
        }

        if(moveMode == "crouching" && yVel > 0 && underObjectCrouched)
        {
            yVel = 0;
        }
        else if(moveMode == "walking" && yVel > 0 && underObject || moveMode == "sprinting" && yVel > 0 && underObject)
        {
            yVel = 0;
        }

        JumpHandler();
    }

    void WallRunHandler()
    {
        if ((wallLeft || wallRight) && Input.GetAxis("Vertical") > 0 && AboveGround() && Input.GetKey(KeyCode.LeftShift) && !wallJumped && !exitingWall)
        {
            playerCam.DoFOV(80f);
            if (wallLeft) playerCam.DoTilt(-5f);
            else if (wallRight) playerCam.DoTilt(5f);
            moveMode = "wallRunning";
            speed = wallRunSpeed;

            if (!setTimerWallRun)
            {
                wallRunTimer = wallRunTime;
                setTimerWallRun = true;
            }

            horizontalForce = Vector3.zero;

            Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

            Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

            if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
            {
                wallForward = -wallForward;
            }

            if (wallRunTimer > 0)
            {
                wallRunTimer -= Time.deltaTime;
            }
            if (wallRunTimer <= 0 && moveMode == "wallRunning")
            {
                exitingWall = true;
                exitWallTimer = exitWallTime;
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    moveMode = "sprinting";
                }
                else
                {
                    moveMode = "walking";
                }
            }

            controller.Move(wallForward * speed * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                jumpCooldownTimer = 0.0f;
                wallJumped = true;
                exitingWall = true;
                exitWallTimer = exitWallTime;
                yVel = wallJumpForceUp;
                horizontalForce = wallNormal * wallJumpForceHorizontal;
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    moveMode = "sprinting";
                }
                else
                {
                    moveMode = "walking";
                }
            }
        }
        else if (moveMode == "wallRunning")
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                moveMode = "sprinting";
            }
            else
            {
                moveMode = "walking";
            }
        }
        if (exitingWall)
        {
            playerCam.DoFOV(70f);
            playerCam.DoTilt(0f);
            if (moveMode == "wallRunning")
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    moveMode = "sprinting";
                }
                else
                {
                    moveMode = "walking";
                }
            }

            if (exitWallTimer > 0)
            {
                exitWallTimer -= Time.deltaTime;
            }

            if (exitWallTimer <= 0)
            {
                exitingWall = false;
            }
        }
        
    }

    void SprintHandler()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && moveMode != "crouching")
        {
            moveMode = "sprinting";
            if (isGrounded)
            {
                speed = sprintSpeed;
            }
        }
        if (Input.GetKeyUp(KeyCode.LeftShift) && moveMode == "sprinting")
        {
            moveMode = "walking";
            if (isGrounded)
            {
                speed = walkSpeed;
            }
        }
    }

    void CrouchHandler()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) && moveMode != "wallRunning")
        {
            requestingUnCrouch = false;
            if (!Input.GetKey(KeyCode.LeftShift) || Input.GetAxis("Vertical") == 0 || controller.velocity.sqrMagnitude < minimumSlideSpeed)
            {
                controller.height = crouchSlideHeight;
                groundCheck = slideCrouchGroundCheckPos;
                moveMode = "crouching";
                speed = crouchSpeed;
            }
            else
            {
                moveMode = "crouching";
                controller.height = crouchSlideHeight;
                groundCheck = slideCrouchGroundCheckPos;
                controller.Move(orientation.forward * (speedBoostSlide * Time.deltaTime));
                isSliding = true;
            }
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            isSliding = false;
        }
        if (isSliding == true)
        {
            if (controller.velocity.y < 0)
            {
                controller.Move(orientation.forward * speed * Time.deltaTime + Vector3.up * yVel * Time.deltaTime + orientation.right * speed * sideMoveMultipliter * Input.GetAxis("Horizontal") * Time.deltaTime + horizontalForce * Time.deltaTime);
                speed += speedSlideAdd * Time.deltaTime;
                speed = Mathf.Clamp(speed, Mathf.NegativeInfinity, maxSlideSpeed);
            }
            else
            {
                controller.Move(orientation.forward * speed * Time.deltaTime + orientation.right * speed * sideMoveMultipliter * Input.GetAxis("Horizontal") * Time.deltaTime + Vector3.up * yVel * Time.deltaTime + horizontalForce * Time.deltaTime);
                speed -= slidingFriction * Time.deltaTime;
                speed = Mathf.Clamp(speed, 0, Mathf.Infinity);
                if (speed <= 0)
                {
                    isSliding = false;
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        requestingUnCrouch = false;
                        moveMode = "crouching";
                        speed = crouchSpeed;
                    }
                    else
                    {
                        requestingUnCrouch = true;
                    }
                }
            }
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            requestingUnCrouch = true;
            isSliding = false;
        }
        if (requestingUnCrouch)
        {
            if (!underObjectCrouched && controller.velocity.y == 0)
            {
                isSliding = false;
                requestingUnCrouch = false;
                groundCheck = groundCheckPos;
                controller.height = startHeight;
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    moveMode = "sprinting";
                    if (isGrounded)
                    {
                        speed = sprintSpeed;
                    }
                }
                else
                {
                    moveMode = "walking";
                    if (isGrounded)
                    {
                        speed = walkSpeed;
                    }
                }
            }
        }
    }

    void JumpHandler()
    {
        if (isGrounded && jumpCooldownTimer >= jumpCooldown && !boosted)
        {
            yVel = 0;
            canJump = true;
        }
        else
        {
            canJump = false;
        }
        if(jumpCooldownTimer <= jumpCooldown)
        {
            jumpCooldownTimer += Time.deltaTime;
            if(jumpCooldownTimer > jumpCooldown)
            {
                canJump = false;
            }
        }
        if (Input.GetKeyDown(KeyCode.Space) && canJump)
        {
            jumpCooldownTimer = 0.0f;
            yVel = jumpForce;
            if (isSliding && !underObjectCrouched)
            {
                speed += (controller.velocity.sqrMagnitude / 100) * jumpSlideBoost * Time.deltaTime;
                speed = Mathf.Clamp(speed, 0.0f, maxSlideSpeed + jumpSlideBoost);
                isSliding = false;
                groundCheck = groundCheckPos;
                controller.height = startHeight;
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    moveMode = "sprinting";
                }
                else
                {
                    moveMode = "walking";
                }
            }
        }
        else
        {
            if (!isGrounded && moveMode != "wallRunning")
            {
                yVel -= gravity * Time.deltaTime;
            }
        }
    }
}