.. meta::
   :http-equiv=X-UA-Compatible: IE=Edge

utils
*********
 
The **utils** object provides useful routines. **utils** implementation can be found at *InstallFolderPath\\JavaScript\\utils.js*


getCustomFields
----------------

.. code-block:: javascript

   utils.getCustomFields(doc)

This function gets all custom fields for a specific document.

-  *doc* - document to be processed.

**Example:** Write a code that removes all custom fields except *scope*.


.. code-block:: javascript

   for (field in utils.getCustomFields(doc)) {    
       if (field != "scope")       
         delete doc[field];
   };


visit
------

.. code-block:: javascript

   utils.visit(doc, callback[, schema])
   
This function processes a document recursively.

-  *doc* - document to be processed

-  *callback* - callback function. *callback* takes these arguments:

   -  *value* - current value

   -  *path* - JSON path to the current value

   -  *descriptor* - schema descriptor

-  The *callback* function returns the new value to completely override it. Or it returns nothing while keeping the old value (it can still be updated).

-  *schema* - optionally override schema document

**Example**: Write a code that searches for all *Hit* records with a *damage* field greater than 10.


.. code-block:: javascript

   utils.visit(doc, function(value, path, descriptor) {
      if (descriptor.name == "Hit" && value.damage > 10) hercules.addSearchResult(doc._id, path, value.damage);
   });

**Example**: Write a code that replaces all the values for *damage* in *Hit* records with 10.


.. code-block:: javascript

   utils.visit(doc, function(value, path, descriptor) {    
      if (descriptor.name == "Hit") value.damage = 10;
   });
