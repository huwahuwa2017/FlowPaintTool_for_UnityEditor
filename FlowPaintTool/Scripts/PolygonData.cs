using UnityEngine;

namespace FlowPaintTool
{
    public class PolygonData
    {
        private int _subMeshIndex = 0;
        private int _triangleIndex = 0;
        private int _indexA = 0;
        private int _indexB = 0;
        private int _indexC = 0;

        private Vector3 _center = Vector3.zero;

        public int SubMeshIndex => _subMeshIndex;

        public int TriangleIndex => _triangleIndex;

        public int IndexA => _indexA;

        public int IndexB => _indexB;

        public int IndexC => _indexC;

        public Vector3 Center => _center;

        public bool DuplicateUV { get; set; }

        public bool MaskResult { get; set; }

        public bool Mask { get; set; }

        public Vector3Int AdjacentPolygonIndex { get; set; }

        public PolygonData(int subMeshIndex, int triangleIndex, int indexA, int indexB, int indexC)
        {
            _subMeshIndex = subMeshIndex;
            _triangleIndex = triangleIndex;
            _indexA = indexA;
            _indexB = indexB;
            _indexC = indexC;
        }

        public void CenterRecalculation(Vector3[] vertexPositionArray)
        {
            _center = (vertexPositionArray[_indexA] + vertexPositionArray[_indexB] + vertexPositionArray[_indexC]) / 3f;
        }
    }
}