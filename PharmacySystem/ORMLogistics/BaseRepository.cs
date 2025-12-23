using PharmacySystem.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace PharmacySystem.ORMLogistics
{
    public abstract class XmlRepository<T> where T : BaseEntity
    {
        protected readonly string FilePath;
        protected readonly string RootName;

        protected XmlRepository(string filePath, string rootName)
        {
            FilePath = filePath;
            RootName = rootName;
            InitializeFile();
        }

        private void InitializeFile()
        {
            if (!File.Exists(FilePath))
            {
                new XDocument(new XElement(RootName)).Save(FilePath);
            }
        }

        protected XDocument GetDocument() => XDocument.Load(FilePath);

        public abstract void Save(T entity);
        public abstract List<T> GetAll();

        public void Delete(Guid id)
        {
            var doc = GetDocument();
            doc.Root.Elements().FirstOrDefault(e => e.Attribute("Id")?.Value == id.ToString())?.Remove();
            doc.Save(FilePath);
        }
    }
}