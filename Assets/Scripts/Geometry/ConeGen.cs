using UnityEngine;

[RequireComponent (typeof (MeshFilter))]
public class ConeGen : MonoBehaviour {

	[SerializeField]
	[Range(3,32)]
	public int subdivisions = 10;
	public float radius = 1f;
	public float height = 2f;
	public bool upsideDown = false;

	void Start () {
		GetComponent<MeshFilter>().sharedMesh = Create(subdivisions, radius, height, upsideDown);
	}

	Mesh Create (int subdivisions, float radius, float height, bool upsideDown) {
		Mesh mesh = new Mesh();
		subdivisions = Mathf.Max(subdivisions, 3);
		Vector3[] vertices = new Vector3[subdivisions + 2];
		Vector2[] uv = new Vector2[vertices.Length];
		int[] triangles = new int[(subdivisions * 2) * 3];
		float baseZ = upsideDown ? -height : 0f;
		float apexZ = upsideDown ? 0f : height;

		vertices[0] = new Vector3(0f, 0f, baseZ);
		uv[0] = new Vector2(0.5f, 0f);
		for(int i = 0, n = subdivisions - 1; i < subdivisions; i++) {
			float ratio = (float)i / n;
			float r = ratio * (Mathf.PI * 2f);
			float x = Mathf.Cos(r) * radius;
			float y = -Mathf.Sin(r) * radius;
			vertices[i + 1] = new Vector3(x, y, baseZ);

			uv[i + 1] = new Vector2(ratio, 0f);
		}
		vertices[subdivisions + 1] = new Vector3(0f, 0f, apexZ);
		uv[subdivisions + 1] = new Vector2(0.5f, 1f);

		// construct base cap

		for(int i = 0, n = subdivisions - 1; i < n; i++) {
			int offset = i * 3;
			triangles[offset] = 0; 
			triangles[offset + 1] = i + 1; 
			triangles[offset + 2] = i + 2; 
		}

		// construct sides

		int bottomOffset = subdivisions * 3;
		for(int i = 0, n = subdivisions - 1; i < n; i++) {
			int offset = i * 3 + bottomOffset;
			triangles[offset] = i + 1; 
			triangles[offset + 1] = subdivisions + 1; 
			triangles[offset + 2] = i + 2; 
		}

		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();

		return mesh;
	}

}
