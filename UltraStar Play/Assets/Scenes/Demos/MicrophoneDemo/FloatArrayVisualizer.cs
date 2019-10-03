using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatArrayVisualizer : MonoBehaviour
{
    public GameObject cubePrefab;
    public float xscale = 1;
    public float yscale = 1;
    public int visualizationLength = 100;
    private int step;

    private float[] floatArray;
    private GameObject[] cubes;
    private bool isInitialized = false;

    public void Init(float[] array)
    {
        floatArray = array;
        step = floatArray.Length / visualizationLength;
        CreateCubes();
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        // Move y position to value of floatArray
        for (int i = 0; i < visualizationLength; i++)
        {
            float y = floatArray[i * step] * yscale;
            Vector3 oldPos = cubes[i].transform.position;
            cubes[i].transform.position = new Vector3(oldPos.x, y, oldPos.z);
        }
    }

    private void CreateCubes()
    {
        cubes = new GameObject[visualizationLength];
        for (int i = 0; i < visualizationLength; i++)
        {
            GameObject cube = Instantiate(cubePrefab);
            cube.transform.SetParent(transform);
            float x = i * xscale;
            cube.transform.position = new Vector3(x, 0, 0);

            cubes[i] = cube;
        }
    }
}
