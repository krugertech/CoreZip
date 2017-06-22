# CoreZip
• Very simple wrapper for natively zipping and unzipping files in .net 4.5.0+
• Handles massive archives i.e. 50GB easily.
• Efficient use of memory and no out of memory errors.
• Sufficient options for granular control.

# Usage
```cs
using Simplified.IO;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {            
            CoreZip.Compress(@"c:\mydir", @"C:\myfile.zip", CoreZip.ExistingArchiveAction.Update);
            CoreZip.Uncompress(@"C:\myfile.zip", @"c:\");
        }
    }
}
```

# Credit
Originally based off the work of Tim Corey on CodeProject.
