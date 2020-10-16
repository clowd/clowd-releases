using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NauUpdateGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            // refactor these into cli params
            const string configPath = "generator.config";
            bool interactive = true;

            try
            {
                Run(configPath, interactive);
                Console.WriteLine("Generation success!");
            }
            catch (Exception e)
            {
                Console.WriteLine("An error has occurred: ");
                Console.WriteLine(e.ToString());
                Environment.ExitCode = 1;
            }

            if (interactive)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to exit..");
                Console.ReadKey();
            }
        }

        static void Run(string configPath, bool interactive)
        {
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"A config file was not found: '{configPath}'");
                Console.WriteLine("Creating new config file...");
                Config.CreateNew(configPath);
                Console.WriteLine("A blank config has been created. Please update this file and run the generator again.");
                return;
            }

            Console.WriteLine("Loading config file...");
            Config conf = Config.Load(configPath);

            const string assetPath = "assets";
            const string feedPath = "feed";
            string baseUrl = conf.BaseUpdateUrl.TrimEnd('/');

            if (!Directory.Exists(assetPath))
                throw new Exception("Asset path does not exist or is not writable: " + Path.GetFullPath(assetPath));

            if (!Directory.Exists(feedPath))
                throw new Exception("Feed path does not exist or is not writable: " + Path.GetFullPath(feedPath));

            //var indexDocument = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), );
            var index = new XElement("Assets", new XAttribute("MainChannel", conf.MainChannelName));

            var channels = Directory.GetDirectories(assetPath);
            foreach (var channelPath in channels)
            {
                var channelName = Path.GetFileName(channelPath);
                var versions = Directory.GetDirectories(channelPath);
                foreach (var versionPath in versions)
                {
                    var versionName = Path.GetFileName(versionPath);
                    var packageName = $"{versionName}-{channelName}";

                    Console.WriteLine($"Writing feed for {packageName}");

                    index.Add(new XElement("Package",
                        new XAttribute("Version", versionName),
                        new XAttribute("Channel", channelName),
                        new XAttribute("FeedUrl", $"{baseUrl}/{feedPath}/{packageName}.xml")
                    ));

                    var vdir = Path.GetFullPath(versionPath);

                    File.WriteAllText(Path.Combine(vdir, "version"), packageName);

                    XElement feed = new XElement("Feed");
                    feed.SetAttributeValue("BaseUrl", $"{baseUrl}/{assetPath}/{channelName}/{versionName}");
                    XElement tasks = new XElement("Tasks");
                    foreach (var file in GetFiles(vdir))
                    {
                        // ignore build related files
                        if (file.EndsWith(".pdb"))
                            continue;

                        if (file.EndsWith(".xml") && File.Exists(file.Substring(0, file.Length - 4) + ".dll"))
                            continue;

                        var relative = GetRelativePath(file, vdir);
                        var fileInfo = new FileInfo(file);
                        XElement fut = new XElement("FileUpdateTask");
                        fut.SetAttributeValue("localPath", relative);
                        fut.SetAttributeValue("lastModified", fileInfo.LastWriteTime.ToFileTime().ToString(System.Globalization.CultureInfo.InvariantCulture));
                        fut.SetAttributeValue("fileSize", fileInfo.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        XElement cond = new XElement("Conditions");
                        XElement checksum = new XElement("FileChecksumCondition");
                        checksum.SetAttributeValue("type", "not");
                        checksum.SetAttributeValue("checksumType", "sha256");
                        checksum.SetAttributeValue("checksum", GetSHA256Checksum(file));
                        cond.Add(checksum);
                        fut.Add(cond);
                        tasks.Add(fut);
                    }
                    feed.Add(tasks);

                    File.WriteAllText(Path.Combine(feedPath, $"{packageName}.xml"), "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + feed.ToString());
                }
            }

            Console.WriteLine("Writing assets.xml");
            File.WriteAllText("assets.xml", "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + index.ToString());

        }
        //public void Page_Load(object sender, EventArgs e)
        //{
        //    string req = "latest";
        //    char branch = 's';

        //    try
        //    {
        //        var tmp = Request.QueryString["v"];
        //        if (!String.IsNullOrWhiteSpace(tmp))
        //        {
        //            req = tmp;
        //        }
        //    }
        //    catch { }

        //    try
        //    {
        //        var tmp = Request.QueryString["b"];
        //        if (!String.IsNullOrWhiteSpace(tmp) && tmp.Length == 1)
        //        {
        //            branch = tmp[0];
        //        }
        //    }
        //    catch { }


        //    if (!req.Equals("latest", StringComparison.InvariantCultureIgnoreCase) && Char.IsLetter(req[req.Length - 1]))
        //    {
        //        branch = req[req.Length - 1];
        //        req = req.Substring(0, req.Length - 1);
        //    }
        //    string directory = Path.GetDirectoryName(Request.PhysicalPath);
        //    decimal version = default(decimal);
        //    if (!req.Equals("latest", StringComparison.InvariantCultureIgnoreCase))
        //    {
        //        decimal t;
        //        if (decimal.TryParse(req, out t))
        //        {
        //            if (Directory.Exists(Path.Combine(directory, t.ToString() + branch)))
        //            {
        //                version = t;
        //            }
        //        }
        //    }
        //    if (version == default(decimal))
        //    {
        //        foreach (string dir in Directory.GetDirectories(directory))
        //        {
        //            string name = Path.GetFileName(dir);
        //            char b = name[name.Length - 1];
        //            if (b == branch)
        //            {
        //                name = name.Substring(0, name.Length - 1);
        //                var v = decimal.Parse(name);
        //                if (v > version)
        //                    version = v;
        //            }
        //        }
        //    }
        //    var vdir = Path.Combine(directory, version.ToString() + branch);
        //    XElement feed = new XElement("Feed");
        //    feed.SetAttributeValue("BaseUrl", "http://clowd.ca/app_updates/" + version.ToString() + branch + "/");
        //    XElement tasks = new XElement("Tasks");
        //    foreach (var file in GetFiles(vdir))
        //    {
        //        var relative = GetRelativePath(file, vdir);
        //        var fileInfo = new FileInfo(file);
        //        XElement fut = new XElement("FileUpdateTask");
        //        fut.SetAttributeValue("localPath", relative);
        //        fut.SetAttributeValue("lastModified", fileInfo.LastWriteTime.ToFileTime().ToString(System.Globalization.CultureInfo.InvariantCulture));
        //        fut.SetAttributeValue("fileSize", fileInfo.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
        //        XElement cond = new XElement("Conditions");
        //        XElement checksum = new XElement("FileChecksumCondition");
        //        checksum.SetAttributeValue("type", "not");
        //        checksum.SetAttributeValue("checksumType", "sha256");
        //        checksum.SetAttributeValue("checksum", GetSHA256Checksum(file));
        //        cond.Add(checksum);
        //        fut.Add(cond);
        //        tasks.Add(fut);
        //    }
        //    feed.Add(tasks);
        //    Response.ContentType = "text/xml";
        //    Response.Write("<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine);
        //    Response.Write(feed.ToString());
        //}

        public static string GetSHA256Checksum(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                var sha = new System.Security.Cryptography.SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }

        public static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public static IEnumerable<string> GetFiles(string path)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception ex)
                {
                    //Console.Error.WriteLine(ex);
                }
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex)
                {
                    //Console.Error.WriteLine(ex);
                }
                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        yield return files[i];
                    }
                }
            }
        }
    }
}
