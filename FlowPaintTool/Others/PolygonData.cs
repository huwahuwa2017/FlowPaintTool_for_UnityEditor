using UnityEngine;

namespace FlowPaintTool
{
    public class PolygonData
    {
        private int _indexA = 0;
        private int _indexB = 0;
        private int _indexC = 0;
        private Vector3 _center = Vector3.zero;

        public int IndexA => _indexA;

        public int IndexB => _indexB;

        public int IndexC => _indexC;

        public Vector3 Center => _center;

        public bool DuplicateUV { get; set; }

        public bool MaskResult { get; set; }

        public bool Mask { get; set; }

        public PolygonData(int indexA, int indexB, int indexC)
        {
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