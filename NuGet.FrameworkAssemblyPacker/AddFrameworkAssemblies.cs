using System;
using System.IO;
using Microsoft.Build.Framework;

namespace NuGet.FrameworkAssemblyPacker
{
    public class AddFrameworkAssemblies : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string NuSpecFile { get; set; }

        [Required]
        public string PatchedNuSpecFile { get; set; }

        [Required]
        public ITaskItem[] FrameworkAssemblies { get; set; }

        public override bool Execute()
        {
            var content = File.ReadAllText(NuSpecFile);
            if (content.IndexOf("<frameworkAssemblies>", StringComparison.Ordinal) >= 0)
            {
                Log.LogWarning("'{0}' already contains a frameworkAssemblies element, you can probably don't need the NuGet.FrameworkAssemblyPacker package", NuSpecFile);
                return false;
            }

            var end = content.IndexOf("</metadata>", StringComparison.Ordinal);
            var newContent = content.Substring(0, end);
            newContent += "<frameworkAssemblies>\n";
            foreach (var r in FrameworkAssemblies ?? new ITaskItem[0])
            {
                var targetFramework = r.GetMetadata("TargetFramework");
                if (string.IsNullOrEmpty(targetFramework))
                {
                    continue;
                }
                newContent += "    <frameworkAssembly assemblyName=\"" + r.ItemSpec + "\" targetFramework=\"" + targetFramework + "\" />\n";
            }
            newContent += "  </frameworkAssemblies>\n  ";
            newContent += content.Substring(end);
            File.WriteAllText(PatchedNuSpecFile, newContent);
            return true;
        }
    }
}
