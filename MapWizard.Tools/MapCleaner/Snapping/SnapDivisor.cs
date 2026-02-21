namespace MapWizard.Tools.MapCleaner.Snapping;

public readonly record struct SnapDivisor(int Numerator, int Denominator)
{
    public override string ToString() => $"{Numerator}/{Denominator}";
}
