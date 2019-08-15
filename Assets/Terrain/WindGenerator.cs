using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindGenerator : MonoBehaviour
{
    [Header("Generation")]
    public Vector2 wind_direction;
    public float gradient_threshold = 0.05f;
    public int definition = 10;

    float[] updraft_map;
    Texture2D drafts_map_texture;
    bool drafts_texture_generated = false;

    [Header("Gameplay")]
    public Arcade_Glider glider;
    private Rigidbody glider_rb;
    public float wind_intensity;
    public float updraft_height = 1000;

    private float map_width;

    private float val;

    private void Start() {
        map_width = GameObject.FindObjectOfType<TerrainGenerator>().heightmap_node_count;
        glider_rb = glider.GetComponent<Rigidbody>();
    }

    public void GenerateUpdrafts(float[] map, int map_size) {
        wind_direction.Normalize();
        int draft_map_size = map_size / definition;
        updraft_map = new float[draft_map_size * draft_map_size];

        float min_value = 0, max_value = 0;

        for (int y = 0; y < map_size; y += definition) {
            for (int x = 0; x < map_size; x += definition) {
                int i = y * map_size + x;
                float here_height = map[i];
                int next_x = (int)(x + wind_direction.x * definition);
                int next_y = (int)(y + wind_direction.y * definition);

                int i2 = next_y * map_size + next_x;
                if (i2 > map.Length)
                    continue;

                float dest_height = map[i2];

                float gradient = dest_height - here_height;
                if (gradient > gradient_threshold) {
                    int j = (y / definition) * draft_map_size + (x / definition);

                    if (j >= updraft_map.Length)
                        continue;

                    updraft_map[j] = Mathf.Max(gradient-gradient_threshold, 0);
                    min_value = Mathf.Min(gradient, min_value);
                    max_value = Mathf.Max(gradient, max_value);
                }


            }
        }

        // Normalize
        if (max_value != min_value) {
            for (int i = 0; i < updraft_map.Length; i++) {
                updraft_map[i] = (updraft_map[i] - min_value) / (max_value - min_value);
            }
        }


        drafts_map_texture = new Texture2D(draft_map_size, draft_map_size);
        for (int y = 0; y < draft_map_size; y++) {
            for (int x = 0; x < draft_map_size; x++) {
                float val = updraft_map[y * draft_map_size + x];
                drafts_map_texture.SetPixel(x, y, new Color(val, val, val));


            }
        }
        drafts_texture_generated = true;
        drafts_map_texture.Apply();
    }

    private void FixedUpdate() {
        int glider_x = (int)(glider.transform.position.x / map_width * definition);
        int glider_z = (int)(glider.transform.position.z / map_width * definition);

        val = updraft_map[glider_z * definition + glider_x] * wind_intensity * 150;
        val *= 1-(glider.ground_altitude / updraft_height);
        if (val > 0) {
            glider_rb.AddForce(0, val, 0);
        }
    }

    private void OnGUI() {
        GUI.DrawTexture(new Rect(Screen.width - 150, Screen.height - 150, 150, 150), drafts_map_texture);

        GUI.Label(new Rect(Screen.width - 150, Screen.height - 140, 150, 150), val.ToString());
    }
}
