﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The collection of <see cref = "Tag" />s in a <see cref = "Repository" />
    /// </summary>
    public class TagCollection : IEnumerable<Tag>
    {
        private readonly Repository repo;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "TagCollection" /> class.
        /// </summary>
        /// <param name = "repo">The repo.</param>
        internal TagCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Gets the <see cref = "Tag" /> with the specified name.
        /// </summary>
        public Tag this[string name]
        {
            get
            {
                return repo.Refs.Resolve<Tag>(NormalizeToCanonicalName(name));
            }
        }

        #region IEnumerable<Tag> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public IEnumerator<Tag> GetEnumerator()
        {
            return Libgit2UnsafeHelper
                .ListAllTagNames(repo.Handle)
                .Select(n => this[n])
                .GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///   Creates an annotated tag with the specified name.
        /// </summary>
        /// <param name = "name">The name.</param>
        /// <param name = "target">The target which can be sha or a canonical reference name.</param>
        /// <param name = "tagger">The tagger.</param>
        /// <param name = "message">The message.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing tag, false otherwise.</param>
        /// <returns></returns>
        public Tag Create(string name, string target, Signature tagger, string message, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(target, "target");
            Ensure.ArgumentNotNull(tagger, "tagger");
            Ensure.ArgumentNotNullOrEmptyString(message, "message");

            GitObject objectToTag = RetrieveObjectToTag(target);

            var targetOid = objectToTag.Id.Oid;
            GitOid oid;
            int res;

            if (allowOverwrite)
            {
                res = NativeMethods.git_tag_create_f(out oid, repo.Handle, name, ref targetOid, GitObject.TypeToTypeMap[objectToTag.GetType()], tagger.Handle, message);
            }
            else
            {
                res = NativeMethods.git_tag_create(out oid, repo.Handle, name, ref targetOid, GitObject.TypeToTypeMap[objectToTag.GetType()], tagger.Handle, message);
            }

            Ensure.Success(res);

            return this[name];
        }

        /// <summary>
        ///   Creates a lightweight tag with the specified name.
        /// </summary>
        /// <param name = "name">The name.</param>
        /// <param name = "target">The target which can be sha or a canonical reference name.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing tag, false otherwise.</param>
        /// <returns></returns>
        public Tag Create(string name, string target, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(target, "target");

            GitObject objectToTag = RetrieveObjectToTag(target);

            repo.Refs.Create(NormalizeToCanonicalName(name), objectToTag.Id.Sha, allowOverwrite);   //TODO: To be replaced by native libgit2 git_tag_create_lightweight() when available.

            return this[name];
        }

        /// <summary>
        ///   Deletes the tag with the specified name.
        /// </summary>
        /// <param name = "name">The name of the tag to delete.</param>
        public void Delete(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            Tag tag = this[name];

            if (tag == null)
            {
                throw new ApplicationException(String.Format(CultureInfo.InvariantCulture, "No tag identified by '{0}' can be found in the repository.", name));
            }

            repo.Refs.Delete(tag.CanonicalName);  //TODO: To be replaced by native libgit2 git_tag_delete() when available.
        }

        private GitObject RetrieveObjectToTag(string target)
        {
            var objectToTag = repo.Lookup(target);

            if (objectToTag == null)
            {
                throw new ApplicationException(String.Format(CultureInfo.InvariantCulture, "No object identified by '{0}' can be found in the repository.", target));
            }

            return objectToTag;
        }

        private static string NormalizeToCanonicalName(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            if (name.StartsWith("refs/tags/", StringComparison.Ordinal))
            {
                return name;
            }

            return string.Format(CultureInfo.InvariantCulture, "refs/tags/{0}", name);
        }
    }
}