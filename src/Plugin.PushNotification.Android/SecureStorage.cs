////////////////////////////////////////////////////////
// Copyright (c) 2017 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using Java.Security;
using Javax.Crypto;
using Plugin.PushNotification.Shared;

namespace Plugin.PushNotification
{
    /// <summary>
    /// Android implementation of secure storage. Done using KeyStore
    /// Make sure to initialize store password for Android.
    /// </summary>
    internal class SecureStorageImplementation : SecureStorageImplementationBase
    {
        /// <summary>
        /// Name of the storage file.
        /// </summary>
        public static string StorageFile = "Util.CrossPushNotification";

        /// <summary>
        /// Password for storage. Assign your own password and obfuscate the app.
        /// </summary>
        public static string StoragePassword = "Replace With Your Password";

        private readonly char[] StoragePasswordArray;

        // Store for Key Value pairs
        private readonly KeyStore _store;

        // password protection for the store
        private readonly KeyStore.PasswordProtection _passwordProtection;

        /// <summary>
        /// Default constructor created or loads the store
        /// </summary>
        public SecureStorageImplementation()
        {
            // verify that password is set
            if (string.IsNullOrWhiteSpace(StoragePassword))
            {
                throw new Exception($"Must set StoragePassword");
            }

            StoragePasswordArray = StoragePassword.ToCharArray();

            // Instantiate store and protection
            _store = KeyStore.GetInstance(KeyStore.DefaultType);
            _passwordProtection = new KeyStore.PasswordProtection(StoragePasswordArray);

            // if store exists, load it from the file
            try
            {
                using (var stream = new IsolatedStorageFileStream(StorageFile, FileMode.Open, FileAccess.Read))
                {
                    _store.Load(stream, StoragePasswordArray);
                }
            }
            catch (Exception)
            {
                // this will happen for the first run. As no file is expected to be present
                _store.Load(null, StoragePasswordArray);
            }
        }

        #region ISecureStorage implementation

        /// <summary>
        /// Retrieves the value from storage.
        /// If value with the given key does not exist,
        /// returns default value
        /// </summary>
        /// <returns>The value.</returns>
        /// <param name="key">Key.</param>
        /// <param name="defaultValue">Default value.</param>
        public override string GetValue(string key, string defaultValue)
        {
            // validate using base class
            base.GetValue(key, defaultValue);

            // get the entry from the store
            // if it does not exist, return the default value
            KeyStore.SecretKeyEntry entry = GetSecretKeyEntry(key);

            if (entry != null)
            {
                var encodedBytes = entry.SecretKey.GetEncoded();
                return Encoding.UTF8.GetString(encodedBytes);
            }

            return defaultValue;
        }

        /// <summary>
        /// Sets the value for the given key. If value exists, overwrites it
        /// Else creates new entry.
        /// Does not accept null value.
        /// </summary>
        /// <returns>true</returns>
        /// <c>false</c>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public override bool SetValue(string key, string value)
        {
            // validate the parameters
            base.SetValue(key, value);

            // create entry
            var secKeyEntry = new KeyStore.SecretKeyEntry(new StringKeyEntry(value));

            // save it in the KeyStore
            _store.SetEntry(key, secKeyEntry, _passwordProtection);

            // save the store
            Save();

            return true;
        }

        /// <summary>
        /// Deletes the key and corresponding value from the storage
        /// </summary>
        public override bool DeleteKey(string key)
        {
            // valdiate using base class
            base.DeleteKey(key);

            // retrieve the entry
            KeyStore.SecretKeyEntry entry = GetSecretKeyEntry(key);

            // if entry exists, delete from store, save the store and return true
            if (entry != null)
            {
                _store.DeleteEntry(key);

                Save();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether specified key exists in the storage
        /// </summary>
        public override bool HasKey(string key)
        {
            // validate if key is valid
            base.HasKey(key);
            // retrieve to see, if it exists
            return GetSecretKeyEntry(key) != null;
        }

        #endregion

        // persists the store using password
        private void Save()
        {
            using (var stream = new IsolatedStorageFileStream(StorageFile, FileMode.OpenOrCreate, FileAccess.Write))
            {
                _store.Store(stream, StoragePasswordArray);
            }
        }

        // retrieves the secret key entry from the store
        private KeyStore.SecretKeyEntry GetSecretKeyEntry(string key)
        {
            try
            {
                return _store.GetEntry(key, _passwordProtection) as KeyStore.SecretKeyEntry;
            }
            catch (UnrecoverableKeyException) // swallow this exception. Can be caused by invalid key
            {
                return null;
            }
        }

        /// <summary>
        /// Class for storing string as entry
        /// </summary>
        private class StringKeyEntry : Java.Lang.Object, ISecretKey
        {
            private const string AlgoName = "RAW";

            private readonly byte[] _bytes;

            /// <summary>
            /// Constructor makes sure that entry is valid.
            /// Converts it to bytes
            /// </summary>
            /// <param name="entry">Entry.</param>
            public StringKeyEntry(string entry)
            {
                if (entry == null)
                {
                    throw new ArgumentNullException();
                }

                _bytes = ASCIIEncoding.UTF8.GetBytes(entry);
            }

            #region IKey implementation
            public byte[] GetEncoded()
            {
                return _bytes;
            }

            public string Algorithm => AlgoName;

            public string Format => AlgoName;

            #endregion
        }
    }
}