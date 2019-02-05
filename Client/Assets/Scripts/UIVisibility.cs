/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;

public class UIVisibility : MonoBehaviour 
{
    public float aSpeed = 4f; 
    float defaultA = 1f;
    public float minAlpha = 0.01f;
    public float hideIn = 0;
	public CanvasGroup myCanvas;

	public bool showOnStart, falseOnHide = true, destroyOnHide = false;

	void Start ()
	{
        if (showOnStart)
            Open();
	}

	void AddCanvas ()
	{
		if (myCanvas) return;

		myCanvas = gameObject.GetComponent<CanvasGroup> ();
		if (!myCanvas)
		myCanvas = gameObject.AddComponent<CanvasGroup> ();
		myCanvas.alpha = (!showOnStart) ? 0 : 1;
	}

    public void Close()
    {
        Open(false);
    }

	public bool activeSelf;

	public void Open (bool show = true)
	{
		if (activeSelf && !gameObject.activeSelf)
		{
			activeSelf = false;
			return;
		}

        if (!myCanvas)
        {
            AddCanvas();
            myCanvas.alpha = (showOnStart) ? 1 : 0;
        }

        activeSelf = show;

        if (show) 
		{
            if (hideIn != 0)
            {
                CancelInvoke();
                Invoke("Close", hideIn);
            }

            gameObject.SetActive (true);
			alphaDown = 1;
		} 
		else 
		{
            if (!gameObject.activeSelf)
                return;

			alphaDown = -1;
		}
	}

	int alphaDown = 0;
	void Update ()
	{
		if (alphaDown != 0) 
		{
			myCanvas.alpha += alphaDown * Time.deltaTime * aSpeed;

			if ((alphaDown == 1 && myCanvas.alpha >= defaultA) ||
			(alphaDown == -1 && myCanvas.alpha <= minAlpha))
			{
				if (alphaDown == -1) 
				{
                    if (falseOnHide)
					gameObject.SetActive (false);
                    if (destroyOnHide)
                    {
                        Destroy(gameObject);
                        return;
                    }
                        
					myCanvas.alpha = minAlpha - 0.01f;
				}
				else 
				{
					myCanvas.alpha = 1;
				}

				alphaDown = 0;
			}
		}
	}
}
