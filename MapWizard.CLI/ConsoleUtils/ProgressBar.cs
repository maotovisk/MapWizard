namespace MapWizard.CLI.ConsoleUtils;

public class ProgressBar
{
    public static void DrawProgressBar(int total, int current, string dotText = ".")
    {
        int progressBarLength = 20;
        double progress = (double)current / total;
        int progressChars = (int)(progress * progressBarLength);

        string progressBar = new string('#', progressChars) + new string('-', progressBarLength - progressChars);
        string progressPercentage = (progress * 100).ToString("0.00") + "%";

        Console.Write("\r[{0}] {1}/{2} {3}", progressBar, current, total, progressPercentage.PadLeft(6));

        if (!string.IsNullOrEmpty(dotText))
        {
            for (int i = 0; i < current % progressBarLength; i++)
            {
                Console.Write(dotText);
            }
        }

        if (current == total)
        {
            Console.WriteLine();
        }
    }
}