﻿using GalaSoft.MvvmLight;
using Microsoft.Practices.ServiceLocation;
using MoneyManager.DataAccess;
using MoneyManager.Models;
using MoneyManager.Src;
using PropertyChanged;
using System.Collections.ObjectModel;

namespace MoneyManager.ViewModels
{
    [ImplementPropertyChanged]
    public class AddTransactionUserControlViewModel : ViewModelBase
    {
        public ObservableCollection<Account> AllAccounts
        {
            get { return ServiceLocator.Current.GetInstance<AccountDataAccess>().AllAccounts; }
        }        
        
        public FinancialTransaction SelectedTransaction
        {
            get { return ServiceLocator.Current.GetInstance<TransactionDataAccess>().SelectedTransaction; }
            set { ServiceLocator.Current.GetInstance<TransactionDataAccess>().SelectedTransaction = value; }
        }

        public bool IsEdit { get; set; }

        public TransactionType TransactionType { get; set; }
    }
}