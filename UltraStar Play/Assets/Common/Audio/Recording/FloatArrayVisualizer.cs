using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatArrayVisualizer : MonoBehaviour
{
    public RectTransform dataPointPrefab;
    public int visualizationLength = 100;

    private float[] floatArray;
    private int step;
    private RectTransform[] dataPoints;
    private bool isInitialized;

    public void Init(float[] array)
    {
        floatArray = array;
        step = floatArray.Length / visualizationLength;
        CreateGameObjects();
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
            RectTransform dataPoint = dataPoints[i];
            int floatArrayIndex = Mathf.Min(i * step, floatArray.Length - 1);
            float y = 0.5f + (floatArray[floatArrayIndex] / 2.0f);
            PositionDataPoint(dataPoint, dataPoint.anchorMin.x, y);
        }
    }

    private void CreateGameObjects()
    {
        dataPoints = new RectTransform[visualizationLength];
        for (int i = 0; i < visualizationLength; i++)
        {
            RectTransform dataPoint = Instantiate(dataPointPrefab);
            dataPoint.transform.SetParent(transform);
            double x = (double)i / visualizationLength;
            PositionDataPoint(dataPoint, (float)x, 0);

            dataPoints[i] = dataPoint;
        }
    }

    private void PositionDataPoint(RectTransform dataPoint, float x, float y)
    {
        dataPoint.anchorMin = new Vector3(x, y, 0);
        dataPoint.anchorMax = new Vector3(x, y, 0);
        dataPoint.anchoredPosition = Vector2.zero;
    }
}
