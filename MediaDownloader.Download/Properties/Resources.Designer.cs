﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MediaDownloader.Download.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("MediaDownloader.Download.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to %(title)s.%(ext)s.
        /// </summary>
        internal static string DownloaderItemTitleTemplate {
            get {
                return ResourceManager.GetString("DownloaderItemTitleTemplate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --ffmpeg-location.
        /// </summary>
        internal static string DownloaderOptionConverterLocation {
            get {
                return ResourceManager.GetString("DownloaderOptionConverterLocation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --encoding utf-8.
        /// </summary>
        internal static string DownloaderOptionEncodingUtf8 {
            get {
                return ResourceManager.GetString("DownloaderOptionEncodingUtf8", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to mp3/ogg/m4a/aac.
        /// </summary>
        internal static string DownloaderOptionFormatAudioOnly {
            get {
                return ResourceManager.GetString("DownloaderOptionFormatAudioOnly", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to bestvideo+bestaudio/best.
        /// </summary>
        internal static string DownloaderOptionFormatBest {
            get {
                return ResourceManager.GetString("DownloaderOptionFormatBest", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to (bestvideo+bestaudio/best)[protocol^=http].
        /// </summary>
        internal static string DownloaderOptionFormatBestDirectLink {
            get {
                return ResourceManager.GetString("DownloaderOptionFormatBestDirectLink", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best.
        /// </summary>
        internal static string DownloaderOptionFormatBestMp4 {
            get {
                return ResourceManager.GetString("DownloaderOptionFormatBestMp4", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --get-filename.
        /// </summary>
        internal static string DownloaderOptionGetFilename {
            get {
                return ResourceManager.GetString("DownloaderOptionGetFilename", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --no-mtime.
        /// </summary>
        internal static string DownloaderOptionNoOriginalDateTime {
            get {
                return ResourceManager.GetString("DownloaderOptionNoOriginalDateTime", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --no-playlist.
        /// </summary>
        internal static string DownloaderOptionNoPlaylist {
            get {
                return ResourceManager.GetString("DownloaderOptionNoPlaylist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --retries.
        /// </summary>
        internal static string DownloaderOptionRetries {
            get {
                return ResourceManager.GetString("DownloaderOptionRetries", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --socket-timeout.
        /// </summary>
        internal static string DownloaderOptionSocketTimeout {
            get {
                return ResourceManager.GetString("DownloaderOptionSocketTimeout", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to -U.
        /// </summary>
        internal static string DownloaderOptionUpdate {
            get {
                return ResourceManager.GetString("DownloaderOptionUpdate", resourceCulture);
            }
        }
    }
}
