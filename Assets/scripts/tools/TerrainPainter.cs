using UnityEngine;

public class TerrainPainter : MonoBehaviour
{
    [Header("height settings")]
    public float mudLevel = 5f;
    public float grassLevel = 20f;

    [Header("slope settings")]
    public float dirtThreshold = 25f;
    public float rockThreshold = 40f;

    [ContextMenu("paint terrain")]//right click on componenet in inspector
    public void PaintTerrain()
    {
        Terrain terrain = GetComponent<Terrain>();
        TerrainData data = terrain.terrainData;

        //get texture weights
        float[,,] alphamapData = new float[data.alphamapWidth, data.alphamapHeight, 4];

        for (int y = 0; y < data.alphamapHeight; y++)
        {
            for (int x = 0; x < data.alphamapWidth; x++)
            {
                float[] weights = new float[4];

                float normX = (float)x / (data.alphamapWidth - 1);
                float normY = (float)y / (data.alphamapHeight - 1);

                //get slope
                float angle = data.GetSteepness(normX, normY);
                float height = data.GetHeight(Mathf.RoundToInt(normY * data.heightmapResolution), Mathf.RoundToInt(normX * data.heightmapResolution));

                // --- LOGIC LAYER ---
                if (angle > rockThreshold)
                {
                    weights[2] = 1f;//rock
                }
                else if (angle > dirtThreshold)
                {
                    weights[1] = 1f;//dirt
                }
                else if (height < mudLevel)
                {
                    weights[3] = 1f;//mud
                }
                else
                {
                    weights[0] = 1f;//grass
                }

                //assigning weights
                for (int i = 0; i < 4; i++)
                {
                    alphamapData[x, y, i] = weights[i];
                }
            }
        }

        //apply new map to terrain
        data.SetAlphamaps(0, 0, alphamapData);
    }
}