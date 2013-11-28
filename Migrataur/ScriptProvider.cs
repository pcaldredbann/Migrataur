using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Migrataur
{
    public sealed class ScriptProvider
    {
        internal readonly IList<Script> _scripts = new List<Script>();

        /// <summary>
        /// Searches through the provided assembly for all embedded .sql files and creates a Script object for each.
        /// </summary>
        /// <param name="assembly">The assembly to process.</param>
        public ScriptProvider(Assembly assembly)
        {
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (Path.GetExtension(resourceName).ToLowerInvariant().Trim().Equals(".sql"))
                {
                    var resource = assembly.GetManifestResourceStream(resourceName);

                    using (StreamReader sr = new StreamReader(resource))
                    {
                        _scripts.Add(new Script() {
                            Name = Path.GetFileName(resourceName),
                            Content = sr.ReadToEnd().Trim()
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Searches through the provided file path for all .sql files and creates a Script object for each.
        /// </summary>
        /// <param name="path">The file path to process.</param>
        public ScriptProvider(string path)
        {
            foreach (string fileName in Directory.GetFiles(path))
            {
                string fullPath = Path.Combine(path, fileName);

                if (Path.GetExtension(fileName).ToLowerInvariant().Equals(".sql"))
                {
                    using (FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                    {
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            _scripts.Add(new Script() {
                                Name = fileName,
                                Content = sr.ReadToEnd().Trim()
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a collection of all migration scripts parsed from an assembly or file path.
        /// </summary>
        /// <returns>IList containing all migration scripts.</returns>
        public IList<Script> GetScripts()
        {
            return _scripts.ToList();
        }

        /// <summary>
        /// Adds the provided Script to the internal collection.
        /// </summary>
        /// <param name="script">The Script to add.</param>
        public void AddScript(Script script)
        {
            if (_scripts.Count(s => s.Name == script.Name) == 0)
            {
                _scripts.Add(script);
            }
        }

        /// <summary>
        /// Removes the provided Script object from the internal collection.
        /// </summary>
        /// <param name="script">The Script object to remove.</param>
        public void RemoveScript(Script script)
        {
            _scripts.Remove(script);
        }

        /// <summary>
        /// Removes the provided Script name from the internal collection.
        /// </summary>
        /// <param name="scriptName">The name of the Script object to remove.</param>
        public void RemoveScript(string scriptName)
        {
            var scriptToRemove = _scripts.Single(s => s.Name == scriptName);

            _scripts.Remove(scriptToRemove);
        }
    }
}