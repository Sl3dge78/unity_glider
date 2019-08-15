using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arcade_Glider : MonoBehaviour
{

    public bool controls_active = true;

    [Header("Aerodynamics")]
    private float lift = 100000;
    public float lift_factor = 1;
    public float air_speed = 100;
    public float stall_speed = 30;

    private float angle_of_attack;

    public float bank_speed = 1.2f;

    public float inertia = 5;

    private Rigidbody rigidbody;

    [Header("Instruments")]
    public float vertical_speed;
    public float ground_altitude;
    public float altitude;
    //public float air_speed;

    private void Start() {
        rigidbody = gameObject.GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        rigidbody.AddRelativeForce(Vector3.forward * 30, ForceMode.VelocityChange);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Get input
        if (Application.isFocused && controls_active) { 
            rigidbody.AddRelativeTorque(Input.GetAxis("Pitch"), 0, 0);
            rigidbody.AddRelativeTorque(0, 0, -Input.GetAxis("Roll") * bank_speed);
        }

        // Bank rotation
        float zRot = Mathf.Sin(rigidbody.rotation.eulerAngles.z * Mathf.Deg2Rad);
        rigidbody.AddRelativeTorque(0, -zRot * Time.deltaTime * 100, 0);
        // Apply bank + go back to 0 on Z
        rigidbody.rotation = Quaternion.Lerp(rigidbody.rotation, Quaternion.Euler(rigidbody.rotation.eulerAngles.x, rigidbody.rotation.eulerAngles.y, 0), Time.deltaTime * 2f);
        
        Vector3 directional_vel = Quaternion.LookRotation(rigidbody.velocity) * Vector3.up;
        angle_of_attack = Vector3.Dot(transform.forward, directional_vel);
        air_speed = rigidbody.velocity.magnitude;

        lift = lift_factor * air_speed*air_speed * angle_of_attack;
        var force = (transform.up * lift + transform.forward * air_speed);
        force *= Time.deltaTime;

        // Move velocity towarsd front slowly if we have lift
        //if (lift < 0 || lift > (rigidbody.mass * 9.81 * 9.81)/2)
            rigidbody.velocity = Vector3.Lerp(rigidbody.velocity, transform.forward * rigidbody.velocity.magnitude, Time.deltaTime);
        rigidbody.AddForce(force);

        update_instruments();

        Debug.DrawRay(transform.position, rigidbody.velocity * 10, Color.blue);
    }
    /*
    private Quaternion bank_rotation(Quaternion current_rotation) {
        Quaternion ang_vel = Quaternion.identity;

       
        float prevX = current_rotation.eulerAngles.x;
        Vector3 rot = new Vector3(0, -zRot, -zRot) * Time.deltaTime;

        //Apply the new rotation 
        ang_vel.eulerAngles = rot;
        ang_vel = current_rotation * ang_vel;
        ang_vel.eulerAngles = new Vector3(prevX, ang_vel.eulerAngles.y, ang_vel.eulerAngles.z);

        return ang_vel;
    }
    */

    private void update_instruments() {
        vertical_speed = rigidbody.velocity.y;
        altitude = transform.position.y;
        //air_speed = rigidbody.velocity.magnitude * 3.6f;
        RaycastHit hit;

        if (Physics.Raycast(transform.position, Vector3.down, out hit, 10000)) {
            Debug.DrawRay(transform.position, Vector3.down * 10000);
            ground_altitude = hit.distance;
        }
    }

    void OnGUI() {

        var vertical_speed = rigidbody.velocity.y;
        var altitude = transform.position.y;
        //var air_speed = rigidbody.velocity.magnitude * 3.6f;
        GUI.Box(new Rect(5, 5, 200, 200), "");
        GUILayout.BeginArea(new Rect(10, 10, Screen.width - 5, Screen.width - 5));
        GUILayout.BeginVertical();
        GUILayout.Label("Vert speed = " + vertical_speed);
        GUILayout.Space(5);
        GUILayout.Label("Alt : " + altitude);
        GUILayout.Space(5);
        GUILayout.Label("Air Speed : " + air_speed * 3.6f);
        GUILayout.Space(5);
        GUILayout.Label("Gnd alt : " + ground_altitude);
        GUILayout.Space(5);
        GUILayout.Label("AoA: " + angle_of_attack);
        GUILayout.Space(5);
        GUILayout.Label("Lift: " + lift);
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

}
