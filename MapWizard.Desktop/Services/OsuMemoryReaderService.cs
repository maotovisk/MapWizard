using System;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;
using MapWizard.Desktop.Models;
using OsuWineMemReader;

namespace MapWizard.Desktop.Services;

public class OsuMemoryReaderService : IOsuMemoryReaderService
{
    public Result<string> GetBeatmapPath()
    {
        if (!OperatingSystem.IsLinux())
        {
            return new Result<string>()
            {
                Value = null,
                Status = ResultStatus.Error,
                ErrorMessage = "This feature is not yet supported on your operating system."
            };
        }
        
        var options = new OsuMemory.OsuMemoryOptions()
        {
            WriteToFile = false,
            RunOnce = true
        };
        
        var running = true;
        try
        {

            OsuMemory.StartBeatmapPathReading(ref running, out var beatmapPath, options);

            return new Result<string>()
            {
                Value = beatmapPath,
                Status = ResultStatus.Success,
                ErrorMessage = null
            };

        }
        catch (Exception exception)
        {
            return new Result<string>()
            {
                Value = null,
                Status = ResultStatus.Error,
                ErrorMessage = exception.Message
            };
        }
    }
}