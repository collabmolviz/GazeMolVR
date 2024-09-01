using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Linq;

namespace UMol {
public class PLYExporter {

    //TODO: Support submeshes
    //TODO: Implement the binary version
    public static string writeMesh(Mesh m) {

        CultureInfo culture = CultureInfo.InvariantCulture;

        StringBuilder sb = new StringBuilder();
        int[] tris = m.triangles;
        Vector3[] vertices = m.vertices;
        Vector3[] normals = m.normals;
        Color32[] colors = m.colors32;

        sb.Append("ply\nformat ascii 1.0\nelement vertex ");
        sb.Append(vertices.Length.ToString());
        sb.Append("\nproperty float x\nproperty float y\nproperty float z\nproperty float nx\nproperty float ny\nproperty float nz\nproperty uchar red\nproperty uchar green\nproperty uchar blue\nproperty uchar alpha\nelement face ");
        sb.Append((tris.Length / 3).ToString());
        sb.Append("\nproperty list uchar int vertex_index\nend_header\n");

        for (int i = 0; i < m.vertexCount; i++) {

            sb.Append((-vertices[i].x).ToString("f4", culture));
            sb.Append(" ");
            sb.Append(vertices[i].y.ToString("f4", culture));
            sb.Append(" ");
            sb.Append(vertices[i].z.ToString("f4", culture));
            sb.Append(" ");

            sb.Append((-normals[i].x).ToString("f4", culture));
            sb.Append(" ");
            sb.Append(normals[i].y.ToString("f4", culture));
            sb.Append(" ");
            sb.Append(normals[i].z.ToString("f4", culture));
            sb.Append(" ");

            sb.Append(colors[i].r.ToString());
            sb.Append(" ");
            sb.Append(colors[i].g.ToString());
            sb.Append(" ");
            sb.Append(colors[i].b.ToString());
            sb.Append(" ");
            sb.Append(colors[i].a.ToString());

            sb.Append("\n");
            // norms.Add(-normals[i].x);
            // norms.Add(normals[i].y);
            // norms.Add(normals[i].z);

            // cols.Add(colors[i].r);
            // cols.Add(colors[i].g);
            // cols.Add(colors[i].b);
            // cols.Add(colors[i].a);
        }
        for (int i = 0; i < tris.Length / 3; i++) {
            sb.Append("3 ");
            sb.Append(tris[i * 3]);
            sb.Append(" ");

            sb.Append(tris[i * 3 + 2]);
            sb.Append(" ");

            sb.Append(tris[i * 3 + 1]);
            sb.Append("\n");
        }

        return sb.ToString();

    }
}
}