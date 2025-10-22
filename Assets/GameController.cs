using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour {
    [Header("Refs")]
    [SerializeField] private GridManager grid;
    [Header("UI (Legacy)")]
    [SerializeField] private Button playPauseBtn, stepBtn, randomBtn, clearBtn, startMatchBtn, p1BrushBtn, p2BrushBtn;
    [SerializeField] private Slider speedSlider;
    [SerializeField] private Text genText, scoreText, stateText;

    public enum Mode { Single, PvP }
    [SerializeField] private Mode mode = Mode.PvP;

    private enum GameState { Editing, Running, Paused }
    private GameState state = GameState.Editing;

    private Owner brush = Owner.P1;
    private int gen = 0, p1 = 0, p2 = 0;
    private Coroutine loop;
    private bool revealScore = false;


    private const float MinDelay = 0.02f;
    private const float MaxDelay = 1.5f;
    private const double RandomFill = 0.20;


    private Text _playBtnText;

    private void Start() {
        if (grid == null) grid = FindObjectOfType<GridManager>();
        if (grid == null) {
            Debug.LogError("GameController: assign GridManager on the Game object.");
            enabled = false;
            return;
        }

        grid.Build(this);

        _playBtnText = playPauseBtn.GetComponentInChildren<Text>();

        playPauseBtn.onClick.AddListener(OnPlayPause);
        stepBtn.onClick.AddListener(StepAction);
        randomBtn.onClick.AddListener(FillRandom);
        clearBtn.onClick.AddListener(ClearAll);

        startMatchBtn.onClick.AddListener(ToggleMode);
        p1BrushBtn.onClick.AddListener(() => SetBrush(Owner.P1));
        p2BrushBtn.onClick.AddListener(() => SetBrush(Owner.P2));

        RefreshModeUI();
        UpdateUI();
        stateText.text = "Editing…";
    }


    private void ToggleMode() {
        mode = (mode == Mode.PvP) ? Mode.Single : Mode.PvP;
        p2BrushBtn.interactable = (mode == Mode.PvP);
        if (mode == Mode.Single) SetBrush(Owner.P1);
        RefreshModeUI();
    }

    private void RefreshModeUI() {
        var t = startMatchBtn.GetComponentInChildren<Text>();
        if (t != null) t.text = (mode == Mode.PvP) ? "Mode: PvP" : "Mode: Single";
    }


    public void OnCellClicked(Cell c, bool right) {
        if (state == GameState.Running) return;

        if (right) {
            c.SetAlive(false, Owner.None);
            UpdateUI();
            return;
        }
        
        if (c.Alive) {
            c.SetAlive(false, Owner.None);
            UpdateUI();
            return;
        }

        if (mode == Mode.Single && brush == Owner.P2) return;
        c.SetAlive(true, brush);
        UpdateUI();
    }

    private void SetBrush(Owner o) {
        brush = o;
        stateText.text = state == GameState.Editing
            ? $"Editing… Brush: {(o == Owner.P1 ? "P1" : "P2")}"
            : $"Brush: {(o == Owner.P1 ? "P1 white" : "P2 black")}";
    }


    private void ClearAll() {
        StopLoop();
        gen = 0;
        p1 = 0;
        p2 = 0;
        revealScore = false;
        state = GameState.Editing;

        grid.ClearAll();
        stateText.text = "Editing…";
        UpdateUI();
    }

    private void FillRandom() {
        if (state == GameState.Running) return;

        var cells = grid.Cells;
        var rnd = new System.Random();

        for (int x = 0; x < cells.GetLength(0); x++) {
            for (int y = 0; y < cells.GetLength(1); y++) {
                bool alive = rnd.NextDouble() < RandomFill;
                if (!alive) {
                    cells[x, y].SetAlive(false, Owner.None);
                    continue;
                }
                Owner o = (mode == Mode.PvP)
                    ? (rnd.NextDouble() < 0.5 ? Owner.P1 : Owner.P2) : Owner.P1;
                cells[x, y].SetAlive(true, o);
            }
        }
        UpdateUI();
    }

    private void OnPlayPause() {
        if (state == GameState.Running) {
            StopLoop();
            state = GameState.Paused;
            revealScore = true;
            stateText.text = "Pause";
        } else {
            state = GameState.Running;
            revealScore = false;
            stateText.text = "Running…";
            loop = StartCoroutine(Loop());
        }

        UpdateUI();
    }

    private void StepAction() {
        StepOnce();
        UpdateUI();
    }

    private IEnumerator Loop() {
        while (true) {
            StepOnce();
            float delay = Mathf.Clamp(speedSlider.value, MinDelay, MaxDelay);
            yield return new WaitForSeconds(delay);
        }
    }

    private void StopLoop() {
        if (loop != null) {
            StopCoroutine(loop);
            loop = null;
        }
    }

    private void StepOnce() {
        var cells = grid.Cells;
        int w = cells.GetLength(0);
        int h = cells.GetLength(1);

        bool[,] next = new bool[w, h];
        Owner[,] own = new Owner[w, h];

        int birthsP1 = 0, birthsP2 = 0;
        int aliveP1Next = 0, aliveP2Next = 0;

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                var c = cells[x, y];
                var nb = grid.CountNeighbors(x, y);

                if (c.Alive) {
                    next[x, y] = (nb.alive == 2 || nb.alive == 3);
                    own[x, y] = c.Owner;
                } else {
                    bool born = (nb.alive == 3);
                    next[x, y] = born;

                    if (born) {
                        Owner bornOwner = (mode == Mode.PvP)
                            ? (nb.p1 > nb.p2 ? Owner.P1 : Owner.P2)
                            : Owner.P1;

                        own[x, y] = bornOwner;
                        if (bornOwner == Owner.P1) birthsP1++; else birthsP2++;
                    }
                }

                if (next[x, y]) {
                    if (own[x, y] == Owner.P1) aliveP1Next++; else aliveP2Next++;
                }
            }
        }

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                cells[x, y].SetAlive(next[x, y], next[x, y] ? own[x, y] : Owner.None);
            }
        }

        p1 += birthsP1;
        p2 += birthsP2;
        gen++;
        if (mode == Mode.PvP && (aliveP1Next == 0 || aliveP2Next == 0)) {
            StopLoop();
            state = GameState.Paused;
            revealScore = true;

            stateText.text = (aliveP1Next == 0 && aliveP2Next == 0)
                ? "Finished: no live cells"
                : (aliveP1Next == 0 ? "Finished: P1 eliminated" : "Finished: P2 eliminated");
        }
    }

    private void UpdateUI() {
        genText.text = $"Gen: {gen}";
        scoreText.text = (revealScore && state != GameState.Editing)
            ? $"P1: {p1} | P2: {p2}"
            : "P1: — | P2: —";
            
        if (_playBtnText != null) _playBtnText.text = (state == GameState.Running) ? "Pause" : "Play";
    }
}
