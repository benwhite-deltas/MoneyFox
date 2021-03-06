﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MoneyFox.DataAccess.DatabaseModels;
using MoneyFox.Foundation.DataModels;
using MoneyFox.Foundation.Interfaces;
using MoneyFox.Foundation.Interfaces.Repositories;

namespace MoneyFox.DataAccess.Repositories
{
    public class RecurringPaymentRepository : IRecurringPaymentRepository
    {
        private readonly IDatabaseManager dbManager;

        /// <summary>
        ///     Creates a RecurringPaymentRepository Object
        /// </summary>
        /// <param name="dbManager">Instanced <see cref="IDatabaseManager"/> data Access</param>
        public RecurringPaymentRepository(IDatabaseManager dbManager)
        {
            this.dbManager = dbManager;
        }

        public IEnumerable<RecurringPaymentViewModel> GetList(Expression<Func<RecurringPaymentViewModel, bool>> filter = null)
        {
            using (var db = dbManager.GetConnection())
            {
                var query = db.Table<RecurringPayment>().AsQueryable().ProjectTo<RecurringPaymentViewModel>();

                if (filter != null)
                {
                    query = query.Where(filter);
                }
                return query.ToList();
            }
        }

        public RecurringPaymentViewModel FindById(int id)
        {
            using (var db = dbManager.GetConnection())
            {
                return Mapper.Map<RecurringPaymentViewModel>(db.Table<RecurringPayment>().FirstOrDefault(x => x.Id == id));
            }
        }

        /// <summary>
        ///     Save a new RecurringPaymentViewModel or update an existing one.
        /// </summary>
        public bool Save(RecurringPaymentViewModel paymentVmToSave)
        {
            using (var db = dbManager.GetConnection())
            {
                var recurringPayment = Mapper.Map<RecurringPayment>(paymentVmToSave);

                if (recurringPayment.Id == 0)
                {
                    var rows = db.Insert(recurringPayment);
                    paymentVmToSave.Id = db.Table<RecurringPayment>().OrderByDescending(x => x.Id).First().Id;

                    return rows == 1;
                }
                return db.Update(recurringPayment) == 1;
            }
        }

        /// <summary>
        ///     Deletes the passed RecurringPaymentViewModel and removes it from cache
        /// </summary>
        public bool Delete(RecurringPaymentViewModel paymentTodelete)
        {
            using (var db = dbManager.GetConnection())
            {
                var itemToDelete = db.Table<RecurringPayment>().Single(x => x.Id == paymentTodelete.Id);
                return db.Delete(itemToDelete) == 1;
            }
        }
    }
}