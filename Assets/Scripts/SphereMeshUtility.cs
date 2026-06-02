using UnityEngine;

public static class SphereMeshUtility
{

    // Genera un mesh de esfera UV con alta resolución y lo aplica al MeshFilter del GameObject.
    // segments = divisiones horizontales y verticales (64 da un look muy suave).

    public static void Apply(GameObject go, int segments = 64)
    {
        var mf = go.GetComponent<MeshFilter>();
        if (mf == null) return;
        mf.mesh = Create(segments);
    }

// Crea
    public static Mesh Create(int segments = 64)
    {
        var mesh = new Mesh { name = "SmoothSphere" };

        int rings    = segments;
        int slices   = segments;
        int vertCount = (rings + 1) * (slices + 1);

        var vertices  = new Vector3[vertCount];
        var normals   = new Vector3[vertCount];
        var uvs       = new Vector2[vertCount];

        for (int r = 0; r <= rings; r++)
        {
            float phi    = Mathf.PI * r / rings;          // 0 → π
            float sinPhi = Mathf.Sin(phi);
            float cosPhi = Mathf.Cos(phi);

            for (int s = 0; s <= slices; s++)
            {
                float theta    = 2f * Mathf.PI * s / slices; // 0 → 2π
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                int idx = r * (slices + 1) + s;

                var n = new Vector3(sinPhi * cosTheta, cosPhi, sinPhi * sinTheta);
                vertices[idx] = n * 0.5f;  // radio 0.5 para mantener escala Unity estándar
                normals[idx]  = n;
                uvs[idx]      = new Vector2((float)s / slices, 1f - (float)r / rings);
            }
        }

        int triCount = rings * slices * 6;
        var triangles = new int[triCount];
        int t = 0;

        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < slices; s++)
            {
                int a = r       * (slices + 1) + s;
                int b = (r + 1) * (slices + 1) + s;
                int c = (r + 1) * (slices + 1) + s + 1;
                int d = r       * (slices + 1) + s + 1;

                // CCW desde afuera → normales apuntan hacia afuera
                triangles[t++] = a;
                triangles[t++] = c;
                triangles[t++] = b;

                triangles[t++] = a;
                triangles[t++] = d;
                triangles[t++] = c;
            }
        }

        mesh.vertices  = vertices;
        mesh.normals   = normals;
        mesh.uv        = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        return mesh;
    }
}
