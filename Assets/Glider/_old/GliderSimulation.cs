using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GliderSimulation : MonoBehaviour {
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

    // Set by wind generator
    public float updraft_amount;
    public float updraft_mult;
    public float updraft_height;

    // Instrumentation
    public float vertical_speed;
    public float ground_altitude;
    public float altitude;
    public float air_speed;

    void Awake() {
        rigidbody = gameObject.GetComponent<Rigidbody>();
        
        wing_area = wing_chord * wing_span;
        wing_ratio = (wing_span * wing_span) / wing_area;
        rigidbody.drag = Mathf.Epsilon;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }
    private void Start() {
        Input.GetAxis("Pitch");
        Input.GetAxis("Roll");
        rigidbody.AddRelativeForce(initial_speed, ForceMode.VelocityChange);
    }

    // Update is called once per frame
    void FixedUpdate() {

        //rigidbody.rotation = Quaternion.Lerp(rigidbody.rotation, Quaternion.LookRotation(rigidbody.velocity, transform.up), Time.deltaTime);
        if (Cursor.lockState == CursorLockMode.Locked) {
            rigidbody.AddRelativeTorque(Input.GetAxis("Pitch"), 0, 0);
            rigidbody.AddRelativeTorque(0, 0, -Input.GetAxis("Roll") * bank_speed);
        }
        doStandardPhysics();


        /*
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
 
            var induced_lift = angle_of_attack * (wing_ratio / (wing_ratio + 2f)) * 2f * Mathf.PI;
            var induced_drag = (induced_lift * induced_lift) / (wing_ratio * Mathf.PI);

            var pressure = rigidbody.velocity.sqrMagnitude * 1.2754f * 0.5f * wing_area;

            var lift = induced_lift * pressure;
            //var drag = (0.021f) * pressure;
            var drag = (0.021f + induced_drag) * pressure;

            var drag_direction = -rigidbody.velocity.normalized;
            var liftDirection = Vector3.Cross(drag_direction, -transform.right);

            

            rigidbody.AddForce(liftDirection * lift + drag_direction * drag);
            
        }
        */
        vertical_speed = rigidbody.velocity.y;
        altitude = transform.position.y;
        air_speed = rigidbody.velocity.magnitude * 3.6f;
        RaycastHit hit;

        if (Physics.Raycast(transform.position, Vector3.down, out hit, 10000)) {
            Debug.DrawRay(transform.position, Vector3.down * 10000);
            ground_altitude = hit.distance;
        }

    }


    public void doStandardPhysics() {
        //rigidbody.rotation = bank_rotation(rigidbody.rotation);
        float zRot = Mathf.Sin(rigidbody.rotation.eulerAngles.z * Mathf.Deg2Rad);
        rigidbody.AddRelativeTorque(0, -zRot * Time.deltaTime * 100, 0);
        rigidbody.rotation = Quaternion.Lerp(rigidbody.rotation, Quaternion.Euler(rigidbody.rotation.eulerAngles.x, rigidbody.rotation.eulerAngles.y, 0), Time.deltaTime * 2f);

        rigidbody.velocity = Vector3.Lerp(rigidbody.velocity,
                                           (rigidbody.rotation * Vector3.forward).normalized * rigidbody.velocity.magnitude,
                                           Time.deltaTime);

        Vector3 directional_vel = Quaternion.LookRotation(rigidbody.velocity) * Vector3.up;
        var angle_of_attack = Mathf.Asin(Vector3.Dot(transform.forward, directional_vel)) * Mathf.Rad2Deg;

        if (rigidbody.velocity != Vector3.zero) {
            // LIFT
            var lift_coefficient = 2 * Mathf.PI * angle_of_attack * Mathf.Deg2Rad;
            var lift = rigidbody.velocity.sqrMagnitude * air_pressure * wing_area * lift_coefficient * Time.deltaTime;
            var lift_direction = Quaternion.LookRotation(rigidbody.velocity) * Vector3.up;
            rigidbody.AddForce(lift_direction * lift);

            Debug.DrawRay(transform.position, lift_direction * lift * 10, Color.green);

            // DRAG
            var dragCoefficient = .0039f * angle_of_attack * angle_of_attack + .025f;
            var liftInducedDrag = (lift * lift) / (.5f * air_pressure * rigidbody.velocity.sqrMagnitude * wing_area * Mathf.PI * .9f * wing_ratio);
            var formDrag = .5f * air_pressure * rigidbody.velocity.sqrMagnitude * wing_area * dragCoefficient * Time.deltaTime;
            var drag = liftInducedDrag + formDrag;
            var drag_direction = Quaternion.LookRotation(rigidbody.velocity) * Vector3.back;
            rigidbody.AddForce(drag_direction * drag);

            Debug.DrawRay(transform.position, drag_direction * drag * 10, Color.red);

            Debug.DrawRay(transform.position, rigidbody.velocity * 10, Color.blue);

        }
    }

    public Quaternion bank_rotation(Quaternion theCurrentRotation) {
        //Quaternion getBankedTurnRotation(float curZRot, float curLift, float curVel, float mass) {
        // The physics of a banked turn is as follows
        //  L * Sin(0) = M * V^2 / r
        // 
        //	L is the lift acting on the aircraft
        //	θ0 is the angle of bank of the aircraft
        //	m is the mass of the aircraft
        //	v is the true airspeed of the aircraft
        //	r is the radius of the turn	
        //
        // Currently, we'll keep turn rotation simple. The following is not based on the above, but it provides
        // A pretty snappy mechanism for getting the job done.
        //Apply Yaw rotations. Yaw rotation is only applied if we have angular roll. (roll is applied directly by the 
        //player)
        Quaternion angVel = Quaternion.identity;
        //Get the current amount of Roll, it will determine how much yaw we apply.
        float zRot = Mathf.Sin(theCurrentRotation.eulerAngles.z * Mathf.Deg2Rad) * Mathf.Rad2Deg;
        //We don't want to change the pitch in turns, so we'll preserve this value.
        float prevX = theCurrentRotation.eulerAngles.x;
        //Calculate the new rotation. The constants determine how fast we will turn.
        Vector3 rot = new Vector3(0, -zRot * 0.8f, -zRot * 0.5f) * Time.deltaTime;

        //Apply the new rotation 
        angVel.eulerAngles = rot;
        angVel *= theCurrentRotation;
        angVel.eulerAngles = new Vector3(prevX, angVel.eulerAngles.y, angVel.eulerAngles.z);

        //Done!
        return angVel;
    }


    void OnGUI() {
        GUILayout.BeginArea(new Rect(5, 5, Screen.width - 5, Screen.width - 5));
        GUILayout.BeginVertical();
        GUILayout.Label("Vert speed = " + vertical_speed);
        GUILayout.Space(5);
        GUILayout.Label("Alt : " + altitude);
        GUILayout.Space(5);
        GUILayout.Label("Air Speed : " + air_speed);
        GUILayout.Space(5);
        GUILayout.Label("Gnd alt : " + ground_altitude);
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