using ReactiveUI;

namespace MapWizard.Desktop.ViewModels
{
    public class HitsoundCopierViewModel : ViewModelBase
    {
        public string Message { get; set; }

        public HitsoundCopierViewModel()
        {
            Message = "Hitsound Copier View";
        }
    }
}