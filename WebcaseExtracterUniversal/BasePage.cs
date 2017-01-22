using System.Reflection;
using Windows.UI.Xaml.Controls;

namespace WebcaseExtracterUniversal
{
    public class BasePage : Page
    {
        private const string ViewModelPropertyName = "ViewModel";

        public BasePage()
        {
            DataContextChanged += (s, e) =>
            {
                SyncVmWithDataContext();
            };
        }

        public void SyncVmWithDataContext()
        {
            var vmProperty = this.GetType().GetProperty(ViewModelPropertyName);
            if (vmProperty != null)
            {
                vmProperty.SetMethod.Invoke(this, new[] { DataContext });
            }
        }
    }
}
