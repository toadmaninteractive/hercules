.. meta::
   :http-equiv=X-UA-Compatible: IE=Edge

.. _scripting_hercules_db:

hercules.db
***************
 
The **hercules.db** variable provides access to the current database API.

create
-------

.. code-block:: javascript

   hercules.db.create(id, document)

This function creates a new document with a specified id and content.

The created document is not saved, but will be opened for validation after script finishes.

connection
-----------

.. code-block:: javascript

   hercules.db.connection

Contains the connection properties for the given database. See :ref:`hercules.connections <scripting_hercules_connections>` for details.

delete
-------

.. code-block:: javascript

  hercules.delete(docId) 

This function deletes a document through its docId. When you use this function, a confirmation dialog comes up.

-  docId - document id

**Example:** Write a function that deletes all the documents whose ids contain the substring "*test*".

.. code-block:: javascript

   if (doc._id.indexOf("test") >= 0)    
       hercules.delete(doc._id);

docsByCategory
----------------

.. code-block:: javascript

    hercules.db.docsByCategory(category)

This function returns an array of all documents under a specific category
when the category id is passed as a parameter.


get
---

.. code-block:: javascript

   hercules.db.get(id[, rev]) 

This function gets a document through its *id* and optional *rev*.


getAllDocs
-----------

.. code-block:: javascript

    hercules.db.getAllDocs()

This function returns an array of all documents for all categories in a database.


getDocRevisions
----------------

.. code-block:: javascript

    hercules.db.getDocRevisions(id)

This function returns an array of documents revisions by its *id*, ordered from the latest to the oldest. 
*hercules.db.get* function can be then used to fetch document JSON for a specific revision.


getHistory
----------------

.. code-block:: javascript

    hercules.db.getHistory(since)

This function returns an array of database changes since the specified time. Time should be passed as the Date object.

Each change is an object with the following properties:

- *id* - document ID
- *rev* - document revision introduced with this change
- *prevRev* - previous document revision
- *time* - time of the change as a Date object
- *user* - author username


**Example:** Get history since 2022-10-07 

.. code-block:: javascript

   var history = hercules.db.getHistory(new Date(2022, 10, 7));


getOrCreate
------------

.. code-block:: javascript

   hercules.db.getOrCreate(id, defaultJson) 

This function gets a document through its *id* or creates and returns a new one with the *defaultJson* content.

The created document is not saved, but will be opened for validation after script finishes.

idsByCategory
----------------

.. code-block:: javascript

    hercules.db.idsByCategory(category)

This function returns an array of ids of all documents under a specific category
when the category’s id is passed as a parameter.

save
-------

.. code-block:: javascript

   hercules.db.save(id, json) 

This function saves an existing or a new document. *json* is the content to save.

saveAll
-------

.. code-block:: javascript

   hercules.db.saveAll() 

This function saves all documents previously modified or created by the script.


update
-------


.. code-block:: javascript

   hercules.db.update(document)

This function updates an existing document.

The updated document is not saved, but will be opened for validation after script finishes.

**Example:** Write a code that gets and updates a document by its id


.. code-block:: javascript

   var cdoc = hercules.db.get("constants");
   cdoc.code_delay = 5;
   hercules.db.update(cdoc);


updateOrCreate
----------------

.. code-block:: javascript

   hercules.db.updateOrCreate(id, json) 

This function updates an existing document or creates a new one with a *json* content. The updated or created document is returned.

The document is not saved, but will be opened for validation after script finishes.

