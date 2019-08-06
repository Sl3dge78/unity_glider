using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Glider : MonoBehaviour {
    new Rigidbody rigidbody;

    public float air_pressure = 1.225f;

    public float stall_angle = 20;
    public float lift_coeff_offset = 0.4f;

    public float wing_chord = 1.0558f;
    public float wing_span = 17.0f;

    public float wing_area;
    private float wing_ratio;

    public float bank_speed;

    public Vector3 initial_speed;

    private float drag = 0;

    void Awake() {
        rigidbody = gameObject.GetComponent<Rigidbody>();
        rigidbody.AddRelativeForce(initial_speed, ForceMode.VelocityChange);
        wing_area = wing_chord * wing_span;
        wing_ratio = (wing_span * wing_span) / wing_area;
        rigidbody.drag = Mathf.Epsilon;
    }

    // Update is called once per frame
    void FixedUpdate() {

        rigidbody.rotation = Quaternion.Lerp(rigidbody.rotation, Quaternion.LookRotation(rigidbody.velocity, transform.up), Time.deltaTime);
        rigidbody.AddRelativeTorque(Input.GetAxis("Pitch"), 0, 0);
        rigidbody.AddRelativeTorque(0, 0, -Input.GetAxis("Roll")*bank_speed);


        // Banking turn
        Quaternion angVel = Quaternion.identity;
        float zRot = Mathf.Sin(rigidbody.rotation.eulerAngles.z * Mathf.Deg2Rad) * Mathf.Rad2Deg;
        float prevX = rigidbody.rotation.eulerAngles.x;
        Vector3 rot = new Vector3(0, -zRot * 0.8f, -zRot * 0.5f) * Time.deltaTime;
        angVel.eulerAngles = rot;
        angVel *= rigidbody.rotation;
        angVel.eulerAngles = new Vector3(prevX, angVel.eulerAngles.y, angVel.eulerAngles.z);

        rigidbody.rotation = angVel;

        // LIFT and drag
        // https://stackoverflow.com/questions/49716989/unity-aircraft-physics
        if (rigidbody.velocity.magnitude > 0) {

            var local_velocity = transform.InverseTransformDirection(rigidbody.velocity);
            var angle_of_attack = Mathf.Atan2(-local_velocity.y, local_velocity.z);

            // float gravity = rigidbody.mass * 9.81f;
            // rigidbody.AddForce(0, -gravity, 0);

            var induced_lift = angle_of_attack * (wing_ratio / (wing_ratio + 2f)) * 2f * Mathf.PI;
            var induced_drag = (induced_lift * induced_lift) / (wing_ratio * Mathf.PI);

            var pressure = rigidbody.velocity.sqrMagnitude * 1.2754f * 0.5f * wing_area;

            var lift = induced_lift * pressure;
            var drag = (0.021f + induced_drag) * pressure;

            var drag_direction = -rigidbody.velocity.normalized;
            var liftDirection = Vector3.Cross(rigidbody.velocity.normalized, transform.right);

            rigidbody.AddForce(liftDirection * lift + drag_direction * drag);
        }
    }

    void OnGUI() {
        GUILayout.BeginArea(new Rect(5, 5, Screen.width - 5, Screen.width - 5));
        GUILayout.BeginVertical();
        GUILayout.Label("Vert speed = " + rigidbody.velocity.y);
        GUILayout.Space(5);
        GUILayout.Label("Alt : " + transform.position.y);
        GUILayout.Space(5);
        GUILayout.Label("Air Speed : " + rigidbody.velocity.magnitude*3.6f);
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    float get_lift_coeff(float angle_of_attack) {
        if (angle_of_attack >= stall_angle)
            return 0;
        else
            return lift_coeff_offset + 2 * Mathf.PI * angle_of_attack * Mathf.Deg2Rad;
    }

    float get_drag_coeff(float angle_of_attack) {
        //return 0.00039f * angle_of_attack * angle_of_attack + 0.025f;
        return 0.1f;
    }

}