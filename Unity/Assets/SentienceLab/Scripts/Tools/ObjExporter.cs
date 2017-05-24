#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Class for exporting meshes within a GameObject as an OBJ file.
/// Source: http://wiki.unity3d.com/index.php/ObjExporter
/// </summary>
///
public class ObjExporter
{
	public static void SaveMeshes(Transform t, string baseFilename)
	{
		materialList = new Dictionary<string, Material>();
		indexOffset  = 0;

		meshString     = new StringBuilder();
		meshString.Append("#" + t.gameObject.name
							+ "\n#" + System.DateTime.Now.ToLongDateString()
							+ "\n#" + System.DateTime.Now.ToLongTimeString()
							+ "\n#-------"
							+ "\n\n"
							+ "mtllib " + baseFilename + ".mtl"
							+ "\n\n");

		ProcessTransform(t);
		using (StreamWriter sw = new StreamWriter(baseFilename + ".obj"))
		{
			sw.Write(meshString);
		}

		materialString = new StringBuilder();
		ProcessMaterials();
		using (StreamWriter sw = new StreamWriter(baseFilename + ".mtl"))
		{
			sw.Write(materialString);
		}

	}


	static void ProcessTransform(Transform t)
	{
		string objectName = t.name.Replace(' ', '_');
		meshString.Append("#" + objectName
						+ "\n#-------"
						+ "\n");
		meshString.Append("g ").Append(objectName).Append("\n");

		MeshFilter mf = t.GetComponent<MeshFilter>();
		if (mf)
		{
			ProcessMesh(mf, t);
		}

		for (int i = 0; i < t.childCount; i++)
		{
			ProcessTransform(t.GetChild(i));
		}

		meshString.Append("\n");
	}


	public static void ProcessMesh(MeshFilter mf, Transform t)
	{
		int numVertices = 0;

		Mesh m = mf.mesh;
		if (!m)
		{
			meshString.Append("####Error####\n");
			return;
		}

		foreach (Vector3 vv in m.vertices)
		{
			Vector3 v = t.TransformPoint(vv);
			numVertices++;
			meshString.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, -v.z));
		}
		meshString.Append("\n");
		foreach (Vector3 nn in m.normals)
		{
			Vector3 v = t.TransformDirection(nn);
			meshString.Append(string.Format("vn {0} {1} {2}\n", -v.x, -v.y, v.z));
		}
		meshString.Append("\n");
		foreach (Vector3 v in m.uv)
		{
			meshString.Append(string.Format("vt {0} {1}\n", v.x, v.y));
		}

		Material[] mats = mf.GetComponent<MeshRenderer>().materials;
		for (int material = 0; material < m.subMeshCount; material++)
		{
			Material mat = mats[material % mats.Length];
			string matName = mat.name.Replace(" (Instance)", "").Replace(' ', '_').Replace("__", "_");

			if (!materialList.ContainsKey(matName))
			{
				materialList.Add(matName, mat);
			}

			meshString.Append("\n");
			meshString.Append("usemtl ").Append(matName).Append("\n");
			meshString.Append("usemap ").Append(matName).Append("\n");

			int[] triangles = m.GetTriangles(material);
			for (int i = 0; i < triangles.Length; i += 3)
			{
				meshString.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
					triangles[i    ] + 1 + indexOffset, 
					triangles[i + 1] + 1 + indexOffset, 
					triangles[i + 2] + 1 + indexOffset));
			}
		}

		indexOffset += numVertices;
	}


	private static void ProcessMaterials()
	{
		materialString = new StringBuilder();
		foreach (string materialName in materialList.Keys)
		{
			ProcessMaterial(materialName, materialList[materialName]);
			materialString.Append("\n");
		}
	}


	private static void ProcessMaterial(string materialName, Material material)
	{
		materialString.Append("newmtl ").Append(materialName).Append("\n");
		Color col = material.color;
		materialString.Append(string.Format("Kd {0:F3} {1:F3} {2:F3}\n", col.r, col.g, col.b));
	}


	private static int                          indexOffset;
	private static StringBuilder                meshString;
	private static Dictionary<string, Material> materialList;
	private static StringBuilder                materialString;

}