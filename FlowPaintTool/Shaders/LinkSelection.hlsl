StructuredBuffer<float3> _Vertices;
StructuredBuffer<float2> _UVs;
StructuredBuffer<uint> _Triangles;
RWStructuredBuffer<int3> _AdjacentResult;
RWStructuredBuffer<float2> _CenterUVResult;
RWStructuredBuffer<int> _DuplicateResult;

uint _TriangleCount;
float _Epsilon;

static float _SquareEpsilon = _Epsilon * _Epsilon;

bool3 ShareEdge_int(int a, int b, int c, int d, int e, int f)
{
    bool3 dd = bool3(all(a == d), all(b == d), all(c == d));
    bool3 ee = bool3(all(a == e), all(b == e), all(c == e));
    bool3 ff = bool3(all(a == f), all(b == f), all(c == f));
    
    return (ee.yzx && dd.zxy) || (ff.yzx && ee.zxy) || (dd.yzx && ff.zxy);
}

bool3 ShareEdge_float2(float2 a, float2 b, float2 c, float2 d, float2 e, float2 f)
{
    bool3 dd = bool3(all(a == d), all(b == d), all(c == d));
    bool3 ee = bool3(all(a == e), all(b == e), all(c == e));
    bool3 ff = bool3(all(a == f), all(b == f), all(c == f));
    
    return (ee.yzx && dd.zxy) || (ff.yzx && ee.zxy) || (dd.yzx && ff.zxy);
}

bool3 ShareEdge_float3(float3 a, float3 b, float3 c, float3 d, float3 e, float3 f)
{
    bool3 dd = bool3(all(a == d), all(b == d), all(c == d));
    bool3 ee = bool3(all(a == e), all(b == e), all(c == e));
    bool3 ff = bool3(all(a == f), all(b == f), all(c == f));
    
    return (ee.yzx && dd.zxy) || (ff.yzx && ee.zxy) || (dd.yzx && ff.zxy);
}

[numthreads(1, 1, 1)]
void Adjacent_Main(uint id : SV_DispatchThreadID)
{
    uint temp0 = id * 3;
    uint index0 = _Triangles[temp0];
    uint index1 = _Triangles[temp0 + 1];
    uint index2 = _Triangles[temp0 + 2];

    float3 a = _Vertices[index0];
    float3 b = _Vertices[index1];
    float3 c = _Vertices[index2];

    float2 g = _UVs[index0];
    float2 h = _UVs[index1];
    float2 i = _UVs[index2];
    
    _CenterUVResult[id] = (g + h + i) / 3.0;

    float temp20 = cross(float3(h - g, 0.0), float3(i - h, 0.0)).z;
    int tempZ0 = sign(temp20) * (abs(temp20) > _SquareEpsilon);

    int3 result = -1;
    
    for (uint index = 0; index < _TriangleCount; ++index)
    {
        uint temp1 = index * 3;
        uint index3 = _Triangles[temp1];
        uint index4 = _Triangles[temp1 + 1];
        uint index5 = _Triangles[temp1 + 2];

        float2 j = _UVs[index3];
        float2 k = _UVs[index4];
        float2 l = _UVs[index5];

        float temp21 = cross(float3(k - j, 0.0), float3(l - k, 0.0)).z;
        int tempZ1 = sign(temp21) * (abs(temp21) > _SquareEpsilon);

        if (index == id || tempZ0 != tempZ1)
            continue;

        float3 d = _Vertices[index3];
        float3 e = _Vertices[index4];
        float3 f = _Vertices[index5];

        bool3 flag = ShareEdge_float2(g, h, i, j, k, l) && ShareEdge_float3(a, b, c, d, e, f);
        result = flag ? index : result;
    }
    
    _AdjacentResult[id] = result;
}

[numthreads(1, 1, 1)]
void Duplicate_Main(uint id : SV_DispatchThreadID)
{
    float2 myUV = _CenterUVResult[id];
    
    for (uint index = 0; index < id; ++index)
    {
        float2 temp0 = _CenterUVResult[index] - myUV;
        
        if (index != id && ((abs(temp0.x) + abs(temp0.y)) < _Epsilon))
        {
            _DuplicateResult[id] = index;
            return;
        }
    }
    
    _DuplicateResult[id] = -1;
}
