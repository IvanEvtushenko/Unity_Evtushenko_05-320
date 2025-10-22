using UnityEngine;

public class GridManager : MonoBehaviour {
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private int width = 35, height = 25;
    [SerializeField] private float spacing = 0.02f;
    [SerializeField] private float offsetX = 5f;

    public Cell[,] Cells { get; private set; }

    public void Build(GameController ctrl) {
        Cells = new Cell[width, height];

        float s = 1f;
        float totalW = width * s + (width - 1) * spacing;
        float totalH = height * s + (height - 1) * spacing;

        Vector2 origin = new Vector2(
            -totalW * 0.5f + s * 0.5f + offsetX,
            -totalH * 0.5f + s * 0.5f
        );

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                var go = Instantiate(cellPrefab, transform);
                go.transform.position = new Vector3(
                    origin.x + x * (s + spacing),
                    origin.y + y * (s + spacing),
                    0f
                );

                var c = go.GetComponent<Cell>();
                c.Init(x, y, ctrl);
                Cells[x, y] = c;
            }
        }

        var cam = Camera.main;
        if (cam && cam.orthographic) cam.orthographicSize = totalH * 0.55f;
    }

    public (int alive, int p1, int p2) CountNeighbors(int x, int y) {
        int alive = 0, p1 = 0, p2 = 0;

        for (int dx = -1; dx <= 1; dx++) {
            for (int dy = -1; dy <= 1; dy++) {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= Cells.GetLength(0) || ny >= Cells.GetLength(1)) continue;

                var c = Cells[nx, ny];
                if (!c.Alive) continue;

                alive++;
                if (c.Owner == Owner.P1) p1++; else p2++;
            }
        }

        return (alive, p1, p2);
    }

    public void ClearAll() {
        foreach (var c in Cells) {
            c.SetAlive(false, Owner.None);
        }
    }
}
