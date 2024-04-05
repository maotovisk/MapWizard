using System.IO;
using System.Text;
using BeatmapParser;
using MapWizard.Tests;
using ShellProgressBar;

namespace MapWizard;

class Program
{
    static void Main(FileInfo input, FileInfo output)
    {
        Decoding.DecodeAllMapsFrom(@"/mnt/SSD1TB/osu/Songs");
    }
}