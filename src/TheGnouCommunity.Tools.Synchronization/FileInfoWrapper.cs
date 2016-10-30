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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal class FileInfoWrapper
    {
        private readonly string fullPath;
        private readonly string relativePath;
        private FileInfo info = null;

        private FileInfoWrapper(string basePath, string fullPath)
        {
            this.fullPath = fullPath;
            this.relativePath = fullPath.Substring(basePath.Length + 1);
        }

        public static IEnumerable<FileInfoWrapper> GetFiles(string basePath) => Directory
            .EnumerateFiles(basePath, "*.*", SearchOption.AllDirectories)
            .Select(t => new FileInfoWrapper(basePath, t));

        public string RelativePath
        {
            get
            {
                return this.relativePath;
            }
        }

        public FileInfo Info
        {
            get
            {
                if (this.info == null)
                {
                    this.info = new FileInfo(this.fullPath);
                }

                return this.info;
            }
        }

        public override string ToString()
        {
            return this.RelativePath;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (this.GetType() != obj.GetType())
            {
                return false;
            }

            FileInfoWrapper other = (FileInfoWrapper)obj;

            if (!object.Equals(this.RelativePath, other.RelativePath))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return this.RelativePath.GetHashCode();
            ;
        }
    }
}
