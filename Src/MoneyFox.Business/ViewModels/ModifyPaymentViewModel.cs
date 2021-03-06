﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MoneyFox.Business.Helpers;
using MoneyFox.Foundation;
using MoneyFox.Foundation.DataModels;
using MoneyFox.Foundation.Interfaces;
using MoneyFox.Foundation.Interfaces.Repositories;
using MoneyFox.Foundation.Messages;
using MoneyFox.Foundation.Resources;
using MvvmCross.Core.ViewModels;
using MvvmCross.Localization;
using MvvmCross.Plugins.Messenger;

namespace MoneyFox.Business.ViewModels
{
    public class ModifyPaymentViewModel : BaseViewModel
    {
        private readonly IDialogService dialogService;
        private readonly IPaymentManager paymentManager;
        private readonly IPaymentRepository paymentRepository;
        private readonly ISettingsManager settingsManager;

        //this token ensures that we will be notified when a message is sent.
        private readonly MvxSubscriptionToken token;

        // This has to be static in order to keep the value even if you leave the page to select a CategoryViewModel.
        private double amount;
        private PaymentViewModel selectedPayment;
        private string recurrenceString;
        private DateTime endDate;
        private bool isEndless;
        private bool isTransfer;
        private bool isEdit;
        private int paymentId;

        public ModifyPaymentViewModel(IPaymentRepository paymentRepository,
            IAccountRepository accountRepository,
            IDialogService dialogService,
            IPaymentManager paymentManager, ISettingsManager settingsManager)
        {
            this.dialogService = dialogService;
            this.paymentManager = paymentManager;
            this.settingsManager = settingsManager;
            this.paymentRepository = paymentRepository;

            TargetAccounts = new ObservableCollection<AccountViewModel>(accountRepository.GetList());
            ChargedAccounts = new ObservableCollection<AccountViewModel>(TargetAccounts);

            token = MessageHub.Subscribe<CategorySelectedMessage>(ReceiveMessage);
        }

        /// <summary>
        ///     Provides an TextSource for the translation binding on this page.
        /// </summary>
        public IMvxLanguageBinder TextSource => new MvxLanguageBinder("", GetType().Name);

        public int PaymentId
        {
            get { return paymentId; }
            private set
            {
                paymentId = value; 
                RaisePropertyChanged();
            }
        }

        private int GetEnumIntFromString => RecurrenceList.IndexOf(RecurrenceString);

        /// <summary>
        ///     Init the view for a new PaymentViewModel. Is executed after the constructor call.
        /// </summary>
        /// <param name="type">Type of the PaymentViewModel. Is ignored when paymentId is passed.</param>
        /// <param name="paymentId">The id of the PaymentViewModel to edit.</param>
        public void Init(PaymentType type, int paymentId = 0)
        {
            if (paymentId == 0)
            {
                IsEdit = false;
                IsEndless = true;

                amount = 0;
                PrepareDefault(type);
            }
            else
            {
                IsEdit = true;
                PaymentId = paymentId;
                selectedPayment = paymentRepository.FindById(PaymentId);
                PrepareEdit();
            }

            AccountViewModelBeforeEdit = SelectedPayment.ChargedAccount;
        }

        private void PrepareDefault(PaymentType type)
        {
            SetDefaultPayment(type);
            IsTransfer = type == PaymentType.Transfer;
            EndDate = DateTime.Now;
        }

        private void PrepareEdit()
        {
            IsTransfer = SelectedPayment.IsTransfer;
            // set the private amount property. This will get properly formatted and then displayed.
            amount = SelectedPayment.Amount;
            RecurrenceString = SelectedPayment.IsRecurring
                ? RecurrenceList[SelectedPayment.RecurringPayment.Recurrence]
                : "";
            EndDate = SelectedPayment.IsRecurring
                ? SelectedPayment.RecurringPayment.EndDate
                : DateTime.Now;
            IsEndless = !SelectedPayment.IsRecurring || SelectedPayment.RecurringPayment.IsEndless;

            // we have to set the AccountViewModel objects here again to ensure that they are identical to the
            // objects in the AccountViewModel collections.
            selectedPayment.ChargedAccount =
                ChargedAccounts.FirstOrDefault(x => x.Id == selectedPayment.ChargedAccountId);
            selectedPayment.TargetAccount =
                TargetAccounts.FirstOrDefault(x => x.Id == selectedPayment.TargetAccountId);
        }

        private void SetDefaultPayment(PaymentType paymentType)
        {
            SelectedPayment = new PaymentViewModel
            {
                Type = (int) paymentType,
                Date = DateTime.Now,
                // Assign empty CategoryViewModel to reset the GUI
                Category = new CategoryViewModel(),
                ChargedAccount = DefaultHelper.GetDefaultAccount(ChargedAccounts.ToList())
            };
        }

        /// <summary>
        ///     Moved to own method for debugg reasons
        /// </summary>
        /// <param name="message">Message sent.</param>
        private void ReceiveMessage(CategorySelectedMessage message)
        {
            if ((SelectedPayment == null) || (message == null))
            {
                return;
            }
            SelectedPayment.Category = message.SelectedCategory;
        }

        private async void Save()
        {
            if (SelectedPayment.ChargedAccount == null)
            {
                ShowAccountRequiredMessage();
                return;
            }

            if (SelectedPayment.IsRecurring && !IsEndless && (EndDate.Date <= DateTime.Today))
            {
                ShowInvalidEndDateMessage();
                return;
            }

            // Make sure that the old amount is removed to not count the amount twice.
            RemoveOldAmount();
            SelectedPayment.Amount = amount;

            //Create a recurring PaymentViewModel based on the PaymentViewModel or update an existing
            await PrepareRecurringPayment();

            // Save item or update the PaymentViewModel and add the amount to the AccountViewModel
            var paymentSucceded = paymentManager.SavePayment(SelectedPayment);
            var accountSucceded = paymentManager.AddPaymentAmount(SelectedPayment);
            if (paymentSucceded && accountSucceded)
            {
                settingsManager.LastDatabaseUpdate = DateTime.Now;
            }

            Close(this);
        }

        private void RemoveOldAmount()
        {
            if (IsEdit)
            {
                paymentManager.RemovePaymentAmount(SelectedPayment, AccountViewModelBeforeEdit);
            }
        }

        private async Task PrepareRecurringPayment()
        {
            if ((IsEdit && await paymentManager.CheckRecurrenceOfPayment(SelectedPayment))
                || SelectedPayment.IsRecurring)
            {
                SelectedPayment.RecurringPayment = RecurringPaymentHelper.
                    GetRecurringFromPayment(SelectedPayment,
                        IsEndless,
                        GetEnumIntFromString,
                        EndDate);
            }
        }

        private void OpenSelectCategoryList()
        {
            ShowViewModel<SelectCategoryListViewModel>();
        }

        private async void Delete()
        {
            if (await dialogService.ShowConfirmMessage(Strings.DeleteTitle, Strings.DeletePaymentConfirmationMessage))
            {
                if (await paymentManager.CheckRecurrenceOfPayment(SelectedPayment))
                {
                    paymentManager.RemoveRecurringForPayment(SelectedPayment);
                }

                var paymentSucceded = paymentRepository.Delete(SelectedPayment);
                var accountSucceded = paymentManager.RemovePaymentAmount(SelectedPayment);
                if (paymentSucceded && accountSucceded)
                {
                    settingsManager.LastDatabaseUpdate = DateTime.Now;
                }
                Close(this);
            }
        }

        private async void ShowAccountRequiredMessage()
        {
            await dialogService.ShowMessage(Strings.MandatoryFieldEmptyTitle,
                Strings.AccountRequiredMessage);
        }

        private async void ShowInvalidEndDateMessage()
        {
            await dialogService.ShowMessage(Strings.InvalidEnddateTitle,
                Strings.InvalidEnddateMessage);
        }

        private void ResetSelection()
        {
            SelectedPayment.Category = null;
        }

        private void Cancel()
        {
            SelectedPayment = paymentRepository.FindById(selectedPayment.Id);
            Close(this);
        }

        private void UpdateOtherComboBox()
        {
            var tempCollection = new ObservableCollection<AccountViewModel>(ChargedAccounts);
            foreach (var i in TargetAccounts)
            {
                if (!tempCollection.Contains(i))
                {
                    tempCollection.Add(i);
                }
            }
            foreach (var i in tempCollection)
            {
                if (!TargetAccounts.Contains(i)) //fills targetaccounts
                {
                    TargetAccounts.Add(i);
                }

                if (!ChargedAccounts.Contains(i)) //fills chargedaccounts
                {
                    ChargedAccounts.Add(i);
                }
            }
            ChargedAccounts.Remove(selectedPayment.TargetAccount);
            TargetAccounts.Remove(selectedPayment.ChargedAccount);
        }

        #region Commands

        /// <summary>
        ///     Updates the targetAccountViewModel and chargedAccountViewModel Comboboxes' dropdown lists.
        /// </summary>
        public IMvxCommand SelectedItemChangedCommand => new MvxCommand(UpdateOtherComboBox);

        /// <summary>
        ///     Saves the PaymentViewModel or updates the existing depending on the IsEdit Flag.
        /// </summary>
        public IMvxCommand SaveCommand => new MvxCommand(Save);

        /// <summary>
        ///     Opens to the SelectCategoryView
        /// </summary>
        public IMvxCommand GoToSelectCategorydialogCommand => new MvxCommand(OpenSelectCategoryList);

        /// <summary>
        ///     Delets the PaymentViewModel or updates the existing depending on the IsEdit Flag.
        /// </summary>
        public IMvxCommand DeleteCommand => new MvxCommand(Delete);

        /// <summary>
        ///     Cancels the operations.
        /// </summary>
        public IMvxCommand CancelCommand => new MvxCommand(Cancel);

        /// <summary>
        ///     Resets the CategoryViewModel of the currently selected PaymentViewModel
        /// </summary>
        public IMvxCommand ResetCategoryCommand => new MvxCommand(ResetSelection);

        #endregion

        #region Properties

        /// <summary>
        ///     Indicates if the view is in Edit mode.
        /// </summary>
        public bool IsEdit
        {
            get { return isEdit; }
            private set
            {
                if(isEdit == value) return;
                isEdit = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Indicates if the PaymentViewModel is a transfer.
        /// </summary>
        public bool IsTransfer
        {
            get { return isTransfer; }
            private set
            {
                if(isTransfer == value) return;
                isTransfer = value; 
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Indicates if the reminder is endless
        /// </summary>
        public bool IsEndless
        {
            get { return isEndless; }
            set
            {
                if(isEndless == value) return;
                isEndless = value; 
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     The Enddate for recurring PaymentViewModel
        /// </summary>
        public DateTime EndDate
        {
            get { return endDate; }
            set
            {
                endDate = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     The selected recurrence
        /// </summary>
        public string RecurrenceString
        {
            get { return recurrenceString; }
            set
            {
                recurrenceString = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Property to format amount string to double with the proper culture.
        ///     This is used to prevent issues when converting the amount string to double
        ///     without the correct culture.
        /// </summary>
        public string AmountString
        {
            get { return Utilities.FormatLargeNumbers(amount); }
            set
            {
                double convertedValue;
                if (double.TryParse(value, out convertedValue))
                {
                    amount = convertedValue;
                }
            }
        }

        /// <summary>
        ///     List with the different recurrence types.
        ///     This has to have the same order as the enum
        /// </summary>
        public List<string> RecurrenceList => new List<string>
        {
            Strings.DailyLabel,
            Strings.DailyWithoutWeekendLabel,
            Strings.WeeklyLabel,
            Strings.MonthlyLabel,
            Strings.YearlyLabel,
            Strings.BiweeklyLabel
        };

        /// <summary>
        ///     The selected PaymentViewModel
        /// </summary>
        public PaymentViewModel SelectedPayment
        {
            get { return selectedPayment; }
            set
            {
                if (value == null)
                {
                    return;
                }
                selectedPayment = value;
            }
        }

        /// <summary>
        ///     Gives access to all accounts for Charged Dropdown list
        /// </summary>
        public ObservableCollection<AccountViewModel> ChargedAccounts { get; }

        /// <summary>
        ///     Gives access to all accounts for Target Dropdown list
        /// </summary>
        public ObservableCollection<AccountViewModel> TargetAccounts { get; }

        /// <summary>
        ///     Returns the Title for the page
        /// </summary>
        public string Title => PaymentTypeHelper.GetViewTitleForType(SelectedPayment.Type, IsEdit);

        /// <summary>
        ///     Returns the Header for the AccountViewModel field
        /// </summary>
        public string AccountHeader
            => SelectedPayment?.Type == (int) PaymentType.Income
                ? Strings.TargetAccountLabel
                : Strings.ChargedAccountLabel;

        /// <summary>
        ///     The PaymentViewModel date
        /// </summary>
        public DateTime Date
        {
            get
            {
                if (!IsEdit && (SelectedPayment.Date == DateTime.MinValue))
                {
                    SelectedPayment.Date = DateTime.Now;
                }
                return SelectedPayment.Date;
            }
            set { SelectedPayment.Date = value; }
        }

        private AccountViewModel AccountViewModelBeforeEdit { get; set; }

        #endregion
    }
}