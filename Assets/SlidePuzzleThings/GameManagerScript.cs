using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManangerScript : MonoBehaviour
{
    [SerializeField] private Transform gameTransform;
    [SerializeField] private Transform piecePrefab;
    private int emptyLocation;
    private int size ;
  
    private List<Transform> pieces;

    private void CreateGamePiece(float gapThickness)
    {
        float width = 1 / (float)size;
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                Transform piece = Instantiate(piecePrefab, gameTransform);
                pieces.Add(piece);
                piece.localPosition = new Vector3(-1f + (2f * width * col) + width,
                                                 1f-(2f * width * row) - width, 0f);
                float pieceSize = (2f * width) - gapThickness;

                piece.localScale = new Vector3(
                    pieceSize,
                    pieceSize,
                    1f
                );
                piece.name = $"{(row*size)+col}";
                SetPieceUV(piece, row, col);
                if ((row == size - 1) && (col == size - 1))
                {
                    emptyLocation = (size * size) - 1;
                    piece.gameObject.SetActive(false);
                }
            }
        }
    }
  // SET IMAGE SECTION FOR EACH PIECE
    private void SetPieceUV(Transform piece, int row, int col)
    {
        MeshFilter meshFilter = piece.GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            Debug.LogError("PuzzlePiece needs a MeshFilter.");
            return;
        }
        Mesh mesh = meshFilter.mesh;

        // CALCULATE UV COORDINATES
        float uvWidth = 1f / size;
        float uvHeight = 1f / size;

        float uMin = col * uvWidth;
        float uMax = (col + 1) * uvWidth;

        // Unity UV coordinates start from the bottom
        // while our puzzle rows start from the top.
        float vMax = 1f - (row * uvHeight);
        float vMin = 1f - ((row + 1) * uvHeight);

        // APPLY UV COORDINATES TO QUAD
        Vector2[] uv = new Vector2[mesh.uv.Length];

        for (int i = 0; i < mesh.uv.Length; i++)
        {
            Vector2 originalUV = mesh.uv[i];

            float u = Mathf.Lerp(uMin,uMax,originalUV.x);
            float v = Mathf.Lerp(vMin, vMax, originalUV.y);
            uv[i] = new Vector2(u, v);
        }
         mesh.uv = uv;
    }
    
    void Start()
    {
        pieces = new List<Transform>();
        size = 3;
        CreateGamePiece(0.03f);
        Shuffle();
    }
    private void Update()
    {
        //Check completion
        if (CheckCompletion())
        {
            Debug.Log("Puzzle Completed");
            //OPEN DOOR AND START COUNTDOWN
        }
        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("No Main Camera found.");
                return;
            }
         // Create a ray from the camera through the mouse position
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // 3D raycast
            if (Physics.Raycast(ray, out hit))
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    if (pieces[i] == hit.transform)
                    {
                        Debug.Log($"Clicked on a piece");
                        if (SwapIfValid(i, -size, size)) { break; }//moveup
                        if (SwapIfValid(i, +size, size)) { break; }//moveDown
                        if (SwapIfValid(i, -1,0)) { break; }//moveLeft
                        if (SwapIfValid(i, +1,size-1)) { break; }//moveRight
                    }
                }
            }
        }
    }

    private bool SwapIfValid(int i, int offset, int colCheck)
    {
        if (((i % size) != colCheck) && ((i + offset) == emptyLocation))
        {
            (pieces[i], pieces[i + offset]) = (pieces[i + offset], pieces[i]);

            (pieces[i].localPosition, pieces[i + offset].localPosition) =
                (pieces[i + offset].localPosition, pieces[i].localPosition);

            emptyLocation = i;
            return true;
        }
        return false;
    }
    private bool CheckCompletion()
    {
       for(int i =0; i< pieces.Count; i++)
        if (pieces[i].name != $"{i}")
        {
            return false;
        }
        return true;
    }
    private void Shuffle()
    {
        int count = 0;
        int last = -1;
         while(count < (size * size * size))
        {
            int rand = Random.Range(0, size * size);
            if(rand == last) { continue; }
            last = emptyLocation;
            if (SwapIfValid(rand, -size, size))
            {count++; }
            else if(SwapIfValid( rand, +size, size)) { count++; }
            else if(SwapIfValid(rand, -1, 0)) { count++; }
            else if (SwapIfValid(rand, +1, size - 1)) { count++; }
        }
    }
}

