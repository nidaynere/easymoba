/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;
using UnityEngine.UI;

public class Filler : MonoBehaviour
{
    public Image _image;
    public float speed = 5;

    public float startingValue = 1f;

	// Use this for initialization
	void Awake ()
    {
		if (_image == null)
        _image = GetComponent<Image>();
	}

    public float fillAmount;

    void Update()
    {
        float v = fillAmount - _image.fillAmount;

        if (Mathf.Abs(v) < speed * Time.deltaTime * 2)
        {
            v = 0;
            _image.fillAmount = fillAmount;
        } 
        else if (v < 0) v = -1;
        else v = 1;

        _image.fillAmount += v * speed * Time.deltaTime;
    }
}
