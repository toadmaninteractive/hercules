.. meta::
   :http-equiv=X-UA-Compatible: IE=Edge

hercules.xml
***************
 
The **hercules.xml** variable provides access to the XML library API.



toJson
----------------

.. code-block:: javascript

   hercules.xml.toJson(xml) 

This function converts XML string to JSON that can be processed with the script.

**Example:** Write a code that accepts XML and processes it as JSON.

.. code-block:: javascript

   var filename = hercules.openFileDialog("Load XML", ".xml");
   if (filename) {
       var xml = hercules.io.loadTextFromFile(filename);
       var json = hercules.xml.toJson(xml);
   }

fromJson
----------------

.. code-block:: javascript

   hercules.xml.fromJson(json, rootName[, options]) 

This function converts JSON to XML string.

- *json* - source JSON
- *rootName* - name of the root XML element
- *options* - options object (optional)

Supported options:

- *indent* - indentation string (two spaces by default) or null to disable pretty print
- *encoding* - encoding (default is ``utf-8``)
- *xmlns* - xml namespace, if needed
- *attributes* - list of keys that are treated as XML attributes rather than elements
