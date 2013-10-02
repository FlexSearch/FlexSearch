namespace FlexSearch.Benchmark
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Xml;

    internal class WikipediaTestDataGenerator
    {
        #region Fields

        private readonly bool discardSmallerPosts;

        private readonly bool generateFullDataSet;

        private readonly int postSizeKb;

        private readonly string rootFolder;

        private int requiredLength;

        #endregion

        #region Constructors and Destructors

        public WikipediaTestDataGenerator(
            string rootFolder,
            bool generateFullDataSet,
            int postSizeKb,
            bool discardSmallerPosts)
        {
            this.rootFolder = rootFolder;
            this.generateFullDataSet = generateFullDataSet;
            this.postSizeKb = postSizeKb;
            this.discardSmallerPosts = discardSmallerPosts;
            this.requiredLength = postSizeKb * 1024 / sizeof(char);
            this.GenerateFromModifiedExtractorOutput();
            //this.GenerateTestDataRegex();
            //this.GenerateTestData();
        }

        #endregion

        #region Methods

        private void GenerateFromModifiedExtractorOutput()
        {
            var fileName = string.Format(@"F:\wikipedia\wikidump{0}KB.txt", this.postSizeKb);
            var target = new StreamWriter(fileName);

            foreach (var file in Directory.EnumerateFiles(this.rootFolder))
            {
                using (var reader = new StreamReader(file))
                {
                    string text;
                    while ((text = reader.ReadLine()) != null)
                    {
                        if (text.EndsWith("[]"))
                        {
                            continue;
                        }

                        var title = text.Substring(1, text.IndexOf('>') - 1);
                        var body = text.Substring(text.IndexOf('[') + 1, text.Length - text.IndexOf('[') - 2);

                        if (!this.generateFullDataSet)
                        {
                            if (body.Length > this.requiredLength)
                            {
                                var breakAt = this.requiredLength;

                                while (body[breakAt] != ' ')
                                {
                                    if (breakAt < body.Length - 1)
                                    {
                                        breakAt++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                body = body.Substring(0, breakAt);
                            }
                            else if (this.discardSmallerPosts)
                            {
                                continue;
                            }
                        }

                        target.WriteLine("{0}|{1}", title, body);
                    }
                }
            }

            target.Flush();
            target.Close();
        }

        private void GenerateTestData()
        {
            var target = new StreamWriter(@"F:\wikipedia\wikidump5KB.txt");

            //create a reader settings objects and set the conformance level to fragment
            var xrs = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };

            foreach (var file in System.IO.Directory.EnumerateFiles(this.rootFolder))
            {
                var title = string.Empty;
                using (var reader = XmlReader.Create(file, xrs))
                {
                    while (reader.Read())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                if (reader.Name != "doc")
                                {
                                    continue;
                                }

                                title = reader.GetAttribute("title");
                                break;
                            case XmlNodeType.Text:
                                // There is no content it is a redirect post
                                try
                                {
                                    if (reader.Value == title + "\n\n")
                                    {
                                        continue;
                                    }

                                    target.WriteLine("<{0}>{1}", title, reader.Value.Replace("\n", " "));
                                }
                                catch (XmlException e)
                                {
                                }
                                break;
                            default:
                                continue;
                        }
                    }
                }
            }
        }

        private void GenerateTestDataRegex()
        {
            Regex regex = new Regex("\\<[^\\>]*\\>");
            var fileName = string.Format(@"F:\wikipedia\wikidump{0}KB.txt", this.postSizeKb);
            var target = new StreamWriter(fileName);

            foreach (var file in Directory.EnumerateFiles(this.rootFolder))
            {
                using (var reader = new StreamReader(file))
                {
                    var text = regex.Replace(reader.ReadToEnd(), string.Empty);
                    var lines = text.Split(new[] { "\r\n" }, StringSplitOptions.None);
                    for (int i = 0; i < lines.Length - 2; i = i + 2)
                    {
                        if (lines[i] == string.Empty)
                        {
                            continue;
                        }

                        if (lines[i + 1] == string.Empty)
                        {
                            continue;
                        }

                        if (!this.generateFullDataSet)
                        {
                            if (lines[i + 1].Length > this.requiredLength)
                            {
                                var breakAt = this.requiredLength;

                                while (lines[i + 1][breakAt] != ' ')
                                {
                                    if (breakAt < lines[i + 1].Length - 1)
                                    {
                                        breakAt++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                lines[i + 1] = lines[i + 1].Substring(0, breakAt);
                            }
                            else if (this.discardSmallerPosts)
                            {
                                continue;
                            }
                        }

                        var line = string.Format("<{0}>{1}", lines[i], lines[i + 1]);
                        if (line.StartsWith("<>"))
                        {
                            continue;
                        }

                        target.WriteLine(line);
                    }
                }
            }

            target.Flush();
            target.Close();

            this.RunTests(fileName);
        }

        private void RunTests(string file)
        {
            using (var reader = new StreamReader(file))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("<>"))
                    {
                        Console.WriteLine("Test data generation fialed as we have empty articles.");
                    }
                }
            }
        }

        #endregion
    }
}