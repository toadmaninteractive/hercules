.. meta::
   :http-equiv=X-UA-Compatible: IE=Edge

hercules.gapi
***************
 
The **hercules.gapi** variable provides access to Google API.

getSheetNames
----------------

.. code-block:: javascript

   hercules.io.getSheetNames(spreadheetId[, options]) 

This function returns the array of Spreadsheet sheet names.

Loads a spreadsheet table from the file.

- *spreadheetId* - spreadheetId (get it from the URL)
- *options* - options

Supported options:

- *credential* - GAPI credential object

**Example**: Output sheet titles to the log:

.. code-block:: javascript

    for (const sheet of hercules.gapi.getSheetNames('1AeKmu6iZrHDVUJ9...'))
        hercules.log(sheet);

loadTable
----------

.. code-block:: javascript

   hercules.io.loadTableFromFile(spreadheetId[, options]) 

Loads a spreadsheet table from the file.

- *spreadheetId* - spreadheetId (get it from the URL, e.g. ``'1BxiMVs0XRA5nFMdKvB...'``)
- *options* - options

Supported options:

- *sheet* - Sheet name (default is the first one)
- *range* - Cell range (default is ``'A:Z'``)
- *header* - Use header mode (default is ``true``)
- *credential* - GAPI credential object

When *header* is ``true`` (default), the first row in a table is expected to be a row with column headers. The function returns an array of rows, where each row is an object.
Keys are column header strings and values are cell values.

When *header* is ``false``, the first row is a regular row, and each row is an array of cells, not an object.

Cell values are always strings.
