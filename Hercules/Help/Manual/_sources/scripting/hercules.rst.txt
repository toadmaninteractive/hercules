.. meta::
   :http-equiv=X-UA-Compatible: IE=Edge

hercules
*********

The **hercules** variable provides access to Hercules application API.


alert
----------------

.. code-block:: javascript

   hercules.alert(message)

This function opens a modal message box with the user-provided message and one button ("OK").


confirm
----------------

.. code-block:: javascript

   hercules.confirm(message)

This function opens a modal message box with the user-provided message and two buttons ("OK" and "Cancel").
Returns ``true`` if OK is used. Otherwise, it returns ``false``.

.. _scripting_hercules_connections:

connections
----------------

.. code-block:: javascript

   hercules.connections

Contains the list of known connections. Each connection object has the following properties:

- *title* - user defined connection title
- *url* - CouchDB base URL
- *database* - CouchDB database name
- *username* - connection user name

customDialog
--------------

.. code-block:: javascript

   hercules.prompt(title[, fields])

This function opens a custom modal dialog with a provided title and set of fields and a single OK button. 

- *title* - dialog title
- *fields* - a field object or array. If it's an object, then each key is a field name and a value is a field descriptor. If it's an array, each item is a field descriptor.

Field descriptors may be one of the following:

- a simple *value*: bool, number or string - the default value for the field, the type is infered from the value.
- an array of string *values* - for combobox fields
- a descriptor object
  
Field descriptor objects may contain the following fields:

- *name* - a field name (used in the result object), only if the *fields* is an array
- *caption* - an optional field caption to be used instead of *name* in the dialog UI
- *type* - an optional field type, if it cannot be infered from *value*
- *value* - an optional default value
- *values* - a list of values for combobox fields

Supported field types are:

- *bool*
- *int*
- *float*
- *string*
- *date*
- *time*
- *datetime*

If OK is pressed, the function returns an object where keys are field names and values are field values confirmed by the user. 
If the dialog is closed (with ESC), the function returns **undefined**.

.. code-block:: javascript

   const {int_value, string_value, bool_value, datetime_value} = hercules.customDialog('Input Dialog',
       {int_value = 0, string_value = ['option1', 'option2'], bool_value = true, datetime_value = new Date()});

debug
----------------

.. code-block:: javascript

   hercules.debug(text)

This function outputs debug text or JSON to Hercules **Log Window**.

error
----------------

.. code-block:: javascript

   hercules.error(error)

This function outputs error text or JSON to Hercules **Log Window**.

getenv
--------

.. code-block:: javascript

   hercules.getenv(envVarName)

Returns environment variable by name. Useful in batch mode.

isBatchMode
-------------

.. code-block:: javascript

   hercules.isBatchMode

Returns ``true`` when running in batch mode, ``false`` otherwise.


loadDatabase
-------------

.. code-block:: javascript

   hercules.loadDatabase(title)

Loads the database by the connection title. Returns the object with the same API as :ref:`hercules.db <scripting_hercules_db>`.

You can access the active database using *hercules.db* variable, but *hercules.loadDatabase* can be used to load another database. 
However it doesn't replace the currently active database.

**Example:** Compare each document of the active database with the document with the same id in another database (ask the user for a connection title).

.. code-block:: javascript

   const connectionTitles = hercules.connections.map(c => c.title);
   const {database: title} = hercules.customDialog('Select database', {database: connectionTitles});
   const otherDb = hercules.loadDatabase(title);
   const myDocs = hercules.db.getAllDocs();
   for (const myDoc of myDocs) {
      const otherDoc = otherDb.get(myDoc._id);
      if (otherDoc) {
         const diff = hercules.json.diff(myDoc, otherDoc, ['_id', '_rev', '_attachments', 'hercules_metadata']);
         if (diff) {
             hercules.log(myDoc._id);
             hercules.log(diff);
         }
      }
   }

log
----------------

.. code-block:: javascript

   hercules.log(text)

This function outputs text or JSON to Hercules **Log Window**.

-  *text* - text that appears on **Log Window**. Any non-string JSON will be stringified.

**Example:** Write a code that outputs the names of all documents with field *price* greater than 10.


.. code-block:: javascript

   if (doc.price > 10)    
       hercules.log(doc.name);

open
----------------


.. code-block:: javascript
  
  hercules.open(docId) 

This function opens a document docId in the editor.

- *docId* - document id 

**Example:** Write a code that opens all the documents with field *action.type* set to *damage*. 


.. code-block:: javascript
  
   if (action.type == "damage")    
       hercules.open(doc._id);


addSearchResult
----------------


.. code-block:: javascript

   hercules.addSearchResult(docId, path, text) 

This function adds a document reference to the **Search Results** tool window.

- *docId* - document id 
- *path* - path of the found element 
- *text* - found text 

**Example:** Write a code that searches for all the documents whose names contain the substring "*test*".

.. code-block:: javascript

   if (doc.name.indexOf("test") >= 0)    
       hercules.addSearchResult(doc._id, "name", doc.name);


openFileDialog
----------------


.. code-block:: javascript

   hercules.openFileDialog(title[, extension]) 

This function shows a system open file dialog and returns the selected filename or null if the user cancels the dialog.

- *title* - dialog title
- *extension* - optional preferred file extension

**Example:** Prompt a user for the filename and load it as json.

.. code-block:: javascript

   var filename = hercules.openFileDialog("Load JSON", ".json");
   if (filename) {
      var json = hercules.io.loadJsonFromFile(filename);
   }

prompt
----------------

.. code-block:: javascript

   hercules.prompt(message[, defaultValue])

This function opens a modal message box with the text input box and two buttons ("OK" and "Cancel").

- *message* - message for the user
- *defaultValue* - optional default text for the text input box

*Return value* - a string containing the text entered by the user. Or it could be null.

saveFileDialog
----------------

.. code-block:: javascript

   hercules.saveFileDialog(title[, extension]) 

This function shows a system save file dialog and returns the selected filename or null if the user cancels the dialog.

- *title* - dialog title
- *extension* - optional preferred file extension

**Example:** Prompt a user for the filename and save all documents to the selected file.

.. code-block:: javascript

   var filename = hercules.saveFileDialog("Save JSON", ".json");
   if (filename) {
      hercules.io.saveJsonToFile(filename, hercules.db.getAllDocs());
   }

view
---------

.. code-block:: javascript

   hercules.view(title, content[, options]) 

This function opens *content* in a separate tab page with the given *title*.

- *title* - page title
- *content* - text or JSON content to show
- *options* - optional options object. 

The following options are supported:

- *type* - *'text'*, *'json'* or *'xml'*
- *syntax* - syntax highlighting file name (one of the files located in **SyntaxHighlight** folder inside installation path)
- *background* - set **true** to open page in background

warning
----------------

.. code-block:: javascript

   hercules.error(error)

This function outputs warning text or JSON to Hercules **Log Window**.
