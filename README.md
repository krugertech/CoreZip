# CoreZip
A simple wrapper for natively zipping and unzipping files in .net 4.5.0+
for quickly compressing and uncompressing folders.

# Usage
```cs
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
```
