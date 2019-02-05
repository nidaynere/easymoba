/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 24 December 2017
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class DataReader
{
    public static string filePath = "/../ServerData/";

    public static void readSingleJson(string fileName, out string content)
    {
        string path = Application.dataPath + filePath + fileName;
        content = File.ReadAllText(path);
    }

    public static void readJson(string folderName, out List<string> contents)
    {
        contents = new List<string>();
        string path = Application.dataPath + filePath + folderName;
        string[] directories = Directory.GetDirectories(path);

        List<string> dds = directories.ToList();
        dds.Add(Application.dataPath + filePath + folderName);

        foreach (string d in dds)
        {
            string[] get = Directory.GetFiles(d, "*.json");

            foreach (string file in get)
            {
                string cnt = File.ReadAllText(file);
                contents.Add(cnt);
            }
        }
    }

    public static void readMaps(out List<Map> maps)
    {
        maps = new List<Map>();
        string path = Application.dataPath + filePath + "Maps";
        string[] directories = Directory.GetDirectories(path);

        foreach (string d in directories) // Each is map
        {
            string get = File.ReadAllText(d + "/MapData.json");

            Map m = JsonUtility.FromJson<Map>(get);

            string[] image = Directory.GetFiles(d, "*.png");
            int size = int.Parse (Path.GetFileNameWithoutExtension(image[0]).Split ('@') [1]);

            byte[] getBytes = File.ReadAllBytes(image[0]);
            m.data = new Texture2D(size, size);
            m.data.LoadImage(getBytes);

            m.mapSize = size;

            maps.Add(m);
        }
    }
}
