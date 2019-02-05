/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Texts
{
    [System.Serializable]
    public class txt
    {
        public ushort id;
        public string text;
    }

    public List<txt> list = new List<txt>();

    public string find(ushort id)
    {
        try
        {
            return list.Find(x => x.id == id).text;
        }
        catch
        {
            return "";
        }
    }
}

public class Language : MonoBehaviour
{
    public static Texts texts;

    public static string GetText(int id)
    {
        try
        {
            return texts.find((ushort)id);
        }
        catch
        {
            return "";
        }
    }

    public bool isManager;
    public int textId = -1;

    public static string loaded;
    public static void LoadLanguage(string s)
    {
        loaded = s;
        TextAsset lang = Resources.Load<TextAsset>("Languages/" + s);
        texts = JsonUtility.FromJson<Texts>(lang.text);

        Language[] lg = FindObjectsOfType<Language>();
        ushort lgs = (ushort) lg.Length;
        for (int i = 0; i < lgs; i++)
                lg[i].UpdateText();
    }

    public bool unused = false;

    private void OnEnable()
    {
        if (!unused)
        UpdateText();
    }

    public void UpdateText()
    {
        if (textId != -1)
        {
            GetComponent<UnityEngine.UI.Text>().text = GetText(textId);
        }
    }
}
