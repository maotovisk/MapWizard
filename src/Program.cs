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
#if DEBUG
        input = new FileInfo("/home/maot/.local/share/osu-wine/osu!/Songs/beatmap-638476587700114431-sem-título/desconhecido - desconhecido (maot) [0].osu");
        output = new FileInfo("/home/maot/.local/share/osu-wine/osu!/Songs/beatmap-638476587700114431-sem-título/desconhecido - desconhecido (maot) [0].osu");
#endif

        HitsoundCopier.CopyFromBeatmap(input.FullName, output.FullName);
    }
}