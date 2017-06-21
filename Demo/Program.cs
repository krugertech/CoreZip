using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Simplified.IO;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {            
            CoreZip.Compress(@"c:\xxxxxx", @"C:\xxx.zip", CoreZip.ExistingArchiveAction.Update);
            CoreZip.Uncompress(@"C:\xxx.zip", @"c:\");
        }
    }
}
