 
.. _dirty-documents:

Dirty Documents
***************

**Dirty field**

A document field is dirty when its source JSON value

-  is not compatible with the provided schema (invalid value)

-  is missing. In this case, the field involved is a required field that lacks a default value (missing value).

Dirty document
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

A dirty document is a source JSON document that contains dirty fields.

A document usually becomes dirty in these scenarios:

-  The document was saved directly to CouchDB by a third-party application

-  A feature that avoids schema validation (for example, database synchronization) was used to work on the document

-  The document schema got updated.

Why You Have To Fix Dirty Documents
-----------------------------------

Client applications struggle to parse dirty documents, so you have to
fix such documents and then save them.

Hercules will not allow you to save a dirty document.

Finding Dirty Documents
--------------------------

To find dirty documents, use the Data / Find Invalid Documents menu
command.

Fixing Dirty Documents
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Automatic fixing
--------------------------

When you open a dirty document, Hercules tries to fix dirty fields by
filling them with default values. The fields that get fixed
automatically are marked as *modified*. The document is also marked as
*modified*.

The **Edit/Undo** command cannot undo or reverse automatic fixes.

Manual fixing
--------------------------

When Hercules fails to fix a document automatically, you have to fix the
document manually.


Safe Schema Updates
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

A schema update or change is considered *safe* when it does not result in dirty documents.

These are examples of safe updates and changes:

-  Adding a new optional field. A field with a missing value is okay.

-  Adding a new required field with a default value (because client JSON parsers use default values for missing fields).

-  Removing a field (because unknown/unexpected fields are ignored by client JSON parsers)

-  Changing a field type to a broader type (e.g. from integer to double; from required to optional).

These are examples of unsafe updates and changes:

-  Changing a field type to an incompatible type (e.g. from an integer to a list of integers)

-  Adding a new required field without a default value.


.. warning::
 * Schemas produced using any Igor version older than 2.0.7 do not contain information on default values for lists (empty list) and dicts (empty dictionary). For this reason, Hercules always considers new list fields dirty (even if they default to empty lists).
 
 * We strongly recommend you use Igor 2.0.7+ and schema version 1.1+. Hercules issues a warning when schemas produced in old program versions are loaded.


   .. code-block:: igor
   
		// adding required lists with empty defaults prevent Hercules from marking fields as dirty,
		// if schema version 1.1+ is used
		list<int> values = [];


