using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplicationSetup : MonoBehaviour
{
    void Start()
    {
        Application.runInBackground = true;
    }
}
