using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {

    [Header("Chunk")]
    public int chunk_amount = 8;
    private Chunk[] chunks;

    public float chunk_scale = 2500;
    public float chunk_elevation_scale = 1000;
    public int chunk_node_count = 128;
    public int heightmap_node_count;
    public Material chunk_material;

    System.Random prng;
    public int seed;
    public bool randomize_seed;

    void Start() {
        heightmap_node_count = chunk_node_count * chunk_amount;

        // Seed
        if (randomize_seed || prng == null) {
            seed = (randomize_seed) ? Random.Range(-10000, 10000) : seed;
            prng = new System.Random(seed);
        }
        Stopwatch sw = new Stopwatch();

        sw.Start();
        heightmap = GenerateHeightmap(heightmap_node_count);
        sw.Stop();
        print("Heightmap generated: " + sw.ElapsedMilliseconds + "ms");

        sw.Restart();
        Erode(heightmap, heightmap_node_count, erosion_iterations);
        sw.Stop();
        print("Erosion completed: " + sw.ElapsedMilliseconds + "ms");

        sw.Restart();
        chunks = GenerateChunks(heightmap, heightmap_node_count, chunk_amount, chunk_node_count, chunk_scale, chunk_elevation_scale, chunk_material);
        sw.Stop();
        print("Chunks generated: " + sw.ElapsedMilliseconds + "ms");
    }

    private Chunk[] GenerateChunks(float[] heightmap, int heightmap_node_count, int chunk_amount, int chunk_node_count, float chunk_scale, float chunk_elevation_scale, Material chunk_material) {

        var chunks = new Chunk[chunk_amount * chunk_amount];

        for (int y = 0; y < chunk_amount; y++) {
            for (int x = 0; x < chunk_amount; x++) {
                int i = y * chunk_amount + x;
                var go = new GameObject();
                go.name = string.Format(x + "," + y);
                go.transform.position = new Vector3((x - chunk_amount/2) * chunk_scale, 0, (y - chunk_amount/2) * chunk_scale);
                go.transform.parent = transform;
                chunks[i] = go.AddComponent<Chunk>();
                chunks[i].GenerateMesh(heightmap, heightmap_node_count, new Vector2Int(x * (chunk_node_count - 1), y * (chunk_node_count - 1)), chunk_material, chunk_node_count, chunk_scale, chunk_elevation_scale);
            }
        }
        return chunks;
    }

    #region HEIGHTMAP
    [Header("Heightmap")]

    private float[] heightmap;
    public int num_octaves = 7;
    public float persistence = .5f;
    public float lacunarity = 2;
    public float initial_scale = 2;

    public float[] GenerateHeightmap(int node_count) {
        var map = new float[node_count * node_count];

        Vector2[] offsets = new Vector2[num_octaves];
        for (int i = 0; i < num_octaves; i++) {
            offsets[i] = new Vector2(prng.Next(-1000, 1000), prng.Next(-1000, 1000));
        }

        float min_value = 0;
        float max_value = 1;

        for (int y = 0; y < node_count; y++) {
            for (int x = 0; x < node_count; x++) {
                float noise_value = 0;
                float noise_scale = initial_scale;
                float weight = 1;

                for (int i = 0; i < num_octaves; i++) {
                    Vector2 p = offsets[i];
                    var factor = 2500.0f / 10000.0f;
                    p += new Vector2(x / (float)node_count, y / (float)node_count) * noise_scale;

                    noise_value += Mathf.PerlinNoise(p.x, p.y) * weight;
                    weight *= persistence;
                    noise_scale *= lacunarity;
                }
                map[y * node_count + x] = noise_value;
                min_value = noise_value < min_value ? min_value : noise_value;
                max_value = noise_value > max_value ? max_value : noise_value;
            }
        }


        for (int i = 0; i < map.Length; i++) {
            //Normalize
            map[i] = (map[i] - min_value) / (max_value - min_value);

            // Circle to make an island

            var x = i % node_count;
            var y = i / node_count;

            var dist_x = Mathf.Pow(node_count / 2.0f - x, 2);
            var dist_y = Mathf.Pow(node_count / 2.0f - y, 2);
            float distance_to_center = Mathf.Sqrt(dist_x + dist_y) / (float)node_count;
            map[i] *= 1 - distance_to_center;

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

    void InitializeErosion(int node_count) {
        if (erosionBrushIndices == null) {
            InitializeBrushIndices(node_count, erosionRadius);
        }
    }

    public void Erode(float[] map, int node_count, int iterations_amount = 1) {
        InitializeErosion(node_count);

        for (int iteration = 0; iteration < iterations_amount; iteration++) {
            // Create water droplet at random point on map
            float posX = prng.Next(0, node_count - 1);
            float posY = prng.Next(0, node_count - 1);
            float dirX = 0;
            float dirY = 0;
            float speed = initialSpeed;
            float water = initialWaterVolume;
            float sediment = 0;

            for (int lifetime = 0; lifetime < maxDropletLifetime; lifetime++) {
                int nodeX = (int)posX;
                int nodeY = (int)posY;
                int dropletIndex = nodeY * node_count + nodeX;
                // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
                float cellOffsetX = posX - nodeX;
                float cellOffsetY = posY - nodeY;

                // Calculate droplet's height and direction of flow with bilinear interpolation of surrounding heights
                HeightAndGradient heightAndGradient = CalculateHeightAndGradient(map, node_count, posX, posY);

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
                if (posX < 0 || posX >= node_count - 1 || posY < 0 || posY >= node_count - 1) {
                    break;
                }

                // Find the droplet's new height and calculate the deltaHeight
                float newHeight = CalculateHeightAndGradient(map, node_count, posX, posY).height;
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
                    map[dropletIndex + node_count] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                    map[dropletIndex + node_count + 1] += amountToDeposit * cellOffsetX * cellOffsetY;

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

    HeightAndGradient CalculateHeightAndGradient(float[] nodes, int node_count, float pos_x, float pos_y) {
        int coord_x = (int)pos_x;
        int coord_y = (int)pos_y;

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = pos_x - coord_x;
        float y = pos_y - coord_y;

        // Calculate heights of the four nodes of the droplet's cell
        int nodeIndexNW = coord_y * node_count + coord_x;
        float heightNW = nodes[nodeIndexNW];
        float heightNE = nodes[nodeIndexNW + 1];
        float heightSW = nodes[nodeIndexNW + node_count];
        float heightSE = nodes[nodeIndexNW + node_count + 1];

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
}
