﻿using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using MoneyFox.Business.Helpers;
using MoneyFox.Business.ViewModels;
using MoneyFox.Foundation.DataModels;
using MvvmCross.Platform;

namespace MoneyFox.Windows.Views
{
    public sealed partial class ModifyAccountView
    {
        public ModifyAccountView()
        {
            InitializeComponent();
            DataContext = Mvx.Resolve<ModifyAccountViewModel>();

            // code to handle bottom app bar when keyboard appears
            // workaround since otherwise the keyboard would overlay some controls
            InputPane.GetForCurrentView().Showing +=
                (s, args) => { BottomCommandBar.Visibility = Visibility.Collapsed; };
            InputPane.GetForCurrentView().Hiding += (s, args2) =>
            {
                if (BottomCommandBar.Visibility == Visibility.Collapsed)
                {
                    BottomCommandBar.Visibility = Visibility.Visible;
                }
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var viewModel = (ModifyAccountViewModel) DataContext;

            var account = e.Parameter as AccountViewModel;
            if (account != null)
            {
                viewModel.IsEdit = true;
                viewModel.SelectedAccountViewModel = account;
            }
            else
            {
                viewModel.IsEdit = false;
                viewModel.SelectedAccountViewModel = new AccountViewModel();
            }

            base.OnNavigatedTo(e);
        }


        private void TextBoxOnFocus(object sender, RoutedEventArgs e)
        {
            TextBoxCurrentBalance.SelectAll();
        }

        private void FormatTextBoxOnLostFocus(object sender, RoutedEventArgs e)
        {
            double amount;
            double.TryParse(TextBoxCurrentBalance.Text, out amount);
            TextBoxCurrentBalance.Text = Utilities.FormatLargeNumbers(amount);
        }
    }
}