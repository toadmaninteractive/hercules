.. meta::
   :http-equiv=X-UA-Compatible: IE=Edge

hercules.io
***************
 
The **hercules.io** variable provides access to the Input/Output library API.

getSheetNames
----------------

.. code-block:: javascript

   hercules.io.getSheetNames(filename) 

This function returns the array of Excel sheet names.

loadJsonFromFile
----------------

.. code-block:: javascript

   hercules.io.loadJsonFromFile(filename) 

This function loads JSON from the file. Returns the loaded JSON.

loadTableFromFile
-----------------

.. code-block:: javascript

   hercules.io.loadTableFromFile(filename, [, options]) 

Loads a spreadsheet table from the file.

- *filename* - target file name. Use ".xlxs" or ".xls" extensions for an Excel file, or ".csv" for a CSV file
- *options* - options

Supported options:

- *sheet* - Sheet name or index (default is 0)

The first row in a table is expected to be a row with column headers. The function returns an array of rows, where each row is an object.
Keys are column header strings and values are cell values.

For CSV tables, all cell values are strings. For Excel tables, cell values may be string, boolean or numeric values.

**Example**: Load a table from Excel file, and update or create a document for each row:

.. code-block:: javascript

   var filename = hercules.openFileDialog("Load a table");
   if (filename) {
      var rows =  hercules.io.loadTableFromFile(filename);
      for (var row of rows) {
          var id = row['_id'];
          var doc = {
             category: 'my_category',
             field1: row['field1'],
             field2: row['field2']
          };
          hercules.db.updateOrCreate(id, doc);
      }      
   }

loadTextFromFile
----------------

.. code-block:: javascript

   hercules.io.loadTextFromFile(filename) 

This function loads text from the file. Returns the loaded string.


saveJsonToFile
----------------

.. code-block:: javascript

   hercules.io.saveJsonToFile(filename, json) 

Saves JSON to the file.


saveTableToFile
----------------

.. code-block:: javascript

   hercules.io.saveTableToFile(filename, columns, rows[, options]) 

Saves a spreadsheet table to the file.

- *filename* - target file name. Use ".xlxs" or ".xls" extensions for an Excel file, or ".csv" for a CSV file
- *column* - list of table column names
- *rows* - list of table rows
- *options* - options

Supported options:

- *sheet* - Sheet name (default is ``"Sheet1"``)
- *open* - Open a spreadsheet after save if *open* is ``true``

Each row may be either an array of cells or an object where keys are column names and values are cell values.

**Example**: Export all documents from category ``my_category`` to the table. 
The table will have 3 columns: ``_id`` containing document id, and ``field1`` and ``field2`` with corresponding document fields.
The spreadsheet file is opened after export.

.. code-block:: javascript

   var filename = hercules.saveFileDialog("Save a table");
   const columns = ['_id', 'field1', 'field2'];
   if (filename) {
      var rows =  hercules.db.docsByCategory('my_category');
      hercules.io.saveTableToFile(filename, columns, rows, {open: true});
   }


saveTextToFile
----------------

.. code-block:: javascript

   hercules.io.saveTextToFile(filename, text[, encoding]) 

Saves text to the file.

- *filename* - target file name
- *text* - text to be stored
- *encoding* - optional encoding, ``utf-8`` is used by default.

**Example**: Save JSON as utf-16 to the project repository file:

.. code-block:: javascript

   var targetText = JSON.stringify(json, null, '\t');
   var targetPath = hercules.project.rootFolder + "\\Content\\Localization\\Game\\en\\Game.archive";
   hercules.io.saveTextToFile(targetPath, targetText, "utf-16");
