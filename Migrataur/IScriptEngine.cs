using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Migrataur
{
    public interface IScriptEngine
    {
        bool NeedsUpdating();
        bool Update();
    }
}