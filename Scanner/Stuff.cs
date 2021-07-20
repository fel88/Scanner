using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Scanner
{
    public static class Stuff
    {
        public static string GetUserFriendlyFileSize(long _v)
        {
            double v = _v;
            string[] sfxs = new[] { "B", "Kb", "Mb", "Gb" };
            for (int i = 0; i < sfxs.Length; i++)
            {
                if (v < 1024)
                {
                    return v.ToString("F") + sfxs[i];
                }
                v /= 1024;
            }
            return v.ToString("F") + sfxs.Last();
        }
        public static List<DirectoryInfo> GetAllDirs(DirectoryInfo dir, List<DirectoryInfo> dirs = null)
        {
            if (dirs == null)
            {
                dirs = new List<DirectoryInfo>();
            }
            dirs.Add(dir);
            try
            {
                var dirss = dir.GetDirectories();
                foreach (var directoryInfo in dirss)
                {
                    try
                    {
                        GetAllDirs(directoryInfo, dirs);
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            catch
            {

            }
            return dirs;
        }

    }

}
