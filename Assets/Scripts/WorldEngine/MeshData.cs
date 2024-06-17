using UnityEngine;

namespace WorldEngine
{
    public struct MeshData
    {
        public readonly int[] triangles;
        public readonly Vector3[] vertices;

        public MeshData(Vector3[] vertices, int[] triangles)
        {
            this.vertices = vertices;
            this.triangles = triangles;
        }
    }
}