using UnityEngine;
using TMPro;

public class TMPFloat : MonoBehaviour
{
    public float amplitude = 3f;
    public float frequency = 2f;
    public float speed = 1f;

    private TMP_Text text;
    private Mesh mesh;
    private Vector3[] vertices;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    void Update()
    {
        text.ForceMeshUpdate();
        mesh = text.mesh;
        vertices = mesh.vertices;

        for (int i = 0; i < text.textInfo.characterCount; i++)
        {
            TMP_CharacterInfo c = text.textInfo.characterInfo[i];

            if (!c.isVisible) continue;

            int index = c.vertexIndex;

            float offset = Mathf.Sin(Time.time * speed + i * frequency) * amplitude;

            Vector3 move = new Vector3(0, offset, 0);

            vertices[index + 0] += move;
            vertices[index + 1] += move;
            vertices[index + 2] += move;
            vertices[index + 3] += move;
        }

        mesh.vertices = vertices;
        text.canvasRenderer.SetMesh(mesh);
    }
}