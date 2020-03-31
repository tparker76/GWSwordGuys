using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    //Player number
    public string playerNumber;

    //Color
    SpriteRenderer sr;
    Color defaultColor;

    // Physics
    Rigidbody2D rb;
    BoxCollider2D col;
    float distanceToGround; // Distance from center of transform to bottom of collider
    public LayerMask groundLayer; // All platforms that count as ground belong to this layer

    // Movement variables
    public int speed;
    public int jumpForce;
    Vector2 movement;
    string lastDirection = "right"; //Last direction player tried to move

    // Ability variables
    bool canDash = true;
    bool dashCoolingDown = false;
    bool initiateDash = false;
    Vector2 dashDirection;
    public float dashCooldown;
    public float dashDuration;
    public float dashSpeed;
    bool canParry = true;
    bool initiateParry = false;
    public float parryCooldown;
    public float parryDuration;

    // Player input
    float xAxis;
    float yAxis;
    bool jumpButton;
    bool dashButton;
    bool parryButton;

    // Player states
    bool onGround = false;
    bool dashing = false;
    bool parrying = false;

    void Start()
    {
        // Assinging rb to player rigidbody
        rb = GetComponent<Rigidbody2D>();
        // Assinging col to player box collider
        col = GetComponent<BoxCollider2D>();
        // Assinging sr to player sprite renderer
        sr = GetComponent<SpriteRenderer>();
        //Set distance to ground to be half the player hitbox;
        distanceToGround = col.bounds.extents.y;
        //Set defaultColor to original color
        defaultColor = sr.color;
    }

    void Update()
    {
        // Input
        xAxis = Input.GetAxis("P" + playerNumber + " Horizontal");
        yAxis = Input.GetAxis("P" + playerNumber + " Vertical");
        jumpButton = Input.GetButton("P" + playerNumber + " Jump");
        dashButton = Input.GetButton("P" + playerNumber + " Dash");
        parryButton = Input.GetButton("P" + playerNumber + " Parry");

        // Get the player facing the proper direction
        if(movement.x < 0)
        {
            gameObject.transform.localScale = new Vector3(-1, 1, 1);
        }
        else if(movement.x > 0)
        {
            gameObject.transform.localScale = new Vector3(1, 1, 1);
        }

        // Get last direction player moved and store it in lastDirection string
        GetLastDirection();

        // Test if ground is under player using a raycast (object must be on groundLayer)
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, distanceToGround + .1f, groundLayer);
        if (hit.collider != null)
        {
            onGround = true;
        }
        else
        {
            onGround = false;
        }

        // Check if trying to parry
        if (dashButton && canDash)
        {
            initiateDash = true;
        }

        // Check if trying to parry
        if (parryButton && canParry)
        {
            initiateParry = true;
        }
    }

    void FixedUpdate()
    {
        if (!dashing && !parrying)
        {
            // Set temp Vector2 to current velocity
            movement = rb.velocity;

            // Apply horizontal axis to temp Vector2
            movement.x = xAxis * speed;

            // Apply temp Vector2 to player
            rb.velocity = movement;

            // Apply jump force to rigidbody if jumping
            if (onGround && jumpButton)
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                onGround = false;
            }
        }

        if (initiateParry)
        {
            StartCoroutine(Parry());
            initiateParry = false;
        }
        else if(initiateDash)
        {
            StartCoroutine(Dash());
            initiateDash = false;
        }
    }

    void GetLastDirection() //Because it's on an arcade machine with a joystick, there will never be more than 1 direction input
    {
        if(xAxis == 1) //Right
        {
            lastDirection = "right";
        }
        else if(xAxis == -1) //Left
        {
            lastDirection = "left";
        }
        else if (yAxis == 1) //Up
        {
            lastDirection = "up";
        }
        else if (yAxis == -1) //Down
        {
            lastDirection = "down";
        }
    }

    public string GetState()
    {
        if(parrying)
        {
            return "parrying";
        }
        else if(dashing)
        {
            return "dashing";
        }
        else
        {
            return "default";
        }
    }

    public void BounceOffPlayer()
    {
        movement = dashDirection;
        movement = -movement * 15;
        rb.velocity = movement;
        //dashing = false;
    }

    IEnumerator Dash()
    {
        dashing = true;
        canDash = false;
        // Set rb gravity to 0
        rb.gravityScale = 0;

        //Set temp Vector2 to direction of dash
        if (lastDirection == "up")
        {
            movement = new Vector2(0, 1);
            dashDirection = movement;
        }
        else if (lastDirection == "down")
        {
            movement = new Vector2(0, -1);
            dashDirection = movement;
        }
        else if (lastDirection == "left")
        {
            movement = new Vector2(-1, 0);
            dashDirection = movement;
        }
        else if (lastDirection == "right")
        {
            movement = new Vector2(1, 0);
            dashDirection = movement;
        }

        // Apply speed to temp Vector2
        movement = movement * dashSpeed;

        // Apply temp Vector2 to player;
        rb.velocity = movement;

        //Change to dash color
        sr.color = new Color(0, 1, 0);

        yield return new WaitForSeconds(dashDuration);
        
        //If the dash wasn't interupted by a parry, stop the dash motion and set dashing to false
        if (!parrying)
        {
            StartCoroutine(DashCooldown());

            dashing = false;
            sr.color = defaultColor;

            //Clear y velocity
            movement.y = 0;
            rb.velocity = movement;

            // Set rb gravity to original (11)
            rb.gravityScale = 11;
        }
    }

    IEnumerator DashCooldown()
    {
        // Dash cooldown
        dashCoolingDown = true;
        yield return new WaitForSeconds(dashCooldown);

        dashCoolingDown = false;
        canDash = true;
    }

    IEnumerator Parry()
    {
        parrying = true;
        canDash = false;
        canParry = false;

        //If parry interrupted a dash, stop dash and start dash cooldown
        if(dashing)
        {
            dashing = false;
            StartCoroutine(DashCooldown());
        }

        // Set rb gravity to 0
        rb.gravityScale = 0;

        // Stop movement
        movement = Vector2.zero;

        rb.velocity = movement;
        // Set color to parry color
        sr.color = new Color(0.25f, 0.25f, 0.25f);

        yield return new WaitForSeconds(parryDuration);

        StartCoroutine(ParryCooldown());

        if (!dashCoolingDown)
        {
            canDash = true;
        }

        parrying = false;
        sr.color = defaultColor;

        // Set rb gravity to original (11)
        rb.gravityScale = 11;
    }

    IEnumerator ParryCooldown()
    {
        // Parry cooldown
        yield return new WaitForSeconds(parryCooldown);

        canParry = true;
    }

    void OnCollisionStay2D(Collision2D col)
    {
        
        if(col.gameObject.CompareTag("Player"))
        {
            //Get the state of the other player
            string otherState = col.gameObject.GetComponent<PlayerController>().GetState();

            if(parrying)
            {
                //If parrying and other player is attacking, kill them
                if(otherState == "dashing")
                {
                    Destroy(col.gameObject);
                    StartCoroutine(PlayerDeath());
                }
            }
            else if(dashing)
            {
                //If dashing and other is default, kill them
                if (otherState == "default")
                {
                    Destroy(col.gameObject);
                    StartCoroutine(PlayerDeath());
                }
                else if(otherState == "dashing")
                {
                    BounceOffPlayer();
                    col.gameObject.GetComponent<PlayerController>().BounceOffPlayer();
                }
            }
        }
    }

    IEnumerator PlayerDeath()
    {
        yield return new WaitForSeconds(1);
        SceneManager.LoadScene(0);
    }
}
