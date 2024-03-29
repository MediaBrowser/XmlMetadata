﻿using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.IO;
using MediaBrowser.Model.Drawing;

namespace XmlMetadata
{
    public class Plugin : BasePlugin, IHasThumbImage
    {
        private Guid _id = new Guid("2850d40d-9c66-4525-aa46-968e8ef04e97");
        public override Guid Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Xml Metadata"; }
        }

        public static string MetadataName
        {
            get { return "Emby Xml"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "Supports Emby Legacy Xml metadata";
            }
        }

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }

        public ImageFormat ThumbImageFormat
        {
            get
            {
                return ImageFormat.Png;
            }
        }
    }
}
