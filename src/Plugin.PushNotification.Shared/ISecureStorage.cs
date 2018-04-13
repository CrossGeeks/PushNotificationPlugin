////////////////////////////////////////////////////////
// Copyright (c) 2017 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using System;

namespace Plugin.PushNotification.Shared
{
    /// <summary>
    /// Interface provides basic functionality including get, set, delete and if exists. 
    /// </summary>
    public interface ISecureStorage
    {
        /// <summary>
        /// Retrieves the value from storage.
        /// If value with the given key does not exist,
        /// returns default value
        /// </summary>
        /// <returns>The value.</returns>
        /// <param name="key">Key.</param>
        /// <param name="defaultValue">Default value.</param>
        string GetValue(string key, string defaultValue = default(string));

        /// <summary>
        /// Sets the value for the given key. If value exists, overwrites it
        /// Else creates new entry.
        /// Does not accept null value.
        /// </summary>
        /// <returns><c>true</c>, if value was set, <c>false</c> otherwise.</returns>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        bool SetValue(string key, string value);

        /// <summary>
        /// Deletes the key and corresponding value from the storage
        /// </summary>
        /// <returns><c>true</c>, if key was deleted, <c>false</c> otherwise.</returns>
        /// <param name="key">Key.</param>
        bool DeleteKey(string key);

        /// <summary>
        /// Determines whether specified key exists in the storage
        /// </summary>
        /// <returns><c>true</c> if this instance has key the specified key; otherwise, <c>false</c>.</returns>
        /// <param name="key">Key.</param>
        bool HasKey(string key);
    }
}
