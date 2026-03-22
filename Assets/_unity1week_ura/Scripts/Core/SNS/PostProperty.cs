using System;
using UnityEngine;
using UnityEngine.UI;

namespace Unity1Week_Ura.Core
{
    public class PostProperty
    {
        // 正解のプレイヤーのアカウント
        public Account CorrectPlayerAccount { get; }

        public string Id { get; }
        public Account Author { get; }
        public string Text { get; }
        public Sprite AttachedImage { get; }
        public string ParentPostId { get; }
        public Account ParentPostAuthor { get; }

        public PostProperty(Account correctPlayerAccount, string id, Account author, string text, Sprite attachedImage, string parentPostId, Account parentPostAuthor)
        {
            CorrectPlayerAccount = correctPlayerAccount;
            Id = id;
            Author = author;
            Text = text;
            AttachedImage = attachedImage;
            ParentPostId = parentPostId;
            ParentPostAuthor = parentPostAuthor;
        }
    }
}
