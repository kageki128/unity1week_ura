using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        const string CsvColumnAccountType = "AccountType";
        const string CsvColumnRelatedPlayerAccountId = "RelatedPlayerAccountID";
        const string CsvColumnPlayerAccountLabel = "PlayerAccountLabel";

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
            public AccountType Type { get; }
            public string RelatedPlayerAccountId { get; }
            public string PlayerAccountLabel { get; }

            public AccountCsvRow(string id, string name, string iconFileName, AccountType type, string relatedPlayerAccountId, string playerAccountLabel)
            {
                Id = id;
                Name = name;
                IconFileName = iconFileName;
                Type = type;
                RelatedPlayerAccountId = relatedPlayerAccountId;
                PlayerAccountLabel = playerAccountLabel;
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
                    var loadedAccountsById = new Dictionary<string, Account>(StringComparer.Ordinal);
                    TextAsset csvAsset = await AddressableAssetLoader.LoadAsync<TextAsset>(assetReference, ct);
                    var csvText = csvAsset.text;
                    ct.ThrowIfCancellationRequested();
                    var rows = await Task.Run(() => ParseAccountRows(csvText), ct);
                    var iconsByFileName = await spriteLabelLoader.LoadAllByLabelAsync(addressableConfig.IconLabel, ct);

                    foreach (var row in rows)
                    {
                        ct.ThrowIfCancellationRequested();
                        var icon = AddressableSpriteLabelLoader.ResolveSprite(row.IconFileName, iconsByFileName);
                        loadedAccountsById[row.Id] = new Account(row.Id, row.Name, icon, row.Type, row.RelatedPlayerAccountId, row.PlayerAccountLabel);
                    }

                    accountsById.Clear();
                    foreach (var accountPair in loadedAccountsById)
                    {
                        accountsById[accountPair.Key] = accountPair.Value;
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
            int accountTypeColumn = -1;
            int relatedPlayerAccountIdColumn = -1;
            int playerAccountLabelColumn = -1;

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
                else if (column.Equals(CsvColumnAccountType, StringComparison.OrdinalIgnoreCase))
                {
                    accountTypeColumn = i;
                }
                else if (column.Equals(CsvColumnRelatedPlayerAccountId, StringComparison.OrdinalIgnoreCase))
                {
                    relatedPlayerAccountIdColumn = i;
                }
                else if (column.Equals(CsvColumnPlayerAccountLabel, StringComparison.OrdinalIgnoreCase))
                {
                    playerAccountLabelColumn = i;
                }
            }

            if (idColumn < 0
                || nameColumn < 0
                || iconFileNameColumn < 0
                || accountTypeColumn < 0
                || relatedPlayerAccountIdColumn < 0
                || playerAccountLabelColumn < 0)
            {
                throw new InvalidOperationException($"Accounts csv must have {CsvColumnId}, {CsvColumnName}, {CsvColumnIconFileName}, {CsvColumnAccountType}, {CsvColumnRelatedPlayerAccountId} and {CsvColumnPlayerAccountLabel} columns.");
            }

            var rows = new List<AccountCsvRow>(lines.Length - 1);

            for (int i = 1; i < lines.Length; i++)
            {
                var columns = lines[i].Split(',');
                int maxRequiredColumn = Math.Max(
                    Math.Max(idColumn, nameColumn),
                    Math.Max(
                        Math.Max(iconFileNameColumn, accountTypeColumn),
                        Math.Max(relatedPlayerAccountIdColumn, playerAccountLabelColumn)));
                if (columns.Length <= maxRequiredColumn)
                {
                    continue;
                }

                var id = columns[idColumn].Trim();
                var name = columns[nameColumn].Trim();
                var iconFileName = columns[iconFileNameColumn].Trim();
                var accountTypeText = columns[accountTypeColumn].Trim();
                var relatedPlayerAccountId = columns[relatedPlayerAccountIdColumn].Trim();
                var playerAccountLabel = columns[playerAccountLabelColumn].Trim();
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (!Enum.TryParse(accountTypeText, true, out AccountType type))
                {
                    continue;
                }

                rows.Add(new AccountCsvRow(id, name, iconFileName, type, relatedPlayerAccountId, playerAccountLabel));
            }

            return rows;
        }
    }
}
