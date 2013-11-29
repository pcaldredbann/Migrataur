Migrataur
=========

Migrataur is an automatic database migration tool that uses ADO.Net to apply and track .sql migration scripts applied or pending to the database.

Quick Start
=========
If you want to get started with Migrataur quickly, then you can copy-and-paste the code below. It obviously needs a small amount of customizing.

    public void InitializeDatabaseWithMigrataur()
    {
        string dbConnectionString = "Data Source=(your data source); Initial Catalog=(your catalog); User Id=(your user id); Password=(your password);";

        // if you are embedding your .sql files into your assembly, then do this:
        var scriptProvider = new ScriptProvider(Assembly.GetExecutingAssembly());

        // otherwise, you need to pass a physical path to your .sql files
        var scriptProvider = new ScriptProvider(@"%PATH%\SQL Migration Files");

        var scriptEngine = new ScriptEngine(dbConnectionString, scriptProvider);
        if (scriptEngine.NeedsUpdating())
        {
            scriptEngine.Update();
        }
    }
  
Documentation
=========
Migrataur is composed of the following core components.

 - Script - each SQL script is parsed to this object, it contains a Name and a Content property. When the script is applied to your database it records the name given here against the time stamp, and executes the commands in the content.

 - ScriptProvider - this object finds scripts in a given assembly, or a given path, and converts each one to a Script. Once the scripts have been added to the internal collection they are then ordered by file name alphanumerically. It's important to note that this is the order scripts will be applied to the database, so I usually prefix my files with an ordinal like this 1 - Initial setup.sql and 2 - Seed database.sql etc.

 - ScriptEngine - this is where the magic happens, it iterates through each Script in the ordered collection and applies them to the database.
