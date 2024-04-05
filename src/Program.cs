using Tools.HitsoundCopier;

namespace MapWizard;

class Program
{
    static void Main(FileInfo input, FileInfo output)
    {
        Console.WriteLine("Copying hitsounds from {0} to {1}", input.FullName, output.FullName);
        HitsoundCopier.CopyFromBeatmap(input.FullName, output.FullName);
    }
}