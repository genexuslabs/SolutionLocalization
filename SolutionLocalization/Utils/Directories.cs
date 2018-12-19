using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SolutionLocalization.Utils
{

    public static class Directories
    {
        public static IEnumerable<FileInfo> GetFilesByPattern(this DirectoryInfo directory, string pattern)
        {
            return directory.GetFilesByPattern(pattern, SearchOption.AllDirectories);
        }

        public static IEnumerable<FileInfo> GetFilesByPattern(this DirectoryInfo directory, string pattern, SearchOption searchOption)
        {
            string[] patterns = pattern.Split(';');
            foreach (string singlePattern in patterns)
            {
                foreach (FileInfo file in directory.GetFiles(singlePattern, searchOption))
                    yield return file;
            }
        }

    }

    public static class Assemblies
    {
        public static string GetAssemblyDirectory()
        {
            Assembly assembly = Assembly.GetCallingAssembly();
            return Path.GetDirectoryName(assembly.Location);
        }
    }
}
   
