using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connector : MonoBehaviour
{
    public Vector2 size = Vector2.one * 4f; // General size of the connector

    public bool isConnected;
    public bool isUnavailable;

    bool isPlaying;

    void Start()
    {
        isPlaying = true;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isConnected ? Color.green : isUnavailable ? Color.magenta : Color.red;

        if (!isPlaying)
        {
            Gizmos.color = Color.cyan;
        }

        Vector2 halfSize = size * 0.5f;
        Vector3 offset = transform.position + transform.up * halfSize.y; // (Offset = Where the line should start from)
        Gizmos.DrawLine(offset, offset + transform.forward);

        // Define top & side vectors
        Vector3 top = transform.up * size.y;
        Vector3 side = transform.right * halfSize.x;

        // Define corner vectors
        Vector3 topRight = transform.position + top + side;
        Vector3 topLeft = transform.position + top - side;
        Vector3 botRight = transform.position + side;
        Vector3 botLeft = transform.position - side;

        // Draw border lines
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, botLeft);
        Gizmos.DrawLine(botLeft, botRight);
        Gizmos.DrawLine(botRight, topRight);

        // Draw diagonal lines
        Gizmos.color *= 0.5f;

        Gizmos.DrawLine(topRight, offset);
        Gizmos.DrawLine(topLeft, offset);
        Gizmos.DrawLine(botRight, offset);
        Gizmos.DrawLine(botLeft, offset);
    }
}
