using System;
using UnityEngine;

public class Jump : MonoBehaviour
{
    public float jumpForce;
    
    private InputSystem_Actions controls;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        controls = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        controls.Enable();
        controls.Player.Jump.performed += ctx => GoUp();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void GoUp()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }
}
