using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NauUpdateGenerator
{
    public class Config
    {
        public string BaseUpdateUrl { get; set; }
        public string MainChannelName { get; set; }
        //public string AssetPath { get; set; }
        //public string FeedPath { get; set; }

        public static Config Load(string configPath)
        {
            var doc = XDocument.Parse(File.ReadAllText(configPath));

            var conf = new Config();

            conf.BaseUpdateUrl = GetVal(doc, nameof(BaseUpdateUrl));
            conf.MainChannelName = GetVal(doc, nameof(MainChannelName));
            //conf.AssetPath = doc.Root.Element(nameof(AssetPath)).Value;
            //conf.FeedPath = doc.Root.Element(nameof(FeedPath)).Value;

            return conf;
        }

        private static string GetVal(XDocument doc, string name)
        {
            try
            {
                return doc.Root.Element(name).Value;
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to read '{name}' from the config. Does it exist?", e);
            }
        }

        public static void CreateNew(string configPath)
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("configuration",
                    new XElement(nameof(BaseUpdateUrl), "UPDATE-ME"),
                    new XElement(nameof(MainChannelName), "stable")
                //new XElement(nameof(AssetPath), "assets\\"),
                //new XElement(nameof(FeedPath), "feed\\")
                )
            );

            var wr = new StringWriter();
            doc.Save(wr);

            File.WriteAllText(configPath, wr.ToString());
        }
    }
}
