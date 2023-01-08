using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class GrassPointCloudRenderer : MonoBehaviour
{
    // variables from GUI
    public MeshFilter filter;

    public int seed;
    public float areaHalf;

    [Range(1, 60000)]
    public int grassNumber;
    
    public float grassOffset = 0.0f;

    public Color color;
    public float minRange = 0.5f;

    // constances
    private float defaultHeight = -5000.0f;
    private float maxRange = 1.0f;

    // private variables
    private Mesh mesh;
    private Vector3 camLastPosition;
	private bool 	init = false;
    
    // mesh containers
    private List<Vector3> positions;
    private int[] indicies;
    private List<Color> colors;
    private List<Vector3> normals;

    void Start()
    {
        camLastPosition = Camera.main.transform.position;
        Random.InitState(seed);
        positions = new List<Vector3>(grassNumber);
        indicies = new int[grassNumber];
        colors = new List<Color>(grassNumber);
        normals = new List<Vector3>(grassNumber);
        for (int i = 0; i < grassNumber; ++i)
        {
            Vector3 origin = camLastPosition;
            origin.x = camLastPosition.x + areaHalf * Random.Range(-1f, 1f);
            origin.y = defaultHeight;
            origin.z = camLastPosition.z + areaHalf * Random.Range(-1f, 1f);

            positions.Add(origin);
            normals.Add(Vector3.up);
            indicies[i] = i;
            colors.Add(new Color(color.r * Random.Range(minRange, maxRange),
                                 color.g * Random.Range(minRange, maxRange),
                                 color.b * Random.Range(minRange, maxRange),
                                 Random.Range(0.5f, 1.0f)));
        }
    }

    private float calcZFromRayCast(Vector3 position, Vector3 normal)
    {
        Vector3 localPosition = position;
        localPosition.y = 1000;

        Ray ray = new Ray(localPosition, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))//, mask)) // 8 - user default layer mask Terrain Grass
        {
            float yValue = hit.point.y + grassOffset; 
            normal = hit.normal;
            return yValue;
        }
        
        return defaultHeight;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 camPosition = Camera.main.transform.position;
        double lengthSize = areaHalf * areaHalf;
        
        if (camLastPosition != camPosition) // no change
        {			
			//int numNotInArea = 0;
			//int numDefaultHeight = 0;
            Vector3 camOffset = camPosition - camLastPosition;
            for (int i = 0; i < grassNumber; ++i)
            {
                Vector3 position = positions[i];
                Vector3 positionLocalToCam = position - camPosition;

                bool isInArea = true;
                if (positionLocalToCam.x < -areaHalf)
                {
                    position.x += areaHalf * 2;
                    isInArea = false;
                } 
                else if (positionLocalToCam.x > areaHalf)
                {
                    position.x -= areaHalf * 2;
                    isInArea = false;
                }

                if (positionLocalToCam.z < -areaHalf)
                {
                    position.z += areaHalf * 2;
                    isInArea = false;
                }
                else if (positionLocalToCam.z > areaHalf)
                {
                    position.z -= areaHalf * 2;
                    isInArea = false;
                }
				
				//if (!isInArea)
				//	numNotInArea++;
				//if (position.y == defaultHeight && init == false)
				//	numDefaultHeight++;
				
                if (!isInArea || (position.y == defaultHeight && init == false))
                    position.y = calcZFromRayCast(position, normals[i]);
                
                positions[i] = position;
            }
            mesh = new Mesh();
            mesh.SetVertices(positions);
            mesh.SetIndices(indicies, MeshTopology.Points, 0);
            mesh.SetColors(colors);
            mesh.SetNormals(normals);
            filter.mesh = mesh;

            camLastPosition = camPosition;
            init = true;

            //Debug.Log("numNotInArea: " + numNotInArea + "  numDefaultHeight: " + numDefaultHeight);
        }
    }
}
