//using System;
//using Windows.UI.Xaml;
//using Windows.UI.Xaml.Data;

//namespace WebcaseExtracterUniversal.Converters
//{
//    public class StringFormatConverter : DependencyObject, IValueConverter
//    {
//        //public string StringFormat { get; set; }
//        public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register(
//              "Stringformat",
//              typeof(String),
//              typeof(StringFormatConverter),
//              new PropertyMetadata(null)
//            );

//        public String StringFormat
//        {
//            get { return (String)GetValue(StringFormatProperty); }
//            set { SetValue(StringFormatProperty, value); }
//        }

//        public object Convert(object value, Type targetType, object parameter, string language)
//        {
//            if (!String.IsNullOrEmpty(StringFormat))
//                return String.Format(StringFormat, value);

//            return value;
//        }

//        public object ConvertBack(object value, Type targetType, object parameter, string language)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
