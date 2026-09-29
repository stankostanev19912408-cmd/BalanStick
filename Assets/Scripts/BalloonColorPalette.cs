using UnityEngine;

[CreateAssetMenu(fileName = "BalloonColorPalette", menuName = "BalanStick/Balloons/Color Palette")]
public sealed class BalloonColorPalette : ScriptableObject
{
    [SerializeField] private Color red;
    [SerializeField] private Color orange;
    [SerializeField] private Color yellow;
    [SerializeField] private Color green;
    [SerializeField] private Color cyan;
    [SerializeField] private Color blue;
    [SerializeField] private Color violet;
    [SerializeField] private Color black;
    [SerializeField] private Color white;
    [SerializeField] private Color gray;

    public Color GetColor(PatternBalloonKind kind)
    {
        switch (kind)
        {
            case PatternBalloonKind.Red: return red;
            case PatternBalloonKind.Orange: return orange;
            case PatternBalloonKind.Yellow: return yellow;
            case PatternBalloonKind.Green: return green;
            case PatternBalloonKind.Cyan: return cyan;
            case PatternBalloonKind.Blue: return blue;
            case PatternBalloonKind.Violet: return violet;
            case PatternBalloonKind.Black: return black;
            case PatternBalloonKind.White: return white;
            case PatternBalloonKind.Gray: return gray;
            default:
                throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "Unknown balloon kind.");
        }
    }
}
