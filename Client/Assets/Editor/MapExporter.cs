using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/*
 * Exports the world as a navigation mesh for server.
 * */

public class MapExporter : EditorWindow
{
    public static string dataPath = "";
    public static string mapSize = "0x0";
    public static string boundSize = "2";

    // Add menu named "My Window" to the Window menu
    [MenuItem("EasyMOBA/Map Exporter")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        MapExporter window = (MapExporter)EditorWindow.GetWindow(typeof(MapExporter));
        window.minSize = window.maxSize = new Vector2(800, 500);

		dataPath = Application.dataPath + "/Resources/Maps";
       
        Terrain t = FindObjectOfType<Terrain>();
        if (t != null)
        {
            mapSize = t.terrainData.size.x + "x" + t.terrainData.size.z;
        }

        window.Show();
    }

    public static Vector2[] availableSizes = new Vector2[]
        {
            new Vector2 (32,32),
            new Vector2 (64,64),
            new Vector2 (128,128),
            new Vector2 (256,256),
            new Vector2 (512,512)
        };

    void OnGUI()
    {
        GUILayout.Label("Callipso Map Exporter", EditorStyles.boldLabel);
        GUILayout.Label("This tool converts your scene in to texture as collision data for both client & server", EditorStyles.label);
        GUILayout.Label("The tool writes only static colliders and slope>0.8f as collisions on texture", EditorStyles.label);
        GUILayout.Label("You should open your scene to bake, you cannot bake a scene from another scene. This tool bakes the opened scene.", EditorStyles.label);
        GUILayout.Label("The collision data (Texture2D file) will be placed in Resources/Maps/", EditorStyles.label);
        GUILayout.Label("Do not forget to add that file to the server side, to do that, follow the documentations at easymoba.com", EditorStyles.label);

        GUILayout.Space(20);

        GUILayout.Label("Map size");
        mapSize = GUILayout.TextField(mapSize, 25);

        GUILayout.Space(20);

        GUILayout.Label("Map bound size");
        boundSize = GUILayout.TextField(boundSize, 5);

        GUILayout.Space(10);

        string[] s = mapSize.Split('x');
        if (s.Length != 2)
        {
            GUILayout.Label("Map size format should be ValuexValue: for example 64x64");
            GUILayout.Space(20);
            return; 
        }
        else
        {
            try
            {
                Vector2 v = availableSizes.ToList().Find(x => x == new Vector2(int.Parse(s[0]), int.Parse(s[1])));
                if (v == Vector2.zero)
                {
                    GUILayout.Label("Map size is invalid. Available map sizes:");
                    GUILayout.Space(20);
                    for (int i = 0; i < availableSizes.Length; i++)
                    {
                        GUILayout.Label(availableSizes [i].x + "x" + availableSizes[i].y);
                        GUILayout.Space(5);
                    }

                    return;
                }
            }
            catch
            {
                GUILayout.Label("Map size format is invalid it should be integerxinteger, for example 64x64");
                GUILayout.Space(20);

                return;
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button ("Export Now"))
        {
            ExportWorld();
        }
    }

    Vector3 pos = Vector3.zero;
    int modY, modX, y, x, getCollision;

    public void ExportWorld()
    {
        Debug.Log("Map export started");

        string[] parseSize = mapSize.Split("x"[0]);
        modY = int.Parse(parseSize[1]);
        modX = int.Parse(parseSize[0]);

        int bd = int.Parse(boundSize);
        Texture2D texture = new Texture2D(modX, modY);
        Color c = Color.black;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                c = Color.black;
                pos = new Vector3(x, 0, y);
                if (y < bd || y >= texture.height - bd || x < bd || x >= texture.width - bd)
                    c.r = 1;
                else c.r = (isWorldBlocked (pos)) ? 1 : 0;
                texture.SetPixel(x, y, c);
            }
        }
		
        texture.Apply();

        // Encode texture into PNG
        byte[] bytes = texture.EncodeToPNG();
        
        System.IO.File.WriteAllBytes(dataPath + "/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "@" + modX + ".png", bytes);

        AssetDatabase.Refresh();
    }
	
	public static bool isWorldBlocked (Vector3 myPos)
    {
        RaycastHit hit = new RaycastHit();

        myPos.y = 100;

        /*
         * CHECK SLOPE
         * */

        int rCount = rectangle.Length;

        for (int i = 0; i < rCount; i++)
        {
            if (Mathf.Abs(MapLoader.GetHeight(myPos) - MapLoader.GetHeight(myPos + rectangle[i])) > 0.8f)
                return true;
        }

        /*
        * */

        if (Physics.Raycast(myPos, -Vector3.up, out hit, 100))
        {
            Collider [] cldrs = Physics.OverlapSphere(hit.point, 0.25f);
            foreach (Collider c in cldrs)
                if (c.gameObject.layer != 8 && hit.collider.gameObject.isStatic)
                {
                    return true;
                }
        }

        return false;
    }

    public static Vector3[] rectangle = new Vector3[8]
    {
        new Vector3 (1,0,0),
        new Vector3 (-1,0,0),
        new Vector3 (0,0,1),
        new Vector3 (0,0,-1),
        new Vector3 (1,0,1),
        new Vector3 (1,0,-1),
        new Vector3 (-1,0,1),
        new Vector3 (-1,0,-1)
    };
}
