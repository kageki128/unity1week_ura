using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Unity1Week_Ura.Core;
using Unity1Week_Ura.Infrastructure;

namespace Unity1Week_Ura.Tests
{
    public class AccountRepositoryTests
    {
        [Test]
        public void Constructor_NullAddressableConfig_ThrowsArgumentNullException()
        {
            var loader = new AddressableSpriteLabelLoader();

            Assert.Throws<ArgumentNullException>(() => new AccountRepository(null, loader));
        }

        [Test]
        public void Constructor_NullSpriteLoader_ThrowsArgumentNullException()
        {
            var config = ScriptableObject.CreateInstance<AddressableConfigSO>();

            Assert.Throws<ArgumentNullException>(() => new AccountRepository(config, null));
        }

        [Test]
        public void ParseAccountRows_ValidCsv_ReturnsExpectedRows()
        {
            var repository = CreateRepository();
            const string csv = "ID,Name,IconFileName,AccountType,RelatedPlayerAccountID\nacc01,Main,main.png,Normal,player01\n,NoId,ignored.png,Normal,player01\nacc02,Sub,sub.jpg,Advertise,player02\nshort";

            var rows = (IList)InvokePrivate(repository, "ParseAccountRows", csv);

            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(GetProperty<string>(rows[0], "Id"), Is.EqualTo("acc01"));
            Assert.That(GetProperty<string>(rows[0], "Name"), Is.EqualTo("Main"));
            Assert.That(GetProperty<string>(rows[0], "IconFileName"), Is.EqualTo("main.png"));
            Assert.That(GetProperty<AccountType>(rows[0], "Type"), Is.EqualTo(AccountType.Normal));
            Assert.That(GetProperty<string>(rows[0], "RelatedPlayerAccountId"), Is.EqualTo("player01"));
            Assert.That(GetProperty<string>(rows[1], "Id"), Is.EqualTo("acc02"));
            Assert.That(GetProperty<AccountType>(rows[1], "Type"), Is.EqualTo(AccountType.Advertise));
            Assert.That(GetProperty<string>(rows[1], "RelatedPlayerAccountId"), Is.EqualTo("player02"));
        }

        [Test]
        public void ParseAccountRows_EmptyRelatedPlayerAccountId_IsAccepted()
        {
            var repository = CreateRepository();
            const string csv = "ID,Name,IconFileName,AccountType,RelatedPlayerAccountID\nad01,Ad,ad.png,Advertise,\nnormal01,Normal,normal.png,Normal,player01";

            var rows = (IList)InvokePrivate(repository, "ParseAccountRows", csv);

            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(GetProperty<string>(rows[0], "Id"), Is.EqualTo("ad01"));
            Assert.That(GetProperty<string>(rows[0], "RelatedPlayerAccountId"), Is.Empty);
            Assert.That(GetProperty<string>(rows[1], "Id"), Is.EqualTo("normal01"));
            Assert.That(GetProperty<string>(rows[1], "RelatedPlayerAccountId"), Is.EqualTo("player01"));
        }

        [Test]
        public void ParseAccountRows_MissingRequiredHeader_ThrowsInvalidOperationException()
        {
            var repository = CreateRepository();
            const string csv = "ID,Name\nacc01,Main";

            Assert.Throws<InvalidOperationException>(() => InvokePrivate(repository, "ParseAccountRows", csv));
        }

        AccountRepository CreateRepository()
        {
            var config = ScriptableObject.CreateInstance<AddressableConfigSO>();
            var loader = new AddressableSpriteLabelLoader();
            return new AccountRepository(config, loader);
        }

        static object InvokePrivate(object instance, string methodName, params object[] args)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(instance.GetType().Name, methodName);
            }

            try
            {
                return method.Invoke(instance, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        static T GetProperty<T>(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                throw new MissingMemberException(instance.GetType().Name, propertyName);
            }

            return (T)property.GetValue(instance);
        }
    }
}
