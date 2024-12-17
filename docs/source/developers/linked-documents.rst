.. meta::
   :http-equiv=X-UA-Compatible: IE=Edge

*****************
Linked Documents
*****************

A linked document is a document inherited from another document.

Why create linked documents?
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

When you create a linked document, you get to:

-  obtain a copy of the original document.

-  make changes to the new (linked) document.

-  synchronize the linked document with the original.

.. note:: You can also obtain a copy of a document by cloning it, but cloning won't help you to keep the two documents in sync.

Implementation in Hercules
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

New Hercules versions store both the contents of the full document and a base document.

The content of the base document (excluding revisions, attachments, and metadata) is stored in the special hercules_base field.

Hercules holds both the original and modified content, so it can easily find out:

-  what fields have been modified by a user

-  what fields are inherited and have to be synchronized with the original document
	 
	 
Benefits of the implementation
------------------------------

Client applications don't have to be aware of the feature. They deal with linked documents transparently by parsing the documents' contents.

External tools—Thalia, external editors, others—don't have to be aware of the feature. They get to deal with linked documents transparently by accessing and modifying the documents' contents.

Advanced Hercules features still work for linked documents. For example, you will still be able to import a linked document from another database. The import operation will not result in a broken document.
Even after the original document gets deleted, the linked document remains valid.

**Dealing With Problems**

When Hercules and third-party applications modify an original document, the content of a linked document is not updated automatically.

In the future, Hercules may provide special support for finding and synchronizing outdated linked documents, but this check is likely to still be triggered externally (by a user).

Broken Links
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

A link is considered broken when

-  its based document is missing (e.g., it might have been deleted)

-  its base document is not accessible (e.g., a linked document was imported from a database, but its base document was not imported)

-  its base document or linked document category got changed

When you open a linked document in Hercules, Hercules shows you broken links.

Document Synchronization
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

A linked document becomes outdated once changes are made to the original document.

When you open a linked document, Hercules synchronization starts. This process involves three JSON states:

-  **Old base** JSON: It is stored in the hercules_base field of the linked document.

-  **Current** JSON of the linked document: It is stored as the content of the linked document.

-  **New base** JSON: It is stored as a new content of the base document.

In the synchronization process, Hercules analyzes the changes applied to the old base by the current base and the new base, applies the changes, and (if necessary) resolves conflicts in favor of the current base.

Synchronization Behavior and Data Types
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Two JSON values are considered equal if they remain equal after all their missing values get substituted with schema defaults.

Fields unknown to the schema are ignored. It ensures that synchronization works correctly with schema changes.

Data Types
--------------------------
.. _atomic:

1. Atomic values

..

   Hercules synchronizes ``atomic`` fields based on these rules:

   If the current value is different from the old base, it is kept.
   Otherwise, the new base value is applied. (Of course, if the new base
   is equal to the old base, there are no changes either).

   Fields can be marked atomic with the atomic meta attribute:

   .. code-block:: igor
   
		[schema meta=(atomic)]
		MyRecord value;

   All non-structural fields are atomic by default.

2. Records

..

   Hercules does synchronization for non-atomic records and variants
   separately, starting with a variant tag.

3. Dictionaries

..

   Hercules dictionaries, like JSON objects, are unordered by design.

   Hercules tries to:

-  identify the keys that have been added or deleted in both current and new base

-  applies the necessary changes

4. Lists

..

   Lists are the most complex entities because the order of items in
   them has to be synchronized.

   These are the three possible cases:

	-  List of atomic values:

	..

		Hercules tries to identify the values that have been added, deleted,
		or reordered in both the current base and new base and then applies
		the necessary changes.

     
	 
	.. _list:

	-  List of records identified by keys:
  
	..

		This case is similar to the previous one, but here Hercules uses keys
		to match records in synchronized lists. When a record with the same
		key exists in both the current base and new base, the record gets
		synchronized recursively.

		Using the key meta attribute, one or multiple keys can be configured
		for a record:
		
		   .. code-block:: igor

				record Item
				{
					[schema meta=(key)]
					string id;

					// other fields
					...
				}

		Records are the same when their key pairs are mutually equal.

		In the example above, Hercules figures out that *Item* records with
		the same id in the old base, current base, and new base lists are the
		same *record* (regardless of how the lists are reordered). Other Item
		fields are also appropriately synchronized.

		Hercules does not have to force the rule that keys inside a list must
		be unique. While having more than one record value with the same key
		set is not recommended, it is not flagged as an error. In such a
		case, Hercules matches records with the same keys in order of
		appearance.

	-  List of non-atomic values:

	..

		Hercules struggles to match reorder list items, so it synchronizes
		list length and then recursively lists the items by index. This
		process is not smart or reliable, so it should be applied when
		dealing with a linked document to which new items are **unlikely** to
		be added, removed, or reordered.

	-  Finally, the whole list can still be made atomic.
