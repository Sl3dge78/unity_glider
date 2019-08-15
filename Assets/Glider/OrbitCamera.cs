using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitCamera : MonoBehaviour
{

    public Arcade_Glider target;
    public float rotation_sensitivity = 100;
    public float distance;
    private float theta, phi;
    public float inertia;
    public float y_offset;

    // Update is called once per frame
    void FixedUpdate()
    {

        if(Input.GetMouseButtonDown(2)) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            target.controls_active = false;
        }

        if (Input.GetMouseButtonUp(2)) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            target.controls_active = true;
        }

        distance -= Input.GetAxis("Mouse ScrollWheel");

        if (Input.GetMouseButton(2)) {
            phi += (Input.GetAxis("Pitch") * rotation_sensitivity * Time.deltaTime);
            theta += (Input.GetAxis("Roll") * rotation_sensitivity * Time.deltaTime);
            transform.position = target.transform.position + Quaternion.Euler(phi, theta, 0) * target.transform.forward * -distance + target.transform.up * Mathf.Sin(y_offset * Mathf.Deg2Rad) * distance;
        } else {
            phi = 0; theta = 0;
            transform.position = Vector3.Lerp(transform.position, target.transform.position + target.transform.forward * -distance + target.transform.up * Mathf.Sin(y_offset * Mathf.Deg2Rad) * distance, Time.deltaTime * inertia);
        }

        transform.localRotation = Quaternion.LookRotation(target.transform.position - transform.position);

    }
}
