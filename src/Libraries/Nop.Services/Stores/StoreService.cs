﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Stores;
using Nop.Data;

namespace Nop.Services.Stores
{
    /// <summary>
    /// Store service
    /// </summary>
    public partial class StoreService : IStoreService
    {
        #region Fields

        private readonly IRepository<Store> _storeRepository;

        #endregion

        #region Ctor

        public StoreService(IRepository<Store> storeRepository)
        {
            _storeRepository = storeRepository;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Parse comma-separated Hosts
        /// </summary>
        /// <param name="store">Store</param>
        /// <returns>Comma-separated hosts</returns>
        protected virtual string[] ParseHostValues(Store store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            var parsedValues = new List<string>();
            if (string.IsNullOrEmpty(store.Hosts))
                return parsedValues.ToArray();

            var hosts = store.Hosts.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var host in hosts)
            {
                var tmp = host.Trim();
                if (!string.IsNullOrEmpty(tmp))
                    parsedValues.Add(tmp);
            }

            return parsedValues.ToArray();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Deletes a store
        /// </summary>
        /// <param name="store">Store</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task DeleteStoreAsync(Store store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            var allStores = await GetAllStoresAsync();
            if (allStores.Count == 1)
                throw new Exception("You cannot delete the only configured store");

            await _storeRepository.DeleteAsync(store);
        }

        /// <summary>
        /// Gets all stores
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the stores
        /// </returns>
        public virtual async Task<IList<Store>> GetAllStoresAsync()
        {
            return await _storeRepository.GetAllAsync(query =>
            {
                return from s in query orderby s.DisplayOrder, s.Id select s;
            }, _ => default, includeDeleted: false);
        }

        /// <summary>
        /// Gets all stores
        /// </summary>
        /// <returns>
        /// The stores
        /// </returns>
        public virtual IList<Store> GetAllStores()
        {
            return _storeRepository.GetAll(query =>
            {
                return from s in query orderby s.DisplayOrder, s.Id select s;
            }, _ => default, includeDeleted: false);
        }

        /// <summary>
        /// Gets a store 
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the store
        /// </returns>
        public virtual async Task<Store> GetStoreByIdAsync(int storeId)
        {
            return await _storeRepository.GetByIdAsync(storeId, cache => default, false);
        }

        /// <summary>
        /// Inserts a store
        /// </summary>
        /// <param name="store">Store</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task InsertStoreAsync(Store store)
        {
            await _storeRepository.InsertAsync(store);
        }

        /// <summary>
        /// Updates the store
        /// </summary>
        /// <param name="store">Store</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task UpdateStoreAsync(Store store)
        {
            await _storeRepository.UpdateAsync(store);
        }

        /// <summary>
        /// Updates the store
        /// </summary>
        /// <param name="store">Store</param>
        public virtual void UpdateStore(Store store)
        {
            _storeRepository.Update(store);
        }

        /// <summary>
        /// Indicates whether a store contains a specified host
        /// </summary>
        /// <param name="store">Store</param>
        /// <param name="host">Host</param>
        /// <returns>true - contains, false - no</returns>
        public virtual bool ContainsHostValue(Store store, string host)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (string.IsNullOrEmpty(host))
                return false;

            var contains = ParseHostValues(store).Any(x => x.Equals(host, StringComparison.InvariantCultureIgnoreCase));

            return contains;
        }

        /// <summary>
        /// Returns a list of names of not existing stores
        /// </summary>
        /// <param name="storeIdsNames">The names and/or IDs of the store to check</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of names and/or IDs not existing stores
        /// </returns>
        public async Task<string[]> GetNotExistingStoresAsync(string[] storeIdsNames)
        {
            if (storeIdsNames == null)
                throw new ArgumentNullException(nameof(storeIdsNames));

            var query = _storeRepository.Table;
            var queryFilter = storeIdsNames.Distinct().ToArray();
            //filtering by name
            var filter = await query.Select(store => store.Name)
                .Where(store => queryFilter.Contains(store))
                .ToListAsync();
            queryFilter = queryFilter.Except(filter).ToArray();

            //if some names not found
            if (!queryFilter.Any())
                return queryFilter.ToArray();

            //filtering by IDs
            filter = await query.Select(store => store.Id.ToString())
                .Where(store => queryFilter.Contains(store))
                .ToListAsync();
            queryFilter = queryFilter.Except(filter).ToArray();

            return queryFilter.ToArray();
        }

        #endregion

        #region Extensions by QuanNH

        public virtual async Task<IList<Store>> GetStoreNameByIdAsync(int[] storeId)
        {
            var query = from s in _storeRepository.Table
                        where storeId.Contains(s.Id)
                        orderby s.DisplayOrder, s.Name
                        select s;
            var stores = await query.Distinct().ToListAsync();
            return stores;
        }

        public virtual async Task<IList<Store>> GetAllStoresByEntityNameAsync(int entityId, string entityName)
        {
            var storeMappingRepository = Nop.Core.Infrastructure.EngineContext.Current.Resolve<IRepository<StoreMapping>>();

            var query = from sm in storeMappingRepository.Table
                        join s in _storeRepository.Table on sm.StoreId equals s.Id
                        where sm.EntityId == entityId &&
                        sm.EntityName == entityName
                        orderby s.DisplayOrder
                        select s;
            var stores = await query.Distinct().ToListAsync();
            return stores;
        }


        #endregion
    }
}