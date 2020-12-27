using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Xml;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System.Threading.Tasks;

namespace XmlMetadata
{
    public abstract class BaseXmlSaver : IMetadataSaver
    {
        private static readonly Dictionary<string, string> CommonTags = new[] {

                    "Added",
                    "AudioDbAlbumId",
                    "AudioDbArtistId",
                    "BirthDate",
                    
                    // Deprecated. No longer saving in this field.
                    "certification",

                    "Chapters",
                    "ContentRating",
                    "Countries",
                    "CustomRating",
                    "CriticRating",
                    "DeathDate",
                    "EndDate",
                    "Genres",
                    "Genre",
                    "GamesDbId",
                    
                    // Deprecated. No longer saving in this field.
                    "IMDB_ID",

                    "IMDB",
                    
                    // Deprecated. No longer saving in this field.
                    "IMDbId",

                    "Language",
                    "LocalTitle",
                    "OriginalTitle",
                    "LockData",
                    "LockedFields",
                    "Format3D",
                    
                    // Deprecated. No longer saving in this field.
                    "MPAARating",

                    "MusicBrainzArtistId",
                    "MusicBrainzAlbumArtistId",
                    "MusicBrainzAlbumId",
                    "MusicBrainzReleaseGroupId",

                    // Deprecated. No longer saving in this field.
                    "MusicbrainzId",

                    "Overview",
                    "Persons",
                    "PremiereDate",
                    "ProductionYear",
                    "Rating",
                    "RottenTomatoesId",
                    "RunningTime",
                    
                    // Deprecated. No longer saving in this field.
                    "Runtime",

                    "SortTitle",
                    "Studios",
                    "Tags",
                    
                    // Deprecated. No longer saving in this field.
                    "TagLine",

                    "Taglines",
                    "TMDbCollectionId",
                    "TMDbId",

                    // Deprecated. No longer saving in this field.
                    "Trailer",

                    "Trailers",
                    "TVcomId",
                    "TvDbId",
                    "TVRageId",
                    "Zap2ItId",
                    "CollectionItems",
                    "PlaylistItems"

        }.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

        public BaseXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger)
        {
            FileSystem = fileSystem;
            ConfigurationManager = configurationManager;
            LibraryManager = libraryManager;
            UserManager = userManager;
            UserDataManager = userDataManager;
            Logger = logger;
            
        }

        protected IFileSystem FileSystem { get; private set; }
        protected IServerConfigurationManager ConfigurationManager { get; private set; }
        protected ILibraryManager LibraryManager { get; private set; }
        protected IUserManager UserManager { get; private set; }
        protected IUserDataManager UserDataManager { get; private set; }
        protected ILogger Logger { get; private set; }
        

        protected ItemUpdateType MinimumUpdateType
        {
            get
            {
                return ItemUpdateType.MetadataDownload;
            }
        }

        public string Name
        {
            get
            {
                return "Emby Xml";
            }
        }

        public string GetSavePath(BaseItem item)
        {
            return GetLocalSavePath(item);
        }

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected abstract string GetLocalSavePath(BaseItem item);

        /// <summary>
        /// Gets the name of the root element.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected virtual string GetRootElementName(BaseItem item)
        {
            return "Item";
        }

        /// <summary>
        /// Determines whether [is enabled for] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified item]; otherwise, <c>false</c>.</returns>
        public abstract bool IsEnabledFor(BaseItem item, ItemUpdateType updateType);

        protected virtual List<string> GetTagsUsed()
        {
            return new List<string>();
        }

        public async Task Save(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            var path = GetLocalSavePath(item);

            Logger.Debug("Saving xml metadata for {0} to {1}.", item.Path ?? item.Name, path);

            using (var memoryStream = new MemoryStream())
            {
                Save(item, memoryStream, path);

                memoryStream.Position = 0;

                cancellationToken.ThrowIfCancellationRequested();

                await SaveToFile(memoryStream, path, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SaveToFile(Stream stream, string path, CancellationToken cancellationToken)
        {
            FileSystem.CreateDirectory(FileSystem.GetDirectoryName(path));
            // On Windows, saving the file will fail if the file is hidden or readonly
            FileSystem.SetAttributes(path, false, false);

            var fileCreated = false;

            try
            {
                using (var filestream = FileSystem.GetFileStream(path, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read, FileOpenOptions.Asynchronous | FileOpenOptions.SequentialScan))
                {
                    fileCreated = true;
                    await stream.CopyToAsync(filestream, StreamDefaults.DefaultCopyToBufferSize, cancellationToken).ConfigureAwait(false);
                }
            }
            catch
            {
                if (fileCreated)
                {
                    TryDelete(path);
                }

                throw;
            }

            if (ConfigurationManager.Configuration.SaveMetadataHidden)
            {
                SetHidden(path, true);
            }
        }

        private void TryDelete(string path)
        {
            try
            {
                FileSystem.DeleteFile(path);
            }
            catch
            {

            }
        }

        private void SetHidden(string path, bool hidden)
        {
            try
            {
                FileSystem.SetHidden(path, hidden);
            }
            catch (Exception ex)
            {
                Logger.Error("Error setting hidden attribute on {0} - {1}", path, ex.Message);
            }
        }

        private void Save(BaseItem item, Stream stream, string xmlPath)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                CloseOutput = false
            };

            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                var root = GetRootElementName(item);

                writer.WriteStartDocument(true);

                writer.WriteStartElement(root);

                var baseItem = item;

                if (baseItem != null)
                {
                    AddCommonNodes(baseItem, writer, LibraryManager, UserManager, UserDataManager, FileSystem, ConfigurationManager);
                }

                WriteCustomElements(item, writer);

                var tagsUsed = GetTagsUsed();

                try
                {
                    AddCustomTags(xmlPath, tagsUsed, writer, Logger, FileSystem);
                }
                catch (FileNotFoundException)
                {

                }
                catch (IOException)
                {

                }
                catch (XmlException ex)
                {
                    Logger.ErrorException("Error reading existng xml", ex);
                }

                writer.WriteEndElement();

                writer.WriteEndDocument();
            }
        }

        protected abstract void WriteCustomElements(BaseItem item, XmlWriter writer);

        public const string DateAddedFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Adds the common nodes.
        /// </summary>
        /// <returns>Task.</returns>
        public static void AddCommonNodes(BaseItem item, XmlWriter writer, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo, IFileSystem fileSystem, IServerConfigurationManager config)
        {
            var writtenProviderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(item.OfficialRating))
            {
                writer.WriteElementString("ContentRating", item.OfficialRating);
            }

            writer.WriteElementString("Added", item.DateCreated.LocalDateTime.ToString("G"));

            writer.WriteElementString("LockData", item.IsLocked.ToString().ToLower());

            if (item.LockedFields.Length > 0)
            {
                writer.WriteElementString("LockedFields", string.Join("|", item.LockedFields));
            }

            if (item.CriticRating.HasValue)
            {
                writer.WriteElementString("CriticRating", item.CriticRating.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(item.Overview))
            {
                writer.WriteElementString("Overview", item.Overview);
            }

            if (!string.IsNullOrEmpty(item.OriginalTitle))
            {
                writer.WriteElementString("OriginalTitle", item.OriginalTitle);
            }
            if (!string.IsNullOrEmpty(item.CustomRating))
            {
                writer.WriteElementString("CustomRating", item.CustomRating);
            }

            if (!string.IsNullOrEmpty(item.Name) && !(item is Episode))
            {
                writer.WriteElementString("LocalTitle", item.Name);
            }

            var forcedSortName = item.ForcedSortName;
            if (!string.IsNullOrEmpty(forcedSortName))
            {
                writer.WriteElementString("SortTitle", forcedSortName);
            }

            if (item.PremiereDate.HasValue)
            {
                if (item is Person)
                {
                    writer.WriteElementString("BirthDate", item.PremiereDate.Value.LocalDateTime.ToString("yyyy-MM-dd"));
                }
                else if (!(item is Episode))
                {
                    writer.WriteElementString("PremiereDate", item.PremiereDate.Value.LocalDateTime.ToString("yyyy-MM-dd"));
                }
            }

            if (item.EndDate.HasValue)
            {
                if (item is Person)
                {
                    writer.WriteElementString("DeathDate", item.EndDate.Value.LocalDateTime.ToString("yyyy-MM-dd"));
                }
                else if (!(item is Episode))
                {
                    writer.WriteElementString("EndDate", item.EndDate.Value.LocalDateTime.ToString("yyyy-MM-dd"));
                }
            }

            if (item.RemoteTrailers.Length > 0)
            {
                writer.WriteStartElement("Trailers");

                foreach (var trailer in item.RemoteTrailers)
                {
                    writer.WriteElementString("Trailer", trailer);
                }

                writer.WriteEndElement();
            }

            if (item.ProductionLocations.Length > 0)
            {
                writer.WriteStartElement("Countries");

                foreach (var name in item.ProductionLocations)
                {
                    writer.WriteElementString("Country", name);
                }

                writer.WriteEndElement();
            }

            if (item.CommunityRating.HasValue)
            {
                writer.WriteElementString("Rating", item.CommunityRating.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (item.ProductionYear.HasValue && !(item is Person))
            {
                writer.WriteElementString("ProductionYear", item.ProductionYear.Value.ToString(CultureInfo.InvariantCulture));
            }

            //if (!string.IsNullOrEmpty(item.HomePageUrl))
            //{
            //    writer.WriteElementString("Website", item.HomePageUrl);
            //}

            if (!string.IsNullOrEmpty(item.PreferredMetadataLanguage))
            {
                writer.WriteElementString("Language", item.PreferredMetadataLanguage);
            }
            if (!string.IsNullOrEmpty(item.PreferredMetadataCountryCode))
            {
                writer.WriteElementString("CountryCode", item.PreferredMetadataCountryCode);
            }

            // Use original runtime here, actual file runtime later in MediaInfo
            var runTimeTicks = item.RunTimeTicks;

            if (runTimeTicks.HasValue)
            {
                var timespan = TimeSpan.FromTicks(runTimeTicks.Value);

                writer.WriteElementString("RunningTime", Convert.ToInt32(timespan.TotalMinutes).ToString(CultureInfo.InvariantCulture));
            }

            if (item.ProviderIds != null)
            {
                foreach (var providerKey in item.ProviderIds.Keys)
                {
                    var providerId = item.ProviderIds[providerKey];
                    if (!string.IsNullOrEmpty(providerId))
                    {
                        writer.WriteElementString(providerKey + "Id", providerId);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(item.Tagline))
            {
                writer.WriteStartElement("Taglines");
                writer.WriteElementString("Tagline", item.Tagline);
                writer.WriteEndElement();
            }

            if (item.Genres.Length > 0)
            {
                writer.WriteStartElement("Genres");

                foreach (var genre in item.Genres)
                {
                    writer.WriteElementString("Genre", genre.Name);
                }

                writer.WriteEndElement();
            }

            if (item.Studios.Length > 0)
            {
                writer.WriteStartElement("Studios");

                foreach (var studio in item.Studios)
                {
                    writer.WriteElementString("Studio", studio.Name);
                }

                writer.WriteEndElement();
            }

            if (item.Tags.Length > 0)
            {
                writer.WriteStartElement("Tags");

                foreach (var tag in item.Tags)
                {
                    writer.WriteElementString("Tag", tag.Name);
                }

                writer.WriteEndElement();
            }

            var people = libraryManager.GetItemPeople(item);

            if (people.Count > 0)
            {
                writer.WriteStartElement("Persons");

                foreach (var person in people)
                {
                    writer.WriteStartElement("Person");
                    writer.WriteElementString("Name", person.Name);
                    writer.WriteElementString("Type", person.Type.ToString());
                    writer.WriteElementString("Role", person.Role);

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            var hasShares = item as IHasShares;
            if (hasShares != null)
            {
                AddShares(hasShares, writer);
            }

            AddMediaInfo(item, writer);
        }

        public static void AddShares(IHasShares item, XmlWriter writer)
        {
            writer.WriteStartElement("Shares");

            foreach (var share in item.Shares)
            {
                writer.WriteStartElement("Share");

                writer.WriteElementString("UserId", share.UserId);
                writer.WriteElementString("CanEdit", share.CanEdit.ToString().ToLower());

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Appends the media info.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void AddMediaInfo<T>(T item, XmlWriter writer)
            where T : BaseItem
        {
            var video = item as Video;

            if (video != null)
            {
                if (video.Video3DFormat.HasValue)
                {
                    switch (video.Video3DFormat.Value)
                    {
                        case Video3DFormat.FullSideBySide:
                            writer.WriteElementString("Format3D", "FSBS");
                            break;
                        case Video3DFormat.FullTopAndBottom:
                            writer.WriteElementString("Format3D", "FTAB");
                            break;
                        case Video3DFormat.HalfSideBySide:
                            writer.WriteElementString("Format3D", "HSBS");
                            break;
                        case Video3DFormat.HalfTopAndBottom:
                            writer.WriteElementString("Format3D", "HTAB");
                            break;
                        case Video3DFormat.MVC:
                            writer.WriteElementString("Format3D", "MVC");
                            break;
                    }
                }
            }
        }

        private XmlReaderSettings Create(bool enableValidation)
        {
            var settings = new XmlReaderSettings();

            if (!enableValidation)
            {
                settings.ValidationType = ValidationType.None;
            }

            return settings;
        }

        private void AddCustomTags(string path, List<string> xmlTagsUsed, XmlWriter writer, ILogger logger, IFileSystem fileSystem)
        {
            var settings = Create(false);

            settings.CheckCharacters = false;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreComments = true;

            using (var fileStream = fileSystem.OpenRead(path))
            {
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    // Use XmlReader for best performance
                    using (var reader = XmlReader.Create(streamReader, settings))
                    {
                        try
                        {
                            reader.MoveToContent();
                        }
                        catch (Exception ex)
                        {
                            logger.ErrorException("Error reading existing xml tags from {0}.", ex, path);
                            return;
                        }

                        reader.Read();

                        // Loop through each element
                        while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                var name = reader.Name;

                                if (!CommonTags.ContainsKey(name) && !xmlTagsUsed.Contains(name, StringComparer.OrdinalIgnoreCase))
                                {
                                    writer.WriteNode(reader, false);
                                }
                                else
                                {
                                    reader.Skip();
                                }
                            }
                            else
                            {
                                reader.Read();
                            }
                        }
                    }
                }
            }
        }
    }
}
