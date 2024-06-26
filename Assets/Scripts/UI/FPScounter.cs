using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Diagnostics;


public class FPScounter : MonoBehaviour
{
    float deltaTime = 0.0f;
    int time = 30;
    int frameCount = 0;
    int[] fpsArray = new int[60];
    int compteur = 0;
    void Start()
    {

        StartCoroutine(AverageFPS());
    }

    void Update()
    {
        frameCount++;
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

    }

    IEnumerator AverageFPS()
    {
        while (true)
        {
            int averageFPS = frameCount / time;
            UnityEngine.Debug.LogFormat("Average FPS over {0} seconds: {1:0.} fps", time, averageFPS);
            frameCount = 0;
            fpsArray[compteur % 60] = averageFPS;
            if (compteur % 60 == 0) UnityEngine.Debug.LogFormat(string.Join(", ", fpsArray));
            compteur++;
            yield return new WaitForSeconds(time);
        }
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, w, h * 2 / 50);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 50;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        GUI.Label(rect, text, style);
    }
}
