using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject licenceViewObj;

    private bool isShow;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            CloseLicenceView();
    }

    public void ShowLicenceView()
    {
        if (isShow)
            return;

        licenceViewObj.SetActive(true);
        isShow = true;
    }

    public void CloseLicenceView()
    {
        if (!isShow)
            return;

        licenceViewObj.SetActive(false);
        isShow = false;
    }
}
