using System.IO;
using System.Text;
using BeatmapParser;
using MapWizard.Tests;
using ShellProgressBar;
using Tools.HitsoundCopier;

namespace MapWizard;

class Program
{
    static void Main(FileInfo input, FileInfo output)
    {
        HitsoundCopier.CopyFromBeatmap(input.FullName, output.FullName);
    }
}