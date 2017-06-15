﻿using System.ComponentModel;
using System.Drawing.Design;
using engenious.Content.Pipeline;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using ContentTool.Controls;

namespace ContentTool.Models
{
    public class ContentFile : ContentItem
    {
        /// <summary>
        /// Path of the file
        /// </summary>
        public override string FilePath => Path.Combine(Parent.FilePath, Name);

        /// <summary>
        /// Importer of the file
        /// </summary>
        [Browsable(false)]
        public IContentImporter Importer { get; private set; }

        /// <summary>
        /// Processor of the file
        /// </summary>
        [Browsable(false)]
        public IContentProcessor Processor { get; private set; }

        /// <summary>
        /// Name of the importer
        /// </summary>
        [RefreshProperties(RefreshProperties.All)]
        [Editor(typeof(ImporterEditor), typeof(UITypeEditor))]
        public string ImporterName { get => importerName;
            set
            {
                if (importerName == value) return;
                var old = importerName;
                importerName = value;
                Importer = PipelineHelper.CreateImporter(Path.GetExtension(FilePath), ref importerName);
                OnPropertyChanged(old,value);
            }
        }
        private string importerName;

        /// <summary>
        /// Name of the processor
        /// </summary
        [RefreshProperties(RefreshProperties.All)]
        [Editor(typeof(ProcessorEditor), typeof(UITypeEditor))]
        public string ProcessorName { get => processorName;
            set
            {
                if (value == processorName) return;
                string old = processorName;
                processorName = value;

                if (string.IsNullOrWhiteSpace(processorName))
                    processorName = PipelineHelper.GetProcessor(Name, importerName);

                if (old != processorName && !string.IsNullOrWhiteSpace(processorName) && Importer != null)
                    Processor = PipelineHelper.CreateProcessor(Importer.GetType(), processorName);

                OnPropertyChanged(old,value);
            }
        }
        private string processorName;

        /// <summary>
        /// Settings for the processor
        /// </summary>
        [Browsable(false)]
        public ProcessorSettings Settings
        {
            get
            {
                return Processor?.Settings;
            }
            set
            {
                if (Processor == null)
                    return;
                Processor.Settings = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of the file</param>
        /// <param name="parent">Parent item</param>
        public ContentFile(string name, ContentItem parent) : base(name, parent)
        {
        }

        /// <summary>
        /// Deserialize the given XmlElement
        /// </summary>
        /// <param name="element">XMLElement</param>
        public override ContentItem Deserialize(XElement element)
        {
            name = element.Element("Name")?.Value;

            if (!File.Exists(FilePath))
                Error = ContentErrorType.NotFound;

            importerName = element.Element("Importer")?.Value;
            Importer = PipelineHelper.CreateImporter(Path.GetExtension(FilePath), ref importerName);

            if (Importer == null)
                Error |= ContentErrorType.ImporterError;

            processorName = element.Element("Processor")?.Value;
            if (string.IsNullOrWhiteSpace(processorName))
                processorName = PipelineHelper.GetProcessor(Name, importerName);

            if (!string.IsNullOrWhiteSpace(processorName) && Importer != null)
                Processor = PipelineHelper.CreateProcessor(Importer.GetType(), processorName);

            if (Importer == null)
                Error |= ContentErrorType.ProcessorError;

            if (Settings != null && element.Element("Settings") != null)
            {
                Settings.Read(element.Element("Settings"));
            }

            return this;
        }

        /// <summary>
        /// Serialize the object and return a XmlElement
        /// </summary>
        /// <returns></returns>
        public override XElement Serialize()
        {
            XElement element = new XElement("ContentFile");

            element.Add(new XElement("Name", Name));
            element.Add(new XElement("Processor", Processor?.GetType().Name));
            element.Add(new XElement("Importer", Importer?.GetType().Name));

            
            Settings?.Write(element);


            return element;
        }
    }
}
