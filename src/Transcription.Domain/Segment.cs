namespace Transcription.Domain;

public record Segment(double Start, double End, string Text)
{
    public Segment ShiftBy(double seconds) => this with { Start = Start + seconds, End = End + seconds };
}
