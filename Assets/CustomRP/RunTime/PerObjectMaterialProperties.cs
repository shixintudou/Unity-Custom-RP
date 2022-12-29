using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    
    static int baseColorID = Shader.PropertyToID("_BaseColor");
    static int cutoffID = Shader.PropertyToID("_Cutoff");
    static int metallicID = Shader.PropertyToID("_Metallic");
    static int smoothnessID = Shader.PropertyToID("_Smoothness");
    static int emissionColorID = Shader.PropertyToID("_EmissionColor");

    [SerializeField]
    Color baseColor= Color.white;
    [SerializeField, Range(0f, 1f)]
    float cutoff = 0.5f;
    [SerializeField, Range(0f, 1f)]
    float metallic = 0.0f;
    [SerializeField, Range(0f, 1f)]
    float smoothness = 0.0f;
    [SerializeField, ColorUsage(false, true)]
    Color emissionColor = Color.black;

    static MaterialPropertyBlock block;

    private void Awake()
    {
        OnValidate();
    }
    private void OnValidate()
    {
        if(block == null)
        {
            block = new MaterialPropertyBlock();
        }
        block.SetColor(baseColorID, baseColor);
        block.SetFloat(cutoffID, cutoff);
        block.SetFloat(metallicID, metallic);
        block.SetFloat(smoothnessID, smoothness);
        block.SetColor(emissionColorID, emissionColor);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }
}
