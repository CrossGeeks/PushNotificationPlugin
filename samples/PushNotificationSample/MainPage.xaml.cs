using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PushNotificationSample
{
    public partial class MainPage : ContentPage
    {

        public string Message
        {
            get
            {
                return textLabel.Text;
            }
            set
            {
                textLabel.Text = value;
            }
        }

        public MainPage()
        {
            InitializeComponent();
        }
    }
}
