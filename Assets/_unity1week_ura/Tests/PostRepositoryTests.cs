using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using UnityEngine;
using Unity1Week_Ura.Core;
using Unity1Week_Ura.Infrastructure;

namespace Unity1Week_Ura.Tests
{
    public class PostRepositoryTests
    {
        [Test]
        public void Constructor_NullAddressableConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PostRepository(null, null, null));
        }

        [Test]
        public void Constructor_NullAccountRepository_ThrowsArgumentNullException()
        {
            var config = ScriptableObject.CreateInstance<AddressableConfigSO>();
            Assert.Throws<ArgumentNullException>(() => new PostRepository(config, null, null));
        }

        [Test]
        public void ParsePostRows_ValidCsv_ReturnsExpectedRows()
        {
            var repository = CreateUninitializedRepository();
            const string csv = "CorrectPlayerAccountID,ID,AuthorAccountID,Text,AttachedImageFileName,ParentPostID,Type,DefaultLikeCount,DefaultRepostCount\n"
                + "acc01,p01,acc02,hello,image.png,,Normal,100,20\n"
                + "acc01,p02,acc03,skip,image2.png,,Normal,abc,0\n";

            var rows = (IList)InvokePrivate(repository, "ParsePostRows", csv);

            Assert.That(rows.Count, Is.EqualTo(1));
            var first = rows[0];
            Assert.That(GetProperty<string>(first, "CorrectPlayerAccountId"), Is.EqualTo("acc01"));
            Assert.That(GetProperty<string>(first, "Id"), Is.EqualTo("p01"));
            Assert.That(GetProperty<PostType>(first, "Type"), Is.EqualTo(PostType.Normal));
            Assert.That(GetProperty<int>(first, "DefaultLikeCount"), Is.EqualTo(100));
            Assert.That(GetProperty<int>(first, "DefaultRepostCount"), Is.EqualTo(20));
        }

        [Test]
        public void ParsePostRows_InvalidHeader_ThrowsInvalidOperationException()
        {
            var repository = CreateUninitializedRepository();
            const string csv = "ID,AuthorAccountID\np01,acc01";

            Assert.Throws<InvalidOperationException>(() => InvokePrivate(repository, "ParsePostRows", csv));
        }

        [Test]
        public void GetPost_EmptyPostId_ThrowsArgumentException()
        {
            var repository = CreateUninitializedRepository();

            Assert.Throws<ArgumentException>(() => repository.GetPost(string.Empty, default).GetAwaiter().GetResult());
        }

        [Test]
        public void GetPost_WhenPostNotFound_ThrowsKeyNotFoundException()
        {
            var repository = CreateUninitializedRepository();
            SetField(repository, "isLoaded", true);
            SetField(repository, "postsById", new Dictionary<string, Post>());

            Assert.Throws<KeyNotFoundException>(() => repository.GetPost("missing", default).GetAwaiter().GetResult());
        }

        [Test]
        public void GetPost_WhenPostExists_ReturnsExpectedPost()
        {
            var repository = CreateUninitializedRepository();
            var expectedPost = (Post)FormatterServices.GetUninitializedObject(typeof(Post));
            var postsById = new Dictionary<string, Post>
            {
                { "p01", expectedPost }
            };

            SetField(repository, "isLoaded", true);
            SetField(repository, "postsById", postsById);

            var actual = repository.GetPost("p01", default).GetAwaiter().GetResult();

            Assert.That(actual, Is.SameAs(expectedPost));
        }

        static PostRepository CreateUninitializedRepository()
        {
            return (PostRepository)FormatterServices.GetUninitializedObject(typeof(PostRepository));
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

        static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(instance.GetType().Name, fieldName);
            }

            field.SetValue(instance, value);
        }

    }
}
