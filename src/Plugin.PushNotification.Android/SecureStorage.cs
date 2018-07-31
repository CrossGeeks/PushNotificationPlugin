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
        private static IsolatedStorageFile File => IsolatedStorageFile.GetUserStoreForApplication();
        private static readonly object SaveLock = new object();

        /// <summary>
        /// Name of the storage file.
        /// </summary>
        public static string StorageFile = "Util.CrossPushNotification";

        /// <summary>
        /// Password for storage. Assign your own password and obfuscate the app.
        /// </summary>
        public static string StoragePassword = "Replace With Your Password";

        private readonly char[] _password;

        // Store for Key Value pairs
        private readonly KeyStore _keyStore;

        // password protection for the store
        private readonly KeyStore.PasswordProtection _protection;

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

            this._password = StoragePassword.ToCharArray();

            _keyStore = KeyStore.GetInstance(KeyStore.DefaultType);
            _protection = new KeyStore.PasswordProtection(this._password);

            if (File.FileExists(StorageFile))
            {
                using (var stream = new IsolatedStorageFileStream(StorageFile, FileMode.Open, FileAccess.Read, File))
                {
                    this._keyStore.Load(stream, _password);
                }
            }
            else
            {
                this._keyStore.Load(null, _password);
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
            var entry = GetSecretKeyEntry(key);

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
            base.SetValue(key, value);
            this._keyStore.SetEntry(key, new KeyStore.SecretKeyEntry(new SecureData(value)), _protection);
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
                _keyStore.DeleteEntry(key);
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
            lock (SaveLock)
            {
                using (var stream = new IsolatedStorageFileStream(StorageFile, FileMode.OpenOrCreate, FileAccess.Write, File))
                {
                    this._keyStore.Store(stream, this._protection.GetPassword());
                }
            }
        }

        // retrieves the secret key entry from the store
        private KeyStore.SecretKeyEntry GetSecretKeyEntry(string key)
        {
            try
            {
                return _keyStore.GetEntry(key, _protection) as KeyStore.SecretKeyEntry;
            }
            catch (UnrecoverableKeyException) // swallow this exception. Can be caused by invalid key
            { 
                return null;
            }
        }

        /// <summary>
        /// Class for storing string as entry
        /// </summary>
        private class SecureData : Java.Lang.Object, ISecretKey
        {
            private const string AlgoName = "RAW";

            private readonly byte[] _bytes;

            /// <summary>
            /// Constructor makes sure that entry is valid.
            /// Converts it to bytes
            /// </summary>
            /// <param name="entry">Entry.</param>
            public SecureData(string entry)
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