using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShadowSettings
{
    [Min(0.01f)]
    public float maxDistance = 20f;
    [Range(0.001f, 1f)]
    public float distanceFade = 0.1f;
    public enum TextureSize
    {
        _256=256, _512=512, _1024=1024,
        _2048=2048, _4096=4096, _8192= 8192
    } 
    public enum FilterMode
    {
        PCF2x2,PCF3x3,PCF5x5,PCF7x7
    }
    public enum CascadeBlendMode
    {
        Hard,Soft,Dither
    }
    [Serializable]
    public struct Directional
    {
        public TextureSize atlasSize;
        [Range(1, 4)]
        public int cascadeCount;
        [Range(0f, 1f)]
        public float cascadeRatio1, cascadeRatio2, cascadeRatio3;
        [Range(0.001f, 1f)]
        public float cascadeFade;

        public FilterMode filter;
        public CascadeBlendMode cascadeBlend;

        public Vector3 cascadeRatios => new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);
    }
    public Directional directional = new Directional()
    {
        atlasSize = TextureSize._1024,
        cascadeCount = 4,
        cascadeRatio1 = 0.1f,
        cascadeRatio2 = 0.25f,
        cascadeRatio3 = 0.5f,
        cascadeFade = 0.1f,
        filter = FilterMode.PCF2x2,
        cascadeBlend = CascadeBlendMode.Soft,
    };
}
