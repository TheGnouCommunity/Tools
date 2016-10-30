/*
    MIT License

    Copyright (c) 2016 @Boulc (https://github.com/Boulc).

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
namespace TheGnouCommunity.Tools.Synchronization
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public class Synchronizer
    {
        private readonly object comparisonSyncLock = new object();

        private readonly string sourcePath;
        private readonly string targetPath;

        private readonly IEnumerable<FileInfoWrapper> sourceFiles;
        private readonly IEnumerable<FileInfoWrapper> targetFiles;

        private Dictionary<string, FileInfoWrapper> relativeSourceFiles;
        private Dictionary<string, FileInfoWrapper> relativeTargetFiles;

        private HashSet<FileInfoWrapper> identicalFiles;
        private HashSet<FileInfoWrapper> differentFiles;
        private HashSet<FileInfoWrapper> missingFiles;
        private HashSet<FileInfoWrapper> extraFiles;
        private List<Tuple<FileInfoWrapper, FileInfoWrapper>> similarFiles;
        private HashSet<FileInfoWrapper> conflictedFiles;

        private Stopwatch sw = new Stopwatch();
        private TimeSpan? comparisonDuration;

        public Synchronizer(string sourcePath, string targetPath)
        {
            this.sourcePath = sourcePath;
            this.targetPath = targetPath;

            this.sourceFiles = FileInfoWrapper.GetFiles(this.sourcePath);
            this.targetFiles = FileInfoWrapper.GetFiles(this.targetPath);
        }

        public IEnumerable<string> SourceFiles
        {
            get
            {
                return this.relativeSourceFiles.Keys;
            }
        }

        public IEnumerable<string> TargetFiles
        {
            get
            {
                return this.relativeTargetFiles.Keys;
            }
        }

        public IEnumerable<string> IdenticalFiles
        {
            get
            {
                return this.identicalFiles.Select(t => t.RelativePath);
            }
        }

        public IEnumerable<string> DifferentFiles
        {
            get
            {
                return this.differentFiles.Select(t => t.RelativePath);
            }
        }

        public IEnumerable<string> MissingFiles
        {
            get
            {
                return this.missingFiles.Select(t => t.RelativePath);
            }
        }

        public IEnumerable<string> ExtraFiles
        {
            get
            {
                return this.extraFiles.Select(t => t.RelativePath);
            }
        }

        public IEnumerable<Tuple<string, string>> SimilarFiles
        {
            get
            {
                return this.similarFiles.Select(t => Tuple.Create(t.Item1.RelativePath, t.Item2.RelativePath));
            }
        }

        public IEnumerable<string> ConflictedFiles
        {
            get
            {
                return this.conflictedFiles.Select(t => t.RelativePath);
            }
        }

        public void Run() => this.Run(ComparisonOptions.None());

        public void Run(ComparisonOptions options)
        {
            lock (this.comparisonSyncLock)
            {
                this.comparisonDuration = null;
                this.sw.Restart();

                this.Compare(options);
                this.SearchForSimilarFiles(ComparisonOptions.FileLength());
                this.DetectConflicts();
                this.ResolveConflicts();

                this.sw.Stop();
                this.comparisonDuration = this.sw.Elapsed;

                Console.WriteLine($"Process ran in {sw.ElapsedMilliseconds} ms.");
                Console.WriteLine();

                this.WriteSummary();
            }
        }

        private void Compare(ComparisonOptions options)
        {
            Console.Write("Comparing files");

            this.relativeSourceFiles = new Dictionary<string, FileInfoWrapper>();
            this.relativeTargetFiles = targetFiles.ToDictionary(t => t.RelativePath, t => t);

            this.identicalFiles = new HashSet<FileInfoWrapper>();
            this.differentFiles = new HashSet<FileInfoWrapper>();
            this.missingFiles = new HashSet<FileInfoWrapper>();
            this.extraFiles = new HashSet<FileInfoWrapper>(this.relativeTargetFiles.Values);

            int n;
            foreach (FileInfoWrapper sourceFile in this.sourceFiles)
            {
                this.relativeSourceFiles.Add(sourceFile.RelativePath, sourceFile);

                FileInfoWrapper targetFile;
                if (this.relativeTargetFiles.TryGetValue(sourceFile.RelativePath, out targetFile))
                {
                    if (FileInfoWrapper.Equals(sourceFile, targetFile, options))
                    {
                        this.identicalFiles.Add(sourceFile);
                    }
                    else
                    {
                        this.differentFiles.Add(sourceFile);
                    }

                    this.extraFiles.Remove(sourceFile);
                }
                else
                {
                    this.missingFiles.Add(sourceFile);
                }

                n = this.identicalFiles.Count + this.differentFiles.Count + this.missingFiles.Count;
                if (n % 1000 == 0)
                {
                    Console.Write(".");
                }
            }

            Console.WriteLine();
        }

        private void SearchForSimilarFiles(ComparisonOptions options)
        {
            Console.Write("Searching for similar files");

            this.similarFiles = new List<Tuple<FileInfoWrapper, FileInfoWrapper>>();

            int n = 1;
            foreach (FileInfoWrapper missingFile in this.missingFiles)
            {
                foreach (FileInfoWrapper extraFile in this.extraFiles)
                {
                    if (missingFile.Info.Name == extraFile.Info.Name)
                    {
                        if (FileInfoWrapper.Equals(missingFile, extraFile, options))
                        {
                            this.similarFiles.Add(Tuple.Create(missingFile, extraFile));
                        }
                    }
                }

                if (n++ % 10 == 0)
                {
                    Console.Write(".");
                }
            }

            Console.WriteLine();
        }

        private void DetectConflicts()
        {
            Console.Write("Detecting conflicts");

            this.conflictedFiles = new HashSet<FileInfoWrapper>(this.similarFiles.GroupBy(t => t.Item1).Where(t => t.Count() > 1).Select(t => t.Key));

            Console.WriteLine();
        }

        private void ResolveConflicts()
        {
            Console.Write("Resolving conflicts");

            int i = 0;
            while (i != this.similarFiles.Count)
            {
                var similarFile = this.similarFiles[i];
                if (!this.conflictedFiles.Contains(similarFile.Item1))
                {
                    this.extraFiles.Remove(similarFile.Item2);
                    i++;
                }
                else
                {
                    this.similarFiles.RemoveAt(i);
                }

                this.missingFiles.Remove(similarFile.Item1);

                if (i % 10 == 0)
                {
                    Console.Write(".");
                }
            }

            Console.WriteLine();
        }

        private void WriteSummary()
        {
            Console.WriteLine("Process summary:");
            Console.WriteLine($"\t- {this.relativeSourceFiles.Count} source files.");
            Console.WriteLine($"\t- {this.relativeTargetFiles.Count} target files.");
            Console.WriteLine($"\t- {this.identicalFiles.Count} identical files.");
            Console.WriteLine($"\t- {this.differentFiles.Count} different files.");
            Console.WriteLine($"\t- {this.missingFiles.Count} missing files.");
            Console.WriteLine($"\t- {this.extraFiles.Count} extra files.");
            Console.WriteLine($"\t- {this.similarFiles.Count} similar files.");
            Console.WriteLine($"\t- {this.conflictedFiles.Count} conflicted files.");
            Console.WriteLine();
        }
    }
}
