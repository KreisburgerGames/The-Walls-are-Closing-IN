using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClosingWall : MonoBehaviour
{
    public Vector3 wallDir;
    public float wallSpeed;
    Rigidbody rb;
    [HideInInspector] public bool closing;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        wallDir = wallDir.normalized;
    }

    // Update is called once per frame
    void Update()
    {
        if (closing)
        {
            rb.velocity = wallDir * wallSpeed * Time.deltaTime;
        }
        else
        {
            rb.velocity = Vector3.zero;
        }
    }
}
