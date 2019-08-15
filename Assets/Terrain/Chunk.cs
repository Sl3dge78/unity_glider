using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Chunk : MonoBehaviour {
    //System.Random prng;

    //int currentSeed;
    //int currentErosionRadius;
    //int currentMapSize;

    //float[] map;
    Mesh mesh;
    Material material;

    MeshRenderer mesh_renderer;
    MeshFilter mesh_filter;
    MeshCollider mesh_collider;

    //WindGenerator wind_generator;

    //public bool update = false;

    private void Start() {
        if (!gameObject.GetComponent<MeshRenderer>()) {
            
        } else {
            mesh_renderer = gameObject.GetComponent<MeshRenderer>();
        }

        if (!gameObject.GetComponent<MeshFilter>()) {
            
        } else {
            mesh_filter = gameObject.GetComponent<MeshFilter>();
        }

        if (!gameObject.GetComponent<MeshCollider>()) {
            
        } else {
            mesh_collider = gameObject.GetComponent<MeshCollider>();
        }
    }

    /*
    [ExecuteInEditMode]
    public void GenerateMap() {
        if (!gameObject.GetComponent<MeshRenderer>()) {
            mesh_renderer = gameObject.AddComponent<MeshRenderer>();
        } else {
            mesh_renderer = gameObject.GetComponent<MeshRenderer>();
        }

        if (!gameObject.GetComponent<MeshFilter>()) {
            mesh_filter = gameObject.AddComponent<MeshFilter>();
        } else {
            mesh_filter = gameObject.GetComponent<MeshFilter>();
        }

        if (!gameObject.GetComponent<MeshCollider>()) {
            mesh_collider = gameObject.AddComponent<MeshCollider>();
        } else {
            mesh_collider = gameObject.GetComponent<MeshCollider>();
        }

        map = GenerateNoise(map_size);
        Erode(map, map_size, erosion_iterations);
        GenerateMesh();
        wind_generator = GameObject.FindObjectOfType<WindGenerator>();

        if (wind_generator)
            wind_generator.GenerateUpdrafts(map, map_size);
    }

    // Start is called before the first frame update
    void Update() {
        if (transform.hasChanged && update) {
            GenerateMap();
        }
    }
    */
    #region MESH

    public void GenerateMesh(float[] heightmap, int heightmap_node_count, Vector2Int offset, Material material, int node_count = 128, float scale = 2500, float elevation_scale = 1000) {
        Vector3[] verts = new Vector3[node_count * node_count];
        int[] triangles = new int[(node_count - 1) * (node_count - 1) * 6];
        Vector2[] uvs = new Vector2[verts.Length];
        int t = 0;

        for (int y = 0; y < node_count; y++) {
            for (int x = 0; x < node_count; x++) {
                int i = y * node_count + x;
                int map_position = (y + offset.y) * heightmap_node_count + x + offset.x;

                Vector2 percent = new Vector2(x / (node_count - 1f), y / (node_count - 1f));
                uvs[i] = percent;
                Vector3 pos = new Vector3(percent.x, 0, percent.y) * scale;
                pos += Vector3.up * heightmap[map_position] * elevation_scale;
                verts[i] = pos;

                // Construct triangles
                if (x != node_count - 1 && y != node_count - 1) {

                    triangles[t + 0] = i + node_count;
                    triangles[t + 1] = i + node_count + 1;
                    triangles[t + 2] = i;

                    triangles[t + 3] = i + node_count + 1;
                    triangles[t + 4] = i + 1;
                    triangles[t + 5] = i;
                    t += 6;
                }
            }
        }

        mesh = new Mesh();

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.uv = uvs;
        mesh.name = gameObject.name;

        mesh_renderer = gameObject.AddComponent<MeshRenderer>();
        mesh_filter = gameObject.AddComponent<MeshFilter>();
        mesh_collider = gameObject.AddComponent<MeshCollider>();

        mesh_filter.sharedMesh = mesh;
        mesh_collider.sharedMesh = mesh;
        mesh_renderer.sharedMaterial = material;
    }
    #endregion

    /*
    [Header("Noise")]
    public int seed;
    public bool randomizeSeed;
    public int numOctaves = 7;
    public float persistence = .5f;
    public float lacunarity = 2;
    public float initialScale = 2;

    public float island_size = 10000.0f;

    public float[] GenerateNoise(int mapSize) {
        var map = new float[mapSize * mapSize];
        seed = (randomizeSeed) ? Random.Range(-10000, 10000) : seed;
        var prng = new System.Random(seed);

        Vector2[] offsets = new Vector2[numOctaves];
        for (int i = 0; i < numOctaves; i++) {
            offsets[i] = new Vector2(prng.Next(-1000, 1000), prng.Next(-1000, 1000));
        }

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int y = 0; y < mapSize; y++) {
            for (int x = 0; x < mapSize; x++) {
                float noiseValue = 0;
                float noise_scale = initialScale;
                float weight = 1;
                
                for (int i = 0; i < numOctaves; i++) {
                    Vector2 p = offsets[i];

                    var factor = mesh_scale / 10000.0f;

                    p += new Vector2(x / (float)mapSize, y / (float)mapSize) * factor * noise_scale;
                    p += new Vector2(transform.position.x, transform.position.z) / mesh_scale * noise_scale * 0.248f;

                    noiseValue += Mathf.PerlinNoise(p.x, p.y) * weight;
                    weight *= persistence;
                    noise_scale *= lacunarity;
                }

                

                map[y * mapSize + x] = noiseValue;

            }
        }

        
        for (int i = 0; i < map.Length; i++) {
            //Normalize
            map[i] = (map[i] - 0.5f) / (1.8f - 0.5f);

            // Circle to make an island
            /*
            var x = i % mapSize;
            var y = i / mapSize;

            var dist_x = Mathf.Pow(mapSize / 2.0f - x, 2);
            var dist_y = Mathf.Pow(mapSize / 2.0f - y, 2);
            var distance_to_center = Mathf.Sqrt(dist_x + dist_y) / mapSize;
            map[i] *= 1 - distance_to_center;
            //

            float x = (i % mapSize); // Entre 0 et mapsize
            x /= mapSize; // entre 0 et 1
            x *= (float)mesh_scale; // entre 0 et mesh_scale
            x += transform.position.x; // World coordinate
            x /= island_size; // Island coordinate

            float y = (i / mapSize); // Entre 0 et mapsize
            y /= mapSize; // entre 0 et 1
            y *= (float)mesh_scale; // entre 0 et mesh_scale
            y += transform.position.z; // World coordinate
            y /= island_size; // island coordinate

            var dist_x = Mathf.Pow(x, 2);
            var dist_y = Mathf.Pow(y, 2);
            var distance_to_center = Mathf.Sqrt(dist_x + dist_y)/island_size;
            map[i] *= 1-distance_to_center;
            
        }
        

        return map;
    }

    #endregion

    #region EROSION
    [Header("Erosion")]
    public int erosion_iterations = 10000;

    [Range(2, 8)]
    public int erosionRadius = 3;
    [Range(0, 1)]
    public float inertia = .05f; // At zero, water will instantly change direction to flow downhill. At 1, water will never change direction. 
    public float sedimentCapacityFactor = 4; // Multiplier for how much sediment a droplet can carry
    public float minSedimentCapacity = .01f; // Used to prevent carry capacity getting too close to zero on flatter terrain
    [Range(0, 1)]
    public float erodeSpeed = .3f;
    [Range(0, 1)]
    public float depositSpeed = .3f;
    [Range(0, 1)]
    public float evaporateSpeed = .01f;
    public float gravity = 4;
    public int maxDropletLifetime = 30;

    public float initialWaterVolume = 1;
    public float initialSpeed = 1;

    // Indices and weights of erosion brush precomputed for every node
    int[][] erosionBrushIndices;
    float[][] erosionBrushWeights;

    void InitializeErosion(int mapSize, bool resetSeed) {
        if (resetSeed || prng == null || currentSeed != seed) {
            prng = new System.Random(seed);
            currentSeed = seed;
        }

        if (erosionBrushIndices == null || currentErosionRadius != erosionRadius || currentMapSize != mapSize) {
            InitializeBrushIndices(mapSize, erosionRadius);
            currentErosionRadius = erosionRadius;
            currentMapSize = mapSize;
        }

    }

    public void Erode(float[] map, int mapSize, int numIterations = 1, bool resetSeed = false) {
        InitializeErosion(mapSize, false);

        for (int iteration = 0; iteration < numIterations; iteration++) {
            // Create water droplet at random point on map
            float posX = prng.Next(0, mapSize - 1);
            float posY = prng.Next(0, mapSize - 1);
            float dirX = 0;
            float dirY = 0;
            float speed = initialSpeed;
            float water = initialWaterVolume;
            float sediment = 0;



            for (int lifetime = 0; lifetime < maxDropletLifetime; lifetime++) {
                int nodeX = (int)posX;
                int nodeY = (int)posY;
                int dropletIndex = nodeY * mapSize + nodeX;
                // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
                float cellOffsetX = posX - nodeX;
                float cellOffsetY = posY - nodeY;

                // Calculate droplet's height and direction of flow with bilinear interpolation of surrounding heights
                HeightAndGradient heightAndGradient = CalculateHeightAndGradient(map, mapSize, posX, posY);

                // Update the droplet's direction and position (move position 1 unit regardless of speed)
                dirX = (dirX * inertia - heightAndGradient.gradientX * (1 - inertia));
                dirY = (dirY * inertia - heightAndGradient.gradientY * (1 - inertia));
                // Normalize direction
                float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                if (len != 0) {
                    dirX /= len;
                    dirY /= len;
                }
                posX += dirX;
                posY += dirY;

                // Stop simulating droplet if it's not moving or has flowed over edge of map
                if (posX < 0 || posX >= mapSize - 1 || posY < 0 || posY >= mapSize - 1) {
                    break;
                }

                // Find the droplet's new height and calculate the deltaHeight
                float newHeight = CalculateHeightAndGradient(map, mapSize, posX, posY).height;
                float deltaHeight = newHeight - heightAndGradient.height;

                // Calculate the droplet's sediment capacity (higher when moving fast down a slope and contains lots of water)
                float sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * sedimentCapacityFactor, minSedimentCapacity);

                // If carrying more sediment than capacity, or if flowing uphill:
                if (sediment > sedimentCapacity || deltaHeight > 0) {
                    // If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                    float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, sediment) : (sediment - sedimentCapacity) * depositSpeed;
                    sediment -= amountToDeposit;

                    // Add the sediment to the four nodes of the current cell using bilinear interpolation
                    // Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                    map[dropletIndex] += amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                    map[dropletIndex + 1] += amountToDeposit * cellOffsetX * (1 - cellOffsetY);
                    map[dropletIndex + mapSize] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                    map[dropletIndex + mapSize + 1] += amountToDeposit * cellOffsetX * cellOffsetY;

                } else {
                    // Erode a fraction of the droplet's current carry capacity.
                    // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                    float amountToErode = Mathf.Min((sedimentCapacity - sediment) * erodeSpeed, -deltaHeight);

                    // Use erosion brush to erode from all nodes inside the droplet's erosion radius
                    for (int brushPointIndex = 0; brushPointIndex < erosionBrushIndices[dropletIndex].Length; brushPointIndex++) {
                        int nodeIndex = erosionBrushIndices[dropletIndex][brushPointIndex];
                        float weighedErodeAmount = amountToErode * erosionBrushWeights[dropletIndex][brushPointIndex];
                        float deltaSediment = (map[nodeIndex] < weighedErodeAmount) ? map[nodeIndex] : weighedErodeAmount;
                        map[nodeIndex] -= deltaSediment;
                        sediment += deltaSediment;
                    }
                }

                // Update droplet's speed and water content
                speed = Mathf.Sqrt(speed * speed + deltaHeight * gravity);
                water *= (1 - evaporateSpeed);
            }
        }
    }

    HeightAndGradient CalculateHeightAndGradient(float[] nodes, int mapSize, float posX, float posY) {
        int coordX = (int)posX;
        int coordY = (int)posY;

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = posX - coordX;
        float y = posY - coordY;

        // Calculate heights of the four nodes of the droplet's cell
        int nodeIndexNW = coordY * mapSize + coordX;
        float heightNW = nodes[nodeIndexNW];
        float heightNE = nodes[nodeIndexNW + 1];
        float heightSW = nodes[nodeIndexNW + mapSize];
        float heightSE = nodes[nodeIndexNW + mapSize + 1];

        // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        float height = heightNW * (1 - x) * (1 - y) + heightNE * x * (1 - y) + heightSW * (1 - x) * y + heightSE * x * y;

        return new HeightAndGradient() { height = height, gradientX = gradientX, gradientY = gradientY };
    }

    void InitializeBrushIndices(int mapSize, int radius) {
        erosionBrushIndices = new int[mapSize * mapSize][];
        erosionBrushWeights = new float[mapSize * mapSize][];

        int[] xOffsets = new int[radius * radius * 4];
        int[] yOffsets = new int[radius * radius * 4];
        float[] weights = new float[radius * radius * 4];
        float weightSum = 0;
        int addIndex = 0;

        for (int i = 0; i < erosionBrushIndices.GetLength(0); i++) {
            int centreX = i % mapSize;
            int centreY = i / mapSize;

            if (centreY <= radius || centreY >= mapSize - radius || centreX <= radius + 1 || centreX >= mapSize - radius) {
                weightSum = 0;
                addIndex = 0;
                for (int y = -radius; y <= radius; y++) {
                    for (int x = -radius; x <= radius; x++) {
                        float sqrDst = x * x + y * y;
                        if (sqrDst < radius * radius) {
                            int coordX = centreX + x;
                            int coordY = centreY + y;

                            if (coordX >= 0 && coordX < mapSize && coordY >= 0 && coordY < mapSize) {
                                float weight = 1 - Mathf.Sqrt(sqrDst) / radius;
                                weightSum += weight;
                                weights[addIndex] = weight;
                                xOffsets[addIndex] = x;
                                yOffsets[addIndex] = y;
                                addIndex++;
                            }
                        }
                    }
                }
            }

            int numEntries = addIndex;
            erosionBrushIndices[i] = new int[numEntries];
            erosionBrushWeights[i] = new float[numEntries];

            for (int j = 0; j < numEntries; j++) {
                erosionBrushIndices[i][j] = (yOffsets[j] + centreY) * mapSize + xOffsets[j] + centreX;
                erosionBrushWeights[i][j] = weights[j] / weightSum;
            }
        }
    }

    struct HeightAndGradient {
        public float height;
        public float gradientX;
        public float gradientY;
    }
    #endregion
    */
}
/*
[CustomEditor(typeof(Chunk))]
public class TerrainGeneratorEditor : Editor {

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        Chunk myScript = (Chunk)target;
        if (GUILayout.Button("Generate")) {
            myScript.GenerateMap();
        }
    }
}
*/