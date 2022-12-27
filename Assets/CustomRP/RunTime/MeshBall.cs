using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBall : MonoBehaviour
{
    // Start is called before the first frame update
    static int baseColorID = Shader.PropertyToID("_BaseColor");
    static int cutoffID = Shader.PropertyToID("_Cutoff");
    static int metallicID = Shader.PropertyToID("_Metallic");
    static int smoothnessID = Shader.PropertyToID("_Smoothness");
    static MaterialPropertyBlock block;
    [SerializeField]
    Mesh mesh = default;
    [SerializeField]
    Material material = default;

    Matrix4x4[] matrices = new Matrix4x4[1023];
    Vector4[] baseColors = new Vector4[1023];
    float[] cutoffs = new float[1023];
    float[] metallics = new float[1023];
    float[] smoothnesses = new float[1023];

    private void Awake()
    {
        for(int i=0;i<matrices.Length;i++)
        {
            matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10f, Quaternion.identity, Vector3.one);
            baseColors[i] = new Vector4(Random.value, Random.value, Random.value, 1);
            cutoffs[i] = Random.value;
            metallics[i] = Random.value < 0.25f ? 1f : 0f;
            smoothnesses[i] = Random.Range(0.05f, 0.95f);
        }
    }
    private void Update()
    {
        if(block==null)
        {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(baseColorID, baseColors);
            block.SetFloatArray(cutoffID, cutoffs);
            block.SetFloatArray(metallicID, metallics);
            block.SetFloatArray(smoothnessID, smoothnesses);
        }
        Graphics.DrawMeshInstanced(mesh, 0, material, matrices, 1023, block);
    }
}
