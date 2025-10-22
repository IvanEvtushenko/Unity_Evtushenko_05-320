using UnityEngine;

public enum Owner { None = 0, P1 = 1, P2 = 2 }

public class Cell : MonoBehaviour {
    public int X, Y;
    public bool Alive { get; private set; }
    public Owner Owner { get; private set; } = Owner.None;

    private SpriteRenderer _sr;
    private GameController _ctrl;

    private static readonly Color DeadColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    private static readonly Color P1Color = Color.white;
    private static readonly Color P2Color = Color.black;

    public void Init(int x, int y, GameController ctrl) {
        X = x;
        Y = y;
        _ctrl = ctrl;
        _sr = GetComponent<SpriteRenderer>();
        SetAlive(false, Owner.None);
    }

    public void SetAlive(bool alive, Owner owner) {
        Alive = alive;
        Owner = alive ? owner : Owner.None;
        _sr.color = Alive ? (Owner == Owner.P1 ? P1Color : P2Color) : DeadColor;
    }

    private void OnMouseDown() {
        if (_ctrl == null) return;
        bool right = Input.GetMouseButton(1);
        _ctrl.OnCellClicked(this, right);
    }
}
