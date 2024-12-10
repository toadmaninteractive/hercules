.. meta::
   :http-equiv=X-UA-Compatible: IE=Edge

hercules.json
***************
 
The **hercules.json** variable provides access to the JSON library API.



diff
----------------

.. code-block:: javascript

   hercules.json.diff(json1, json2[, excludeKeys])

This function compares two JSON objects and returns an array of differences.

- *json1* - first object
- *json2* - second object
- *excludeKeys* - an optional lists of root level keys to exclude from comparison

Each difference in the output array is an object with the following properties:

- *path* - JSON path of the difference chunk
- *value1* - the value in the first object
- *value2* - the value in the second object

fetch
----------------

.. code-block:: javascript

   hercules.json.fetch(json, path)

This function returns value stored under path or undefined if path does not exist.

- *json* - source JSON
- *path* - JSON path, use dot as nested fields delimiter and [] for accessing object keys

patch
----------------

.. code-block:: javascript

   hercules.json.patch(json, path, value)

This function stores *value* under *path* in the given JSON object and returns the modified object. If the old value exists, it is replaced.

- *json* - source JSON
- *path* - JSON path, use dot as nested fields delimiter and [] for accessing object keys
- *value* - a value to store
