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
    using Microsoft.Extensions.Configuration;

    public class Program
    {
        public static void Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                   .AddJsonFile("appsettings.json");

            IConfigurationRoot configuration = builder.Build();
            IConfigurationSection jobs = configuration.GetSection("jobs");
            Synchronizer s = null;
            foreach (IConfigurationSection job in jobs.GetChildren())
            {
                try
                {
                    s = new Synchronizer(job.Key, job["sourcePath"], job["targetPath"]);
                    s.Run();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unhandled exception occurred: {ex.Message}");
                }
            }

            Console.WriteLine("Press Enter key to close...");
            Console.ReadLine();
        }
    }
}
