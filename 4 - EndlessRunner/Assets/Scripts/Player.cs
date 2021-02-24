using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private Manager GameManager;
    [SerializeField] private float ForwardSpeed;
    [SerializeField] private float SideSpeed;


    // Update is called once per frame
    void Update()
    {
        if (GameManager.IsGameRunning)
        {
            var moveTo = new Vector3(0, 0, -ForwardSpeed);
            if (Input.GetKey("left"))
            {
                moveTo.x = SideSpeed;
            }
            if (Input.GetKey("right"))
            {
                moveTo.x = -SideSpeed;
            }

            transform.position = Vector3.Lerp(transform.position, transform.position + moveTo, Time.deltaTime);

            if (transform.position.y < -1f)
            {
                GameManager.GameOver();
            }
        }
    }
}
