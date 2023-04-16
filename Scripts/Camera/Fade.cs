using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fade : MonoBehaviour
{
    public Transform planet;
    public Transform mainCam;

    public float fadeStart, fadeEnd;

    void Update()
    {
        float invDiff = 1 / (fadeEnd - fadeStart);
        float fade = Mathf.Clamp01((fadeEnd - Vector3.Distance(planet.position, mainCam.position)) * invDiff);

        //print("distance: " + Vector3.Distance(planet.position, mainCam.position).ToString() + ", fade: " + fade);
        var renderer = GetComponent<Renderer>();
        var c = renderer.sharedMaterial.color;

        c.a = 1f - fade;

        renderer.sharedMaterial.color = c;
    }
}
