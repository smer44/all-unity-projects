using UnityEngine;
using UnityEngine.UI;

public class ParticleForceSimulation : MonoBehaviour
{

    [SerializeField] private ComputeShader computeShader;


    [SerializeField] private Renderer displayRenderer;

    [SerializeField] private Image displayImage;
    [SerializeField] private Material displayMaterialTemplate;

    [SerializeField] private float particlePositionScale = 1.0f;
    [SerializeField] private float dotRadius = 0.05f;
    [SerializeField] private float dotSoftness = 0.02f;


    private const int MaxParticles = 256;
    private const int TypeCount = 4;

    private ComputeBuffer positionBufferA;
    private ComputeBuffer positionBufferB;
    private ComputeBuffer velocityBuffer;
    private ComputeBuffer typeBuffer;
    private ComputeBuffer attractionMatrixBuffer;

    private ComputeBuffer positionsRead;
    private ComputeBuffer positionsWrite;


    private int kernel;
    private int particleCount;

    private Vector2[] positions;
    private Vector2[] velocities;
    private int[] types;
    private float[] attractionMatrix;    

    private MaterialPropertyBlock displayBlock;


    //Shader Property Ids

    private static readonly int ParticlePositionsId =
        Shader.PropertyToID("_ParticlePositions");

    private static readonly int ParticleTypesId =
        Shader.PropertyToID("_ParticleTypes");

    private static readonly int ParticleCountId =
        Shader.PropertyToID("_ParticleCount");

    private static readonly int ParticlePositionScaleId =
        Shader.PropertyToID("_ParticlePositionScale");

    private static readonly int DotRadiusId =
        Shader.PropertyToID("_DotRadius");

    private static readonly int DotSoftnessId =
        Shader.PropertyToID("_DotSoftness");

    private static readonly int ParticleTypeColorsId =
        Shader.PropertyToID("_ParticleTypeColors");

    private Vector4[] particleTypeColors;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {   
        StartColors();
        StartMaterial();
        StartArrays();
    

        SetDummyAttractionMatrix();
        InitComputeBufers();

        kernel = computeShader.FindKernel("CSUpdateVelocity");

        StartSetBuffersToKernel();

    }

    void StartArrays()
    {
        particleCount = MaxParticles;
        positions = new Vector2[particleCount];
        velocities = new Vector2[particleCount];
        types = new int[particleCount];

        attractionMatrix = new float[TypeCount * TypeCount];

        for (int i = 0; i < particleCount; i++)
        {
            positions[i] = Random.insideUnitCircle * 5f;
            velocities[i] = Vector2.zero;
            types[i] = Random.Range(0, TypeCount);
        }            


    }

    void StartColors()
    {
        
        displayBlock = new MaterialPropertyBlock();

        particleTypeColors = new Vector4[16];

        particleTypeColors[0] = new Vector4(1f, 0.1f, 0.1f, 1f); // red
        particleTypeColors[1] = new Vector4(0.1f, 0.4f, 1f, 1f); // blue
        particleTypeColors[2] = new Vector4(0.2f, 1f, 0.2f, 1f); // green
        particleTypeColors[3] = new Vector4(1f, 1f, 0.2f, 1f);   // yellow

        for (int i = 4; i < particleTypeColors.Length; i++)
        {
            particleTypeColors[i] = new Vector4(1f, 1f, 1f, 1f);
        }

    }

    void StartMaterial()
    {
        Material displayMaterialInstance = new Material(displayMaterialTemplate);
        displayImage.material = displayMaterialInstance;
        displayImage.raycastTarget = false;        
    }



    void SetDummyAttractionMatrix()
    {
        // Example attraction matrix.
        // matrix[selfType * TypeCount + otherType]
        attractionMatrix[0 * TypeCount + 0] = -1.0f;
        attractionMatrix[0 * TypeCount + 1] =  2.0f;
        attractionMatrix[0 * TypeCount + 2] = -0.5f;
        attractionMatrix[0 * TypeCount + 3] =  0.0f;

        attractionMatrix[1 * TypeCount + 0] = -2.0f;
        attractionMatrix[1 * TypeCount + 1] =  1.0f;
        attractionMatrix[1 * TypeCount + 2] =  0.5f;
        attractionMatrix[1 * TypeCount + 3] = -1.0f;

        attractionMatrix[2 * TypeCount + 0] =  1.0f;
        attractionMatrix[2 * TypeCount + 1] = -1.0f;
        attractionMatrix[2 * TypeCount + 2] =  0.2f;
        attractionMatrix[2 * TypeCount + 3] =  2.0f;

        attractionMatrix[3 * TypeCount + 0] =  0.0f;
        attractionMatrix[3 * TypeCount + 1] =  1.0f;
        attractionMatrix[3 * TypeCount + 2] = -2.0f;
        attractionMatrix[3 * TypeCount + 3] = -0.2f;        
    }


    private void BindDisplayBuffer(ComputeBuffer currentPositionBuffer)
    {
        displayRenderer.GetPropertyBlock(displayBlock);

        displayBlock.SetBuffer(ParticlePositionsId, currentPositionBuffer);
        displayBlock.SetBuffer(ParticleTypesId, typeBuffer);

        displayBlock.SetInt(ParticleCountId, particleCount);

        displayBlock.SetFloat(ParticlePositionScaleId, particlePositionScale);
        displayBlock.SetFloat(DotRadiusId, dotRadius);
        displayBlock.SetFloat(DotSoftnessId, dotSoftness);

        displayBlock.SetVectorArray(ParticleTypeColorsId, particleTypeColors);

        displayRenderer.SetPropertyBlock(displayBlock);
    }


    void InitComputeBufers()
    {
        positionBufferA = new ComputeBuffer(particleCount, sizeof(float) * 2);
        positionBufferB = new ComputeBuffer(particleCount, sizeof(float) * 2);

        velocityBuffer = new ComputeBuffer(particleCount, sizeof(float) * 2);
        typeBuffer = new ComputeBuffer(particleCount, sizeof(int));
        attractionMatrixBuffer = new ComputeBuffer(TypeCount * TypeCount, sizeof(float));

        
        positionBufferA.SetData(positions);
        positionBufferB.SetData(positions);
        velocityBuffer.SetData(velocities);
        typeBuffer.SetData(types);
        attractionMatrixBuffer.SetData(attractionMatrix);   

        positionsRead = positionBufferA;
        positionsWrite = positionBufferB;

    }

    void StartSetBuffersToKernel()
    {
        //computeShader.SetBuffer(kernel, "_Positions", positionBuffer);
        computeShader.SetBuffer(kernel, "_Velocities", velocityBuffer);
        computeShader.SetBuffer(kernel, "_Types", typeBuffer);
        computeShader.SetBuffer(kernel, "_AttractionMatrix", attractionMatrixBuffer);

        computeShader.SetInt("_ParticleCount", particleCount);
        computeShader.SetInt("_TypeCount", TypeCount);

        computeShader.SetFloat("_ForceScale", 10.0f);
        computeShader.SetFloat("_Damping", 0.2f);

        computeShader.SetFloat("_MinDistance", 0.05f);
        computeShader.SetFloat("_MaxDistance", 10.0f);
        computeShader.SetFloat("_Softening", 0.1f);       

    }

    void UpdateSetBuffersToKernel()
    {
        computeShader.SetFloat("_DeltaTime", Time.deltaTime);
        computeShader.SetFloat("_DeltaTimeScale", 1.0f);       

        computeShader.SetBuffer(kernel, "_PositionsRead", positionsRead);
        computeShader.SetBuffer(kernel, "_PositionsWrite", positionsWrite); 

    }

    // Update is called once per frame
    void Update()
    {
        UpdateSetBuffersToKernel();

        int threadGroups = Mathf.CeilToInt(particleCount / 64.0f);

        computeShader.Dispatch(kernel, threadGroups, 1, 1);

        // positionsWrite now contains the newest simulation state.
        BindDisplayBuffer(positionsWrite);

        SwapPositionBuffers();

    }


    private void SwapPositionBuffers()
    {
        ComputeBuffer temp = positionsRead;
        positionsRead = positionsWrite;
        positionsWrite = temp;
    }

    private void OnDestroy()
    {
        positionBufferA?.Release();
        positionBufferB?.Release();
        velocityBuffer?.Release();
        typeBuffer?.Release();
        attractionMatrixBuffer?.Release();
    }



}
