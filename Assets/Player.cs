using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public float interactionRange;
    public Camera camera;
    RaycastHit hit;
    PlayerMovement player;
    void Start()
    {
        player = GetComponent<PlayerMovement>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Wall")
        {
            player.Die();
        }
        if(other.gameObject.tag == "Wall Trigger")
        {
            foreach (ClosingWall wall in GameObject.FindObjectsOfType<ClosingWall>())
            {
                wall.closing = true;
            }
        }
        if (other.gameObject.tag == "Wall Stop")
        {
            foreach (ClosingWall wall in GameObject.FindObjectsOfType<ClosingWall>())
            {
                wall.closing = false;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        Physics.Raycast(camera.transform.position, camera.transform.forward, out hit, interactionRange);
        if(hit.collider != null)
        {
            if(hit.collider.gameObject.tag == "Interactable" && Input.GetMouseButtonDown(0))
            {
                hit.collider.gameObject.GetComponent<Interactable>().interaction.Invoke();
            }
        }
    }
}
