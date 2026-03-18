using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.Infrastructure
{
    public class AccountRepository : IAccountRepository
    {
        const string CsvColumnId = "ID";
        const string CsvColumnName = "Name";
        const string CsvColumnIconFileName = "IconFileName";

        readonly AddressableConfigSO addressableConfig;
        readonly AddressableSpriteLabelLoader spriteLabelLoader;
        readonly Dictionary<string, Account> accountsById = new();
        readonly SemaphoreSlim loadGate = new(1, 1);
        bool isLoaded;

        sealed class AccountCsvRow
        {
            public string Id { get; }
            public string Name { get; }
            public string IconFileName { get; }

            public AccountCsvRow(string id, string name, string iconFileName)
            {
                Id = id;
                Name = name;
                IconFileName = iconFileName;
            }
        }

        public AccountRepository(AddressableConfigSO addressableConfig, AddressableSpriteLabelLoader spriteLabelLoader)
        {
            if (addressableConfig == null)
            {
                throw new ArgumentNullException(nameof(addressableConfig));
            }

            if (spriteLabelLoader == null)
            {
                throw new ArgumentNullException(nameof(spriteLabelLoader));
            }

            this.addressableConfig = addressableConfig;
            this.spriteLabelLoader = spriteLabelLoader;
        }

        public async UniTask<Account> GetAccount(string accountId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(accountId))
            {
                throw new ArgumentException("accountId is null or empty.", nameof(accountId));
            }

            await EnsureAccountsLoadedAsync(ct);

            if (accountsById.TryGetValue(accountId, out var account))
            {
                return account;
            }

            throw new KeyNotFoundException($"Account not found. accountId: {accountId}");
        }

        async UniTask EnsureAccountsLoadedAsync(CancellationToken ct)
        {
            if (isLoaded)
            {
                return;
            }

            await loadGate.WaitAsync(ct).AsUniTask();
            try
            {
                if (isLoaded)
                {
                    return;
                }

                if (addressableConfig.AccountDatas == null || !addressableConfig.AccountDatas.RuntimeKeyIsValid())
                {
                    throw new InvalidOperationException("AddressableConfigSO.AccountDatas is not assigned.");
                }

                var assetReference = addressableConfig.AccountDatas;
                try
                {
                    TextAsset csvAsset = await AddressableAssetLoader.LoadAsync<TextAsset>(assetReference, ct);
                    var rows = ParseAccountRows(csvAsset.text);
                    var iconsByFileName = await spriteLabelLoader.LoadAllByLabelAsync(addressableConfig.IconLabel, ct);

                    foreach (var row in rows)
                    {
                        ct.ThrowIfCancellationRequested();
                        var icon = AddressableSpriteLabelLoader.ResolveSprite(row.IconFileName, iconsByFileName);
                        accountsById[row.Id] = new Account(row.Id, row.Name, icon);
                    }

                    isLoaded = true;
                }
                finally
                {
                    assetReference.ReleaseAsset();
                }
            }
            finally
            {
                loadGate.Release();
            }
        }

        List<AccountCsvRow> ParseAccountRows(string csvText)
        {
            if (string.IsNullOrWhiteSpace(csvText))
            {
                throw new InvalidOperationException("Accounts csv is empty.");
            }

            var lines = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1)
            {
                throw new InvalidOperationException("Accounts csv does not contain data rows.");
            }

            int idColumn = -1;
            int nameColumn = -1;
            int iconFileNameColumn = -1;

            var headerColumns = lines[0].Split(',');
            for (int i = 0; i < headerColumns.Length; i++)
            {
                var column = headerColumns[i].Trim();
                if (column.Equals(CsvColumnId, StringComparison.OrdinalIgnoreCase))
                {
                    idColumn = i;
                }
                else if (column.Equals(CsvColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    nameColumn = i;
                }
                else if (column.Equals(CsvColumnIconFileName, StringComparison.OrdinalIgnoreCase))
                {
                    iconFileNameColumn = i;
                }
            }

            if (idColumn < 0 || nameColumn < 0 || iconFileNameColumn < 0)
            {
                throw new InvalidOperationException($"Accounts csv must have {CsvColumnId}, {CsvColumnName} and {CsvColumnIconFileName} columns.");
            }

            var rows = new List<AccountCsvRow>(lines.Length - 1);

            for (int i = 1; i < lines.Length; i++)
            {
                var columns = lines[i].Split(',');
                if (columns.Length <= Math.Max(Math.Max(idColumn, nameColumn), iconFileNameColumn))
                {
                    continue;
                }

                var id = columns[idColumn].Trim();
                var name = columns[nameColumn].Trim();
                var iconFileName = columns[iconFileNameColumn].Trim();
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                rows.Add(new AccountCsvRow(id, name, iconFileName));
            }

            return rows;
        }
    }
}
