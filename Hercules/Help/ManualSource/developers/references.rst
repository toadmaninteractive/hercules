.. meta::
   :http-equiv=X-UA-Compatible: IE=Edge

***********
References
***********

.. _local-references:

Local references
--------------------

Local references are used to reference and validate values inside the
same document.

**Example**:

	.. code-block:: igor

		record Item
		{
			[schema meta=(reference.id="item" reference.source reference.unique)]
			string id;

			// other fields
			...

		}
		[schema meta=(reference.id="item" reference.validate)]
		define ItemRef string;

		variant Card.CardInventory[inventory]
		{
			list<Item> items;
			?ItemRef default_item;
		}

In the example:

-  item is used as a reference source

-  ItemRef type can hold any id from an item’s collection

In Hercules, you get to view a combo box. Hercules also validates non-existing references.