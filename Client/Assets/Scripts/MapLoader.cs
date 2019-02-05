/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using System.Collections.Generic;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
    public static List<Texture2D> maps = new List<Texture2D>(); // loaded maps

    public static bool isBlocked(ushort mapId, Vector3 cPosition, Vector3 tPosition, bool fixedMagnitude = true)
    {
        Vector3 direction = (tPosition - cPosition).normalized;

        int magnitude = (fixedMagnitude) ? 2 : Mathf.RoundToInt(GameManager.GetDistance(cPosition, tPosition));

        if (magnitude < 1) magnitude = 1;
        for (int i = 1; i < magnitude; i++)
        {
            Vector3 position = cPosition + direction * i;
            if (position.x <= 0 || position.x > maps[mapId].width || position.z <= 0 || position.z >= maps[mapId].height)
            {
                return true; // Out of bounds
            }

            Color col = maps[mapId].GetPixel(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));

            bool c = col.r > 0.1f;

            if (c)
            {
                return true;
            }
        }

        return false;
    }

    public static bool isPositionBlocked(ushort mapId, Vector3 position)
    {
        position = new Vector3(Mathf.RoundToInt(position.x), 0, Mathf.RoundToInt(position.z));
        if (position.x <= 0 || position.x >= maps[mapId].width || position.z >= maps[mapId].height || position.z <= 0)
            return false;

        Color c = maps[mapId].GetPixel(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));

        return (c.r > 0.9f);
    }

    public static LayerMask layerMask = 1 << 8;

    public static float GetHeight(Vector3 myPos)
    {
        RaycastHit hit = new RaycastHit();

        myPos.y = 100;

        if (Physics.Raycast(myPos, -Vector3.up, out hit, 100, layerMask))
        {
            return hit.point.y;
        }

        return 0f;
    }

    public static Vector3 latestPoint(ushort mapId, Vector3 position1, Vector3 position2)
    {
        position1.y = 0;
        position2.y = 0;

        Vector3 normal = (position2 - position1).normalized;
        float currentDistance = 100;
        while (currentDistance > 1)
        {
            currentDistance = GameManager.GetDistance(position2, position1);
            position1 += normal;

            if (isPositionBlocked(mapId, position1))
                return position1 - normal;
        }

        return position2;
    }
}
